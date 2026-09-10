using IgniteAuth.Data;
using IgniteAuth.Interfaces;

namespace IgniteAuth.Processors.Validation;

public sealed class CommandValidator : ICommandValidator
{
    private readonly CommandData _commandData;

    public CommandValidator(CommandData commandData)
    {
        _commandData = commandData;
    }

    public bool IsValidCommand(
        string subSystem,
        string command)
    {
        if (string.IsNullOrWhiteSpace(subSystem) ||
            string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var subsystemData =
            _commandData.GetSubsystem(subSystem);

        if (subsystemData is null)
        {
            return false;
        }

        return subsystemData.SupportedCommands
            .Contains(command, StringComparer.Ordinal);
    }
}