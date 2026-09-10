using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Interfaces
{
    public interface IIntentValidator
    
    {
        bool IsValidIntent(string subSystem, string intentHash);
    }
}
