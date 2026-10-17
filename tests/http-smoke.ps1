param([string]$BaseUrl = 'http://127.0.0.1:18549', [string]$JwtKey = '')
$ErrorActionPreference = 'Stop'
$count = 0
function Request($method, $path, $token = $null, $body = $null) {
    $params = @{ Method = $method; Uri = "$BaseUrl$path"; SkipHttpErrorCheck = $true }
    if ($token) { $params.Headers = @{ Authorization = "Bearer $token" } }
    if ($null -ne $body) { $params.ContentType = 'application/json'; $params.Body = $body | ConvertTo-Json -Depth 10 -Compress }
    $response = Invoke-WebRequest @params
    return @{ Status = [int]$response.StatusCode; Json = $(if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }) }
}
function Check($condition, $message) {
    if (-not $condition) { throw $message }
    $script:count++
}
function Login($username, $password) {
    Request 'POST' '/api/auth/login' $null @{ username = $username; password = $password }
}
$anonymous = Request 'GET' '/api/ReferenceSystem'
Check ($anonymous.Status -eq 401) 'Anonymous catalog access must return 401'
$badLogin = Login 'admin' 'wrong'
Check ($badLogin.Status -eq 401) 'Incorrect password must return 401'
$adminLogin = Login 'admin' 'password123'
$userLogin = Login 'test' 'passwordtest'
Check ($adminLogin.Status -eq 200 -and $adminLogin.Json.role -eq 'Admin') 'Admin login failed'
Check ($userLogin.Status -eq 200 -and $userLogin.Json.role -eq 'User') 'User login failed'
$admin = $adminLogin.Json.accessToken
$user = $userLogin.Json.accessToken
foreach ($token in @($admin, $user)) {
    $session = Request 'GET' '/api/auth/session' $token
    Check ($session.Status -eq 200 -and $session.Json.authenticated) 'Session request failed'
    $catalog = Request 'GET' '/api/ReferenceSystem' $token
    Check ($catalog.Status -eq 200 -and $catalog.Json.intentCommandMappings.Count -eq 7 -and $catalog.Json.authenticatedUser.authId -eq $session.Json.username) 'Catalog request or identity binding failed'
}
$cases = @(
    @('GPIO', 'IntentHash1', 'ReadPinState', $true),
    @('GPIO', 'IntentHash2', 'SetPinState', $false),
    @('ADC', 'IntentHash3', 'ReadADCChannel', $true),
    @('Memory', 'IntentHash4', 'ReadMemoryRegion', $true),
    @('FirmwareManager', 'IntentHash5', 'GetFirmwareVersion', $true),
    @('FirmwareManager', 'IntentHash6', 'RequestFirmwareUpdate', $false),
    @('SystemMonitor', 'IntentHash7', 'ReadSystemHealth', $true)
)
function Decide($token, $subsystem, $intent, $command, $target, $system = 1, $userId = 'spoofed') {
    Request 'POST' '/api/ControlPlane/decide' $token @{
        userId = $userId; referenceSystemId = $system; subSystemId = $target
        intent = $intent; command = $command; subSystem = $subsystem
    }
}
$firstCpc = Decide $admin 'GPIO' 'IntentHash1' 'ReadPinState' 'GPIO'
$secondCpc = Decide $admin 'GPIO' 'IntentHash1' 'ReadPinState' 'GPIO'
Check ($firstCpc.Json.cpcId -gt 0 -and $secondCpc.Json.cpcId -gt $firstCpc.Json.cpcId) 'Backend must issue a fresh CPC ID for each call'
foreach ($case in $cases) {
    $a = Decide $admin $case[0] $case[1] $case[2] $case[0]
    Check ($a.Status -eq 200 -and $a.Json.decision -eq 'Allow') "Admin denied $($case[2]): $($a.Json.reasonCode)"
    $u = Decide $user $case[0] $case[1] $case[2] $case[0] 1 'admin'
    $expected = if ($case[3]) { 'Allow' } else { 'Deny' }
    Check ($u.Status -eq 200 -and $u.Json.decision -eq $expected) "User wrong result for $($case[2])"
    if (-not $case[3]) { Check ($u.Json.reasonCode -eq 'ROLE_FORBIDDEN') 'Write must be role denied even if userId is spoofed' }
}
$invalid = @(
    @('GPIO', 'missing', 'ReadPinState', 'GPIO', 1, 'INTENT_INVALID'),
    @('GPIO', 'IntentHash1', 'missing', 'GPIO', 1, 'COMMAND_INVALID'),
    @('GPIO', 'IntentHash1', 'SetPinState', 'GPIO', 1, 'ICT_MAPPING_FAILED'),
    @('GPIO', 'IntentHash1', 'ReadPinState', 'ADC', 1, 'TARGET_INVALID'),
    @('GPIO', 'IntentHash1', 'ReadPinState', '', 1, 'TARGET_INVALID'),
    @('Unknown', 'IntentHash1', 'ReadPinState', 'Unknown', 1, 'INTENT_INVALID'),
    @('GPIO', 'IntentHash1', 'ReadPinState', 'GPIO', 999, 'REFERENCE_SYSTEM_INVALID')
)
foreach ($case in $invalid) {
    $result = Decide $admin $case[0] $case[1] $case[2] $case[3] $case[4]
    Check ($result.Json.decision -eq 'Deny' -and $result.Json.reasonCode -eq $case[5]) "Expected $($case[5]), got $($result.Json.reasonCode)"
}
$analytics = Request 'GET' '/api/analytics?limit=5&subsystem=GPIO' $admin
Check ($analytics.Status -eq 200) 'Authenticated analytics request failed'
Check ($analytics.Json.returnedObservations -le 5 -and $analytics.Json.matchedObservations -ge $analytics.Json.returnedObservations) 'Analytics window metadata is inconsistent'
Check ($analytics.Json.totalStoredObservations -ge $analytics.Json.matchedObservations -and $analytics.Json.outcomes.Count -gt 0) 'Analytics summary is missing'
Check (($analytics.Json.observations | Where-Object { $_.subsystem -ne 'GPIO' }).Count -eq 0) 'Analytics subsystem filter was not applied'
$rootAnalytics = Request 'GET' '/analytics?limit=1' $user
Check ($rootAnalytics.Status -eq 200 -and $rootAnalytics.Json.returnedObservations -le 1) 'Root analytics alias failed'
$anonymousAnalytics = Request 'GET' '/analytics'
Check ($anonymousAnalytics.Status -eq 401) 'Anonymous analytics access must return 401'
$unauth = Decide $null 'GPIO' 'IntentHash1' 'ReadPinState' 'GPIO'
Check ($unauth.Status -eq 401) 'Anonymous decision must return 401'
$malformed = Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/ControlPlane/decide" -SkipHttpErrorCheck -Headers @{ Authorization = "Bearer $admin" } -ContentType 'application/json' -Body '{bad-json'
Check ([int]$malformed.StatusCode -eq 400) 'Malformed JSON must return 400'
$badToken = Request 'GET' '/api/ReferenceSystem' ($user + 'invalid')
Check ($badToken.Status -eq 401) 'An invalid signature must return 401'
if ($JwtKey) {
    function Base64Url([byte[]]$bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }
    function MakeToken($role) {
        $header = Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
        $claims = @{ iss = 'IgAPI'; aud = 'IgAPI.Client'; sub = 'test'; exp = [DateTimeOffset]::UtcNow.AddMinutes(5).ToUnixTimeSeconds() }
        if ($role) { $claims.role = $role }
        $payload = Base64Url ([Text.Encoding]::UTF8.GetBytes(($claims | ConvertTo-Json -Compress)))
        $signer = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($JwtKey))
        try { $sig = Base64Url ($signer.ComputeHash([Text.Encoding]::ASCII.GetBytes("$header.$payload"))) } finally { $signer.Dispose() }
        return "$header.$payload.$sig"
    }
    foreach ($role in @('', 'Other')) {
        $result = Request 'GET' '/api/ReferenceSystem' (MakeToken $role)
        Check ($result.Status -eq 403) "Missing or unknown role must return 403"
        $result = Decide (MakeToken $role) 'GPIO' 'IntentHash1' 'ReadPinState' 'GPIO'
        Check ($result.Status -eq 403) "Missing or unknown role must not request a decision"
    }
}
Write-Host "HTTP smoke tests passed: $count checks."
