namespace IgAPI.DTOs.ReferenceSystem;

public sealed record IgniteAuthReferenceDataDto(
    IReadOnlyList<RegisteredSubsystemDto> Subsystems,
    IReadOnlyList<DisplayableIntentDto> Intents,
    IReadOnlyList<RegisteredCommandDto> Commands);

public sealed record RegisteredSubsystemDto(string SubsystemId, string Name);
public sealed record DisplayableIntentDto(string SubsystemId, string Intent);
public sealed record RegisteredCommandDto(string SubsystemId, string Command);
