using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using IgniteAuth.Utilities;

namespace IgniteAuth.Data;

public sealed class IntentCommandMapping
{
    [JsonPropertyName("file")]
    public required string File { get; init; }

    [JsonPropertyName("system")]
    public required string System { get; init; }

    [JsonPropertyName("policy")]
    public required string Policy { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("mappings")]
    public required List<SubsystemMapping> Mappings { get; init; }


    public static IntentCommandMapping LoadFromJson(string filePath)
    {
        if (!global::System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"The file '{filePath}' was not found.");
        }

        var json = global::System.IO.File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<IntentCommandMapping>(json)
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
    /// and write the resulting IntentCommandMapping JSON to <paramref name="outputFilePath"/>.
    /// </summary>
    public static void ConvertPlainIntentToHashedJson(
        string sourceFilePath,
        string outputFilePath,
        byte[] secretKey)
    {
        var plain = LoadFromJson(sourceFilePath);

        var hashedMappings = plain.Mappings
            .Select(sm => new SubsystemMapping
            {
                SubSystem = sm.SubSystem,
                IntentMappings = sm.IntentMappings
                    .Select(im => new IntentMapping
                    {
                        Intent = IntentHasher.ComputeHmacSha256Hex(im.Intent, secretKey),
                        Commands = im.Commands
                    })
                    .ToList()
            })
            .ToList();

        var result = new IntentCommandMapping
        {
            File = global::System.IO.Path.GetFileName(outputFilePath),
            System = plain.System,
            Policy = plain.Policy,
            Description = plain.Description,
            Mappings = hashedMappings
        };

        result.SaveToJson(outputFilePath);
    }

    public SubsystemMapping? GetSubsystem(string subSystem)
    {
        return Mappings.FirstOrDefault(
            x => string.Equals(
                x.SubSystem,
                subSystem,
                StringComparison.Ordinal));
    }


    public IntentMapping? GetIntentMapping(
        string subSystem,
        string intent)
    {
        var subsystem = GetSubsystem(subSystem);

        return subsystem?.IntentMappings.FirstOrDefault(
            x => string.Equals(
                x.Intent,
                intent,
                StringComparison.Ordinal));
    }
}


public sealed class SubsystemMapping
{
    [JsonPropertyName("subSystem")]
    public required string SubSystem { get; init; }

    [JsonPropertyName("intentMappings")]
    public required List<IntentMapping> IntentMappings { get; init; }
}


public sealed class IntentMapping
{
    [JsonPropertyName("intent")]
    public required string Intent { get; init; }

    [JsonPropertyName("commands")]
    public required List<string> Commands { get; init; }
}