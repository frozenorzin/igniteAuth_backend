using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace IgAPI.Logging;

public sealed record DecisionLogDetails(
    int CpcId,
    int ReferenceSystemId,
    string? Subsystem,
    string? Target,
    string? Intent,
    string? Command,
    string Decision,
    string ReasonCode,
    string Message);

/// <summary>Writes one JSON record per HTTP response, without request or response bodies.</summary>
public sealed class ResponseLogWriter
{
    public static readonly object DecisionDetailsKey = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger _logger;

    public ResponseLogWriter(string contentRoot, ILogger logger)
    {
        _logger = logger;
        var root = FindSolutionRoot(contentRoot) ?? contentRoot;
        var directory = Path.Combine(root, "logs");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "IgniteAuth.log");
        using var file = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
    }

    public async Task WriteAsync(HttpContext context, long startedAt, int statusCode, string? exceptionType)
    {
        var details = context.Items.TryGetValue(DecisionDetailsKey, out var value)
            ? value as DecisionLogDetails : null;
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            traceId = Limit(context.TraceIdentifier),
            method = Limit(context.Request.Method),
            path = Limit(context.Request.Path.Value),
            statusCode,
            durationMs = Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 2),
            user = Limit(context.User.FindFirstValue(ClaimTypes.NameIdentifier)),
            role = Limit(context.User.FindFirstValue(ClaimTypes.Role)),
            errorType = exceptionType,
            decision = details is null ? null : new
            {
                details.CpcId,
                details.ReferenceSystemId,
                subsystem = Limit(details.Subsystem),
                target = Limit(details.Target),
                intent = Limit(details.Intent),
                command = Limit(details.Command),
                details.Decision,
                details.ReasonCode,
                details.Message
            }
        };

        try
        {
            var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
            await _writeLock.WaitAsync();
            try { await File.AppendAllTextAsync(_filePath, line); }
            finally { _writeLock.Release(); }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Logging failure must not change a response that may already have been sent.
            _logger.LogError(exception, "Could not write the IgniteAuth response log.");
        }
    }

    private static string? Limit(string? value) => value is { Length: > 256 } ? value[..256] : value;

    private static string? FindSolutionRoot(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IgniteAuth_V0.9.slnx")))
                return directory.FullName;
        }
        return null;
    }
}
