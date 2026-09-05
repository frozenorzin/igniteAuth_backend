using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;

namespace IgniteAuth.Data
{
    public sealed class IntentHashes

    {
        public required Dictionary<string, Dictionary<string, string>> IntentData { get; init; }

        // Script to load the JSON file and deserialize it into the IntentHashes object
        public static IntentHashes LoadFromJson(string filePath)

        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");
            }

            var json = File.ReadAllText(filePath);

            var root = JsonSerializer.Deserialize<IntentHashesRoot>(json)
                ?? throw new InvalidOperationException($"Invalid {filePath} format");

            return new IntentHashes
            {
                IntentData = root.IntentData ?? new Dictionary<string, Dictionary<string, string>>()
            };
        }

        // Value extraction for Valid Intent Hashes
        public HashSet<string> GetAllIntentHashes()
        {
            return IntentData.Values
                .SelectMany(intentMap => intentMap.Values)
                .ToHashSet(StringComparer.Ordinal);
        }

        // General format for the data
        public sealed class  IntentHashesRoot
        {
            public Dictionary<string, Dictionary<string, string>>? IntentData { get; init; }

        }


    }
}
