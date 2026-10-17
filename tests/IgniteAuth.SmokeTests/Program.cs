using System.Text;
using System.Text.Json;
using IgniteAuth.Data;
using IgniteAuth.Processors;
using IgniteAuth.Processors.Validation;
using IgniteAuth.Utilities;

var data = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../IgniteAuth/Data"));
var secret = Encoding.UTF8.GetBytes("smoke-test-key");
var intents = IntentHashes.LoadFromJson(Path.Combine(data, "IntentHashes.json"));
var commands = CommandData.LoadFromJson(Path.Combine(data, "CommandData.json"));
var mappings = IntentCommandMapping.LoadFromJson(Path.Combine(data, "IntentCommandMapping.json"));
var hashedIntents = new IntentHashes {
    File = "test", System = intents.System, Policy = intents.Policy, Description = intents.Description,
    IntentData = intents.IntentData.Select(x => new SubsystemIntents {
        SubSystem = x.SubSystem, Intents = x.Intents.Select(i => IntentHasher.ComputeHmacSha256Hex(i, secret)).ToList()
    }).ToList()
};
var hashedMappings = new IntentCommandMapping {
    File = "test", System = mappings.System, Policy = mappings.Policy, Description = mappings.Description,
    Mappings = mappings.Mappings.Select(x => new SubsystemMapping {
        SubSystem = x.SubSystem, IntentMappings = x.IntentMappings.Select(m => new IntentMapping {
            Intent = IntentHasher.ComputeHmacSha256Hex(m.Intent, secret), Commands = m.Commands.ToList()
        }).ToList()
    }).ToList()
};
PolicyCatalogValidator.Validate(intents, hashedIntents, commands, mappings, hashedMappings, secret);
try {
    PolicyCatalogValidator.Validate(intents, hashedIntents, commands, mappings, hashedMappings, Encoding.UTF8.GetBytes("wrong"));
    throw new Exception("Wrong secret was accepted");
} catch (InvalidOperationException) { }
var processor = new ControlPlaneProcessor(new IntentValidator(hashedIntents, secret),
    new CommandValidator(commands), new IntentCommandMapper(hashedMappings, secret));
var count = 0;
async Task Check(string subsystem, string intent, string command, string target, string expected,
    string? user = "admin", string? raw = null) {
    raw ??= JsonSerializer.Serialize(new { UserId = user, SubSystem = subsystem, Intent = intent, Command = command, SubSystemId = target });
    var result = await processor.ProcessAsync(user!, subsystem, intent, command, target, raw);
    if (result.ReasonCode != expected) throw new Exception($"{subsystem}/{intent}/{command}: expected {expected}, got {result.ReasonCode}");
    count++;
}
foreach (var group in mappings.Mappings)
    foreach (var mapping in group.IntentMappings)
        foreach (var command in mapping.Commands)
            await Check(group.SubSystem, mapping.Intent, command, group.SubSystem, "ALLOW_VALIDATED");
await Check("GPIO", "not-an-intent", "ReadPinState", "GPIO", "INTENT_INVALID");
await Check("GPIO", "IntentHash1", "NotACommand", "GPIO", "COMMAND_INVALID");
await Check("GPIO", "IntentHash1", "SetPinState", "GPIO", "ICT_MAPPING_FAILED");
await Check("Unknown", "IntentHash1", "ReadPinState", "Unknown", "INTENT_INVALID");
await Check("GPIO", "IntentHash1", "ReadPinState", "ADC", "TARGET_INVALID");
await Check("GPIO", "IntentHash1", "ReadPinState", "", "TARGET_INVALID");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", "");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", raw: "not-json");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", raw: "{}");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", raw: "[]");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", raw: "{\"UserId\":\"other\",\"SubSystem\":\"GPIO\",\"Intent\":\"IntentHash1\",\"Command\":\"ReadPinState\",\"SubSystemId\":\"GPIO\"}");
await Check("GPIO", "IntentHash1", "ReadPinState", "GPIO", "SCHEMA_INVALID", raw: "{\"UserId\":\"admin\",\"userId\":null}");
await Check("GPIO", "  intenthash1  ", "ReadPinState", "GPIO", "ALLOW_VALIDATED");
using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
try {
    await processor.ProcessAsync("admin", "GPIO", "IntentHash1", "ReadPinState", "GPIO", "{}", cancelled.Token);
    throw new Exception("Cancellation was ignored");
} catch (OperationCanceledException) { }
Console.WriteLine($"Core smoke tests passed: {count} decisions, catalog consistency and cancellation.");
