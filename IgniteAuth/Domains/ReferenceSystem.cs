using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Domains
{
    internal class ReferenceSystem
    
    {
        int ReferenceSystemId { get; set; }
        string SystemName { get; set; } = string.Empty;
        string SystemType { get; set; } = string.Empty;
        string SystemVersion { get; set; } = string.Empty;
        string Description { get; set; } = string.Empty;
        string Status { get; set; } = string.Empty;
        DateTime CreatedAt { get; set; }


    }
}
