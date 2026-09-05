using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Interfaces
{
    public interface IIntentCommandMapper
    {
        bool IsValidMapping(string intentHash, string command);

    }
}
