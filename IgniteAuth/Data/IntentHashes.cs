using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using IgniteAuth.Utilities;

namespace IgniteAuth.Data;

public sealed class IntentHashes
{
    [JsonPropertyName("file")]
    public required string File { get; init; } = String.Empty;

    [JsonPropertyName("system")]
    public required string System { get; init; }

    [JsonPropertyName("policy")]
    public required string Policy { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("intentData")]
    public required List<SubsystemIntents> IntentData { get; init; }


    public static IntentHashes LoadFromJson(string filePath)
    {
        if (!global::System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"The file '{filePath}' was not found.");
        }

        var json = global::System.IO.File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<IntentHashes>(json)
            ?? throw new InvalidOperationException(
                $"{filePath}: Unknown JSON format.");
    }

    public void SaveToJson(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(this, options);
        global::System.IO.File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Convert plain (human-readable) intents contained in <paramref name="sourceFilePath"/>
    /// into deterministic HMAC-SHA256 hex strings using <paramref name="secretKey"/>,
    /// and write the resulting IntentHashes JSON to <paramref name="outputFilePath"/>.
    /// </summary>
    /// 
    public static void ConvertPlainIntentToHashedJson(
        string sourceFilePath,
        string outputFilePath,
        byte[] secretKey)
    {
        var plain = LoadFromJson(sourceFilePath);

        var hashedIntentData = plain.IntentData
            .Select(si=>new SubsystemIntents
            { 
                
                SubSystem = si.SubSystem,
                Intents = si.Intents
                           .Select(intent=>IntentHasher.ComputeHmacSha256Hex(intent, secretKey))
                           .ToList()



            }).ToList();

        var result = new IntentHashes
        {
            File = global::System.IO.Path.GetFileName(outputFilePath),
            System = plain.System,
            Policy = plain.Policy,
            Description = plain.Description,
            IntentData = hashedIntentData
        };

        result.SaveToJson(outputFilePath);
    }

    // sub system intents are unique, so we can use FirstOrDefault to find the matching subsystem
    public SubsystemIntents? GetSubsystem(string subSystem)
    {
        return IntentData.FirstOrDefault(
            x => string.Equals(
                x.SubSystem,
                subSystem,
                StringComparison.Ordinal));
    }


    public HashSet<string> GetAllIntentHashes()
    {
        return IntentData
            .SelectMany(x => x.Intents)
            .ToHashSet(StringComparer.Ordinal);
    }
}


public sealed class SubsystemIntents
{
    [JsonPropertyName("subSystem")]
    public required string SubSystem { get; init; }

    [JsonPropertyName("intents")]
    public required List<string> Intents { get; init; }
}