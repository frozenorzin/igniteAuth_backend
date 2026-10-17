using System.Diagnostics.CodeAnalysis;
using IgniteAuth.Utilities;

namespace IgniteAuth.Data;

/// <summary>Checks that the displayed and enforced STM32 policies agree.</summary>
public static class PolicyCatalogValidator
{
    public static void Validate(IntentHashes plainIntents, IntentHashes hashedIntents,
        CommandData commands, IntentCommandMapping plainMappings,
        IntentCommandMapping hashedMappings, byte[] secretKey)
    {
        void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Invalid STM32 policy catalog: {message}");
        }

        Require(secretKey is { Length: > 0 }, "intent secret is empty.");
        var systems = new[] { plainIntents.System, hashedIntents.System, commands.System, plainMappings.System, hashedMappings.System };
        var policies = new[] { plainIntents.Policy, hashedIntents.Policy, commands.Policy, plainMappings.Policy, hashedMappings.Policy };
        Require(systems.All(x => x == "STM32ReferenceSystem") && policies.All(x => x == "STM32IgniteAuthPolicy"), "system or policy identifiers disagree.");

        static bool UniqueNames(IEnumerable<string> names)
        {
            var list = names.ToList();
            return list.Count > 0 && list.All(x => !string.IsNullOrWhiteSpace(x)) && list.Distinct(StringComparer.Ordinal).Count() == list.Count;
        }

        Require(plainIntents.IntentData != null && hashedIntents.IntentData != null && commands.Commands != null && plainMappings.Mappings != null && hashedMappings.Mappings != null, "a required collection is missing.");
        Require(UniqueNames(plainIntents.IntentData.Select(x => x.SubSystem)) &&
                UniqueNames(hashedIntents.IntentData.Select(x => x.SubSystem)) &&
                UniqueNames(commands.Commands.Select(x => x.SubSystem)) &&
                UniqueNames(plainMappings.Mappings.Select(x => x.SubSystem)) &&
                UniqueNames(hashedMappings.Mappings.Select(x => x.SubSystem)), "subsystems must be nonempty and unique.");

        var names = plainIntents.IntentData.Select(x => x.SubSystem).ToHashSet(StringComparer.Ordinal);
        Require(names.SetEquals(hashedIntents.IntentData.Select(x => x.SubSystem)) &&
                names.SetEquals(commands.Commands.Select(x => x.SubSystem)) &&
                names.SetEquals(plainMappings.Mappings.Select(x => x.SubSystem)) &&
                names.SetEquals(hashedMappings.Mappings.Select(x => x.SubSystem)), "subsystem catalogs disagree.");

        foreach (var name in names)
        {
            var intents = plainIntents.GetSubsystem(name)!.Intents;
            var hashes = hashedIntents.GetSubsystem(name)!.Intents;
            var allowedCommands = commands.GetSubsystem(name)!.Commands;
            var mappings = plainMappings.GetSubsystem(name)!.IntentMappings;
            var hashed = hashedMappings.GetSubsystem(name)!.IntentMappings;
            Require(intents != null && hashes != null && allowedCommands != null && mappings != null && hashed != null, $"{name} has missing data.");
            Require(UniqueNames(intents!) && UniqueNames(hashes!) && UniqueNames(allowedCommands!) &&
                    UniqueNames(mappings!.Select(x => x.Intent)) && UniqueNames(hashed!.Select(x => x.Intent)), $"{name} has empty or duplicate entries.");
            var generatedHashes = intents!.Select(x => IntentHasher.ComputeHmacSha256Hex(x, secretKey)).ToList();
            Require(UniqueNames(generatedHashes) && generatedHashes.ToHashSet(StringComparer.Ordinal).SetEquals(hashes!), $"{name} intent hashes do not match the configured secret. Regenerate both hashed catalogs.");
            Require(intents.ToHashSet(StringComparer.Ordinal).SetEquals(mappings!.Select(x => x.Intent)) &&
                    generatedHashes.ToHashSet(StringComparer.Ordinal).SetEquals(hashed!.Select(x => x.Intent)), $"{name} intent mappings disagree.");
            var mappedCommands = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in mappings)
            {
                Require(mapping.Commands != null && UniqueNames(mapping.Commands), $"{name} has an empty or duplicate command mapping.");
                var hash = IntentHasher.ComputeHmacSha256Hex(mapping.Intent, secretKey);
                var hashedMapping = hashed.Single(x => x.Intent == hash);
                Require(hashedMapping.Commands != null && UniqueNames(hashedMapping.Commands) &&
                        mapping.Commands!.ToHashSet(StringComparer.Ordinal).SetEquals(hashedMapping.Commands), $"{name} hashed mappings disagree.");
                mappedCommands.UnionWith(mapping.Commands!);
            }
            Require(mappedCommands.SetEquals(allowedCommands!), $"{name} commands and mappings disagree.");
        }
    }
}
