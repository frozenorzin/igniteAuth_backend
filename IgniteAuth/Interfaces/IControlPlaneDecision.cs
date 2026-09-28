using System;

namespace IgniteAuth.Interfaces
{
    public interface IControlPlaneDecision
    {
        int CPCId { get; set; }

        string UserId { get; set; }

        int ReferenceSystemId { get; set; }

        string SubSystemId { get; set; }

        string Intent { get; set; }

        string Command { get; set; }

        string SubSystem { get; set; }

        DateTime TimeStamp { get; set; }
    }
}