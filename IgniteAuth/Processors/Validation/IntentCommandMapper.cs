using IgniteAuth.Data;
using IgniteAuth.Interfaces;
using IgniteAuth.Utilities;

namespace IgniteAuth.Processors.Validation;

public sealed class IntentCommandMapper : IIntentCommandMapper
{
    private readonly IntentCommandMapping _mappingData;
    private readonly byte[] _secretKey;

    public IntentCommandMapper(IntentCommandMapping mappingData, byte[] secretKey)
    {
        _mappingData = mappingData;
        _secretKey = secretKey;
    }

    public bool IsValidMapping(
        string subSystem,
        string plainIntent,
        string command)
    {
        // 0. Basic input validation
        if (string.IsNullOrWhiteSpace(subSystem) ||
            string.IsNullOrWhiteSpace(plainIntent) ||
            string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        // Hash the plain intent
        var intentHash = IntentHasher.ComputeHmacSha256Hex(plainIntent, _secretKey);

        // 1. Identify subsystem
        var subsystemMapping =
            _mappingData.GetSubsystem(subSystem);

        if (subsystemMapping is null)
        {
            return false;
        }

        // 2. Find the intent mapping for this subsystem
        var intentMapping =
            subsystemMapping.IntentMappings
                .FirstOrDefault(x =>
                    string.Equals(
                        x.Intent,
                        intentHash,
                        StringComparison.Ordinal));

        if (intentMapping is null)
        {
            return false;
        }

        // 3. Verify command is permitted for this intent
        return intentMapping.Commands
            .Contains(command, StringComparer.Ordinal);
    }
}