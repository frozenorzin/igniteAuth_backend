namespace IgAPI.Models
{
    public interface ControlPlaneCall
    {
        long CPCId { get; set; }
        int RequestId { get; set; }
        long UserId { get; set; }

        int ReferenceSystemId { get; set; }

        string SubSystemId { get; set; }

        string Intent { get; set; }

        string Command { get; set; }

        string Target { get; set; }

        string RequestSource { get; set; }

        DateTime TimeStamp { get; set;  }

    }
}
