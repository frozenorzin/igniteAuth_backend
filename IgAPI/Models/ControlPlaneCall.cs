using IgniteAuth.Interfaces;

namespace IgAPI.Models
{
    public class ControlPlaneCall : IControlPlaneDecision
    {
        public int CPCId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int ReferenceSystemId { get; set; }

        public string SubSystemId { get; set; } = string.Empty;

        public string Intent { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public string SubSystem { get; set; } = string.Empty;

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public class ControlPlaneResponse
    {
        public int CPCId { get; set; }

        public string Intent { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public string SubSystem { get; set; } = string.Empty;

        public string Decision { get; set; } = "DENY";

        public string ReasonCode { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}