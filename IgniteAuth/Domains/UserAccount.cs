using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Domains
{
    internal class UserAccount
    
    {
        int UserId { get; set; }
        string Username { get; set; } = string.Empty;
        string PasswordHash { get; set; } = string.Empty;
        int RoleId { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime LastLoginAt { get; set; }
        bool IsActive { get; set; }
    }
}
