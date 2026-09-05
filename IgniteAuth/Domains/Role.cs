using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Domains
{
    internal class Role
    
    {
        int RoleId { get; set; }
        string RoleName { get; set; } = string.Empty;
        string Description { get; set; } = string.Empty;
    }
}
