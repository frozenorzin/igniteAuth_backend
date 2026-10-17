using System.Globalization;
using System.Text;

namespace IgAPI.Analytics;

public sealed record DecisionAnalyticsEvent(
    Guid EventId,
    DateTimeOffset RecordedAtUtc,
    string TraceId,
    int CpcId,
    int ReferenceSystemId,
    string Role,
    string Subsystem,
    string Target,
    string Intent,
    string Command,
    string Decision,
    string ReasonCode,
    double DurationMs);

public sealed record DecisionAnalyticsQuery(
    int Limit,
    DateTimeOffset? FromUtc,
    string? Subsystem,
    string? Decision);

public sealed record DecisionOutcomeSummary(string Decision, long Count, double Percentage);

public sealed record DecisionAnalyticsSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string Storage,
    long TotalStoredObservations,
    long MatchedObservations,
    int ReturnedObservations,
    double MeanLatencyMs,
    double P95LatencyMs,
    IReadOnlyList<DecisionOutcomeSummary> Outcomes,
    IReadOnlyList<DecisionAnalyticsEvent> Observations);

public interface IDecisionAnalyticsStore
{
    Task AppendAsync(DecisionAnalyticsEvent observation, CancellationToken cancellationToken = default);
    Task<DecisionAnalyticsSnapshot> QueryAsync(DecisionAnalyticsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Append-only CSV storage intended for a single IgAPI process. Writes and reads
/// are serialized so readers never observe a partially appended record.
/// </summary>
public sealed class CsvDecisionAnalyticsStore : IDecisionAnalyticsStore
{
    private const string Header = "schema_version,event_id,recorded_at_utc,trace_id,cpc_id,reference_system_id,role,subsystem,target,intent,command,decision,reason_code,duration_ms";
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<CsvDecisionAnalyticsStore> _logger;

    public CsvDecisionAnalyticsStore(string contentRoot, ILogger<CsvDecisionAnalyticsStore> logger)
    {
        _logger = logger;
        var root = FindSolutionRoot(contentRoot) ?? contentRoot;
        var directory = Path.Combine(root, "logs", "analytics");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "control-plane-decisions.v1.csv");
        InitializeFile();
    }

    public async Task AppendAsync(DecisionAnalyticsEvent observation, CancellationToken cancellationToken = default)
    {
        var fields = new[]
        {
            "1",
            observation.EventId.ToString("D"),
            observation.RecordedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            SafeText(observation.TraceId),
            observation.CpcId.ToString(CultureInfo.InvariantCulture),
            observation.ReferenceSystemId.ToString(CultureInfo.InvariantCulture),
            SafeText(observation.Role),
            SafeText(observation.Subsystem),
            SafeText(observation.Target),
            SafeText(observation.Intent),
            SafeText(observation.Command),
            SafeText(observation.Decision),
            SafeText(observation.ReasonCode),
            observation.DurationMs.ToString("0.00", CultureInfo.InvariantCulture)
        };
        var line = string.Join(',', fields.Select(EscapeCsv)) + Environment.NewLine;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_filePath, line, new UTF8Encoding(false), cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogError(exception, "Could not append the control-plane analytics CSV.");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DecisionAnalyticsSnapshot> QueryAsync(
        DecisionAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var recent = new Queue<DecisionAnalyticsEvent>(query.Limit);
            long total = 0;
            long matched = 0;

            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var header = await reader.ReadLineAsync(cancellationToken);
            if (!string.Equals(header, Header, StringComparison.Ordinal))
                throw new InvalidDataException("The analytics CSV schema header is invalid.");

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                total++;
                if (!TryParse(line, out var observation))
                {
                    _logger.LogWarning("Skipped malformed analytics CSV record {RecordNumber}.", total);
                    continue;
                }
                if (query.FromUtc is not null && observation.RecordedAtUtc < query.FromUtc.Value) continue;
                if (!string.IsNullOrWhiteSpace(query.Subsystem) &&
                    !string.Equals(observation.Subsystem, query.Subsystem, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrWhiteSpace(query.Decision) &&
                    !string.Equals(observation.Decision, query.Decision, StringComparison.OrdinalIgnoreCase)) continue;

                matched++;
                if (recent.Count == query.Limit) recent.Dequeue();
                recent.Enqueue(observation);
            }

            var observations = recent.ToArray();
            var durations = observations.Select(value => value.DurationMs).OrderBy(value => value).ToArray();
            var mean = durations.Length == 0 ? 0 : durations.Average();
            var p95Index = Math.Max(0, (int)Math.Ceiling(durations.Length * 0.95) - 1);
            var p95 = durations.Length == 0 ? 0 : durations[p95Index];
            var outcomes = observations
                .GroupBy(value => value.Decision, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new DecisionOutcomeSummary(
                    group.Key,
                    group.LongCount(),
                    observations.Length == 0 ? 0 : Math.Round(group.LongCount() * 100d / observations.Length, 2)))
                .ToArray();

            return new DecisionAnalyticsSnapshot(
                DateTimeOffset.UtcNow,
                "append-only-csv-v1",
                total,
                matched,
                observations.Length,
                Math.Round(mean, 2),
                Math.Round(p95, 2),
                outcomes,
                observations);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void InitializeFile()
    {
        if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
        {
            File.WriteAllText(_filePath, Header + Environment.NewLine, new UTF8Encoding(false));
            return;
        }

        using var reader = new StreamReader(_filePath, Encoding.UTF8, true);
        if (!string.Equals(reader.ReadLine(), Header, StringComparison.Ordinal))
            throw new InvalidDataException("Existing analytics CSV has an unsupported schema.");
    }

    private static bool TryParse(string line, out DecisionAnalyticsEvent observation)
    {
        observation = default!;
        var fields = ParseCsv(line);
        if (fields.Count != 14 || fields[0] != "1" ||
            !Guid.TryParse(fields[1], out var eventId) ||
            !DateTimeOffset.TryParseExact(fields[2], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var recordedAt) ||
            !int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cpcId) ||
            !int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var referenceSystemId) ||
            !double.TryParse(fields[13], NumberStyles.Float, CultureInfo.InvariantCulture, out var durationMs))
            return false;

        observation = new DecisionAnalyticsEvent(
            eventId, recordedAt, RestoreText(fields[3]), cpcId, referenceSystemId,
            RestoreText(fields[6]), RestoreText(fields[7]), RestoreText(fields[8]),
            RestoreText(fields[9]), RestoreText(fields[10]), RestoreText(fields[11]),
            RestoreText(fields[12]), durationMs);
        return true;
    }

    private static List<string> ParseCsv(string line)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                result.Add(value.ToString());
                value.Clear();
            }
            else value.Append(character);
        }
        result.Add(value.ToString());
        return result;
    }

    private static string EscapeCsv(string value) =>
        value.IndexOfAny([',', '"']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static string SafeText(string? value)
    {
        var normalized = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (normalized.Length > 256) normalized = normalized[..256];
        return normalized.Length > 0 && "=+-@\t".Contains(normalized[0]) ? "'" + normalized : normalized;
    }

    private static string RestoreText(string value) =>
        value.Length > 1 && value[0] == '\'' && "=+-@\t".Contains(value[1]) ? value[1..] : value;

    private static string? FindSolutionRoot(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "IgniteAuth_V0.9.slnx"))) return directory.FullName;
        return null;
    }
}
