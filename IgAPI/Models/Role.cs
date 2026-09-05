namespace IgAPI.Models
{
    public interface Role
    {
        // sysAdmin, cp_admin, client_user
        int RoleId { get; set; }

        string RoleName { get; set; } 

        string Description { get; set; }



    }
}
