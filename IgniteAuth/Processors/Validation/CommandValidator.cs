using IgniteAuth.Data;
using IgniteAuth.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Processors.Validation
{
    public sealed class CommandValidator : ICommandValidator
    
    {
        private readonly HashSet<string> _validCommands;

        public CommandValidator(CommandData commandData)
        {
            _validCommands = commandData.GetAllCommands();
        }

        public bool IsValidCommand(string Command)
        {
            return !string.IsNullOrWhiteSpace(Command) && _validCommands.Contains(Command);
        }
    }
}
