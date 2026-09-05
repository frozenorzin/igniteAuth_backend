namespace IgAPI.Models;

public interface User
{
    string UserName { get; set; } 

    string PasswordHash { get; set; } 

    long UserId { get; set; }

    int RoleId { get; set; }

    bool IsActive { get; set; }

    DateTime CreatedAt { get; set; }

    DateTime LastLoginAt { get; set; }



}