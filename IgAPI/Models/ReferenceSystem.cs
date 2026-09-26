using System.Reflection.PortableExecutable;

namespace IgAPI.Models
{
    public class ReferenceSystem
    {
        public int ReferenceSystemId { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public string SystemType { get; set; } = string.Empty;
        public string SystemVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<SubSystem> Subsystems { get; set; } = new();
        public List<Policy> Policies { get; set; } = new();
    }
}