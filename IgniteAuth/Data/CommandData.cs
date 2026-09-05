using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace IgniteAuth.Data
{
    public sealed class CommandData
    
    {
        [JsonPropertyName("Commands")]
        public required Dictionary<string, List<string>> Commands { get; init; }

        public static CommandData LoadFromJson(string filePath)
        {
            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' was not found.");
            }

            var json = File.ReadAllText(filePath);

            var data = JsonSerializer.Deserialize<CommandData>(json) 
                ?? throw new InvalidOperationException($" {filePath}: Unknown JSON format .");

            return data;
        }

        public HashSet<string> GetAllCommands() => Commands.Values
              .SelectMany(c => c)
              .ToHashSet(StringComparer.Ordinal);
        }
}
