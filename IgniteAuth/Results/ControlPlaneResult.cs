using System;

namespace IgniteAuth.Results
{
    public enum ControlPlaneDecisionVerdict
    {
        Deny = 0,
        Allow = 1
    }

    public sealed record ControlPlaneResult
    {
        public ControlPlaneDecisionVerdict Decision { get; init; } = ControlPlaneDecisionVerdict.Deny;

        public required string ReasonCode { get; init; }

        public required string Message { get; init; }

        public string? CorrelationId { get; init; }

        public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }
}