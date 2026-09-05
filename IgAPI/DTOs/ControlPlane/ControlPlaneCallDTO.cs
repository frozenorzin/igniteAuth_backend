namespace IgAPI.DTOs.ControlPlane
{
    sealed class ControlPlaneCallDTO
    
    
    {   // the input of Control Plane call from the system

        public required string AuthId { get; init; }
        public required string IntentHash { get; init; }
        public required string Command { get; init; }

        public required string SubSystem { get; init; }
        public required string RequestSource { get; init; }

        public required string RawDataJson { get; init; }
        


    }
}
