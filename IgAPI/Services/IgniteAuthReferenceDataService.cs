using IgniteAuth.Data;
using IgAPI.DTOs.ReferenceSystem;
using IgAPI.Services.Interfaces;

namespace IgAPI.Services;

/// <summary>
/// Adapts IgniteAuth's existing JSON contracts into a safe API projection.
/// The display catalog is distinct from the HMAC-backed catalog used by validators.
/// </summary>
public sealed class IgniteAuthReferenceDataService : IIgniteAuthReferenceDataService
{
    private readonly IntentHashes _displayableIntents;
    private readonly CommandData _commands;
    private readonly IntentCommandMapping _intentCommandMapping;

    public IgniteAuthReferenceDataService(
        IntentHashes displayableIntents,
        CommandData commands,
        IntentCommandMapping intentCommandMapping)
    {
        _displayableIntents = displayableIntents;
        _commands = commands;
        _intentCommandMapping = intentCommandMapping;
    }

    public Task<IgniteAuthReferenceDataDto> GetReferenceDataAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var subsystemNames = _displayableIntents.IntentData
            .Select(entry => entry.SubSystem)
            .Concat(_commands.Commands.Select(entry => entry.SubSystem))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var subsystems = subsystemNames
            .Select(name => new RegisteredSubsystemDto(name, name))
            .ToList();
        var intents = _displayableIntents.IntentData
            .SelectMany(entry => entry.Intents.Select(intent => new DisplayableIntentDto(entry.SubSystem, intent)))
            .OrderBy(item => item.SubsystemId, StringComparer.Ordinal)
            .ThenBy(item => item.Intent, StringComparer.Ordinal)
            .ToList();
        var commands = _commands.Commands
            .SelectMany(entry => entry.SupportedCommands.Select(command => new RegisteredCommandDto(entry.SubSystem, command)))
            .OrderBy(item => item.SubsystemId, StringComparer.Ordinal)
            .ThenBy(item => item.Command, StringComparer.Ordinal)
            .ToList();

        var mappings = _intentCommandMapping.Mappings
            .SelectMany(subsystem => subsystem.IntentMappings.Select(mapping =>
                new IntentCommandMappingDto(
                    subsystem.SubSystem,
                    mapping.Intent,
                    mapping.Commands
                        .OrderBy(command => command, StringComparer.Ordinal)
                        .ToList())))
            .OrderBy(item => item.SubsystemId, StringComparer.Ordinal)
            .ThenBy(item => item.Intent, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(new IgniteAuthReferenceDataDto(
            subsystems, intents, commands, mappings));
    }
}
