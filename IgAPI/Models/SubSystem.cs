namespace IgAPI.Models
{
    public class SubSystem
    {
        public int SubSystemId { get; set; }

        // Foreign key / parent link
        public int ReferenceSystemId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }
}