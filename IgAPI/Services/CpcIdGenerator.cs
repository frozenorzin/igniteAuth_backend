using IgAPI.Services.Interfaces;

namespace IgAPI.Services;

/// <summary>
/// Issues a process-local correlation ID for each control-plane call.
/// </summary>
public sealed class CpcIdGenerator : ICpcIdGenerator
{
    private int _lastIssued;

    public int Issue()
    {
        var cpcId = Interlocked.Increment(ref _lastIssued);

        if (cpcId <= 0)
            throw new InvalidOperationException("The CPC ID range has been exhausted.");

        return cpcId;
    }
}
