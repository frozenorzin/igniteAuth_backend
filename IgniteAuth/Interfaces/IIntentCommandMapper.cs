using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Interfaces
{
    public interface IIntentCommandMapper
    {
        bool IsValidMapping(string subSystem, string intentHash, string command);

    }
}
