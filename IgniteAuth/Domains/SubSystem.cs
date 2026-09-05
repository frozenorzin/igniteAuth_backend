using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Domains
{
    internal class SubSystem
    {
        int SubSystemId { get; set; }
        string SubSystemName { get; set; } = string.Empty;
        string Description { get; set; } = string.Empty;
    }
}
