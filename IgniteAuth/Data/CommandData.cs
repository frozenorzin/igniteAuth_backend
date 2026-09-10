using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace IgniteAuth.Data;

public sealed class CommandData
{
    [JsonPropertyName("file")]
    public required string File { get; init; }

    [JsonPropertyName("system")]
    public required string System { get; init; }

    [JsonPropertyName("policy")]
    public required string Policy { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("commands")]
    public required List<SubsystemCommands> Commands { get; init; }


    public static CommandData LoadFromJson(string filePath)
    {
        if (!global::System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"The file '{filePath}' was not found.");
        }

        var json = global::System.IO.File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<CommandData>(json)
            ?? throw new InvalidOperationException(
                $"{filePath}: Unknown JSON format.");
    }


    public SubsystemCommands? GetSubsystem(string subSystem)
    {
        return Commands.FirstOrDefault(
            x => string.Equals(
                x.SubSystem,
                subSystem,
                StringComparison.Ordinal));
    }


    public HashSet<string> GetAllCommands()
    {
        return Commands
            .SelectMany(x => x.SupportedCommands)
            .ToHashSet(StringComparer.Ordinal);
    }
}


public sealed class SubsystemCommands
{
    [JsonPropertyName("subSystem")]
    public required string SubSystem { get; init; }

    [JsonPropertyName("supportedCommands")]
    public required List<string> SupportedCommands { get; init; }
}