using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Interfaces
{
    public interface ICommandValidator
    {
        bool IsValidCommand(string subSystem, string command);

 }
}
