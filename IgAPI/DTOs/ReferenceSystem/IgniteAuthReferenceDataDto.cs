namespace IgAPI.DTOs.ReferenceSystem;

public sealed record IgniteAuthReferenceDataDto(
    IReadOnlyList<RegisteredSubsystemDto> Subsystems,
    IReadOnlyList<DisplayableIntentDto> Intents,
    IReadOnlyList<RegisteredCommandDto> Commands,
    IReadOnlyList<IntentCommandMappingDto> IntentCommandMappings);

public sealed record RegisteredSubsystemDto(string SubsystemId, string Name);
public sealed record DisplayableIntentDto(string SubsystemId, string Intent);
public sealed record RegisteredCommandDto(string SubsystemId, string Command);
public sealed record IntentCommandMappingDto(
    string SubsystemId,
    string Intent,
    IReadOnlyList<string> Commands);
