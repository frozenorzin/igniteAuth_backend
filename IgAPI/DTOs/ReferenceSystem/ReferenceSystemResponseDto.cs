namespace IgAPI.DTOs.ReferenceSystem;

public sealed record ReferenceSystemResponseDto(
    int ReferenceSystemId,
    string SystemName,
    string SystemType,
    string SystemVersion,
    string Description,
    string Status,
    DateTime CreatedAt,
    AuthenticatedUserDto AuthenticatedUser,
    IReadOnlyList<RegisteredSubsystemDto> RegisteredSubsystems,
    IReadOnlyList<DisplayableIntentDto> ValidIntents,
    IReadOnlyList<RegisteredCommandDto> ValidCommands);

public sealed record AuthenticatedUserDto(string AuthId);
