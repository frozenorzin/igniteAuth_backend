using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Results
{
    public enum ControlPlaneDecision
    
    {
        deny = 0,
        allow = 1
    }


    public sealed record ControlPlaneResult
    {
            public ControlPlaneDecision Decision { get; init; } = ControlPlaneDecision.deny;
       
            public required string ReasonCode { get; init; }
            public required string Message { get; init; }
            public string? CorrelationId { get; init; }
            public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
        


    }




}
