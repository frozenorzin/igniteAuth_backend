using IgniteAuth.Data;
using IgniteAuth.Interfaces;
using IgniteAuth.Utilities;

namespace IgniteAuth.Processors.Validation;

public sealed class IntentValidator : IIntentValidator
{
    private readonly IntentHashes _intentHashes;
    private readonly byte[] _secretKey;

    public IntentValidator(IntentHashes intentHashes, byte[] secretKey)
    {
        _intentHashes = intentHashes;
        _secretKey = secretKey;
    }

    public bool IsValidIntent(
        string subSystem,
        string plainIntent)
    {
        if (string.IsNullOrWhiteSpace(subSystem) ||
            string.IsNullOrWhiteSpace(plainIntent))
        {
            return false;
        }

        var subsystemData =
            _intentHashes.GetSubsystem(subSystem);

        if (subsystemData is null)
        {
            return false;
        }

        // Hash the plain intent and compare against stored hashes
        var intentHash = IntentHasher.ComputeHmacSha256Hex(plainIntent, _secretKey);

        return subsystemData.Intents
            .Contains(intentHash, StringComparer.Ordinal);
    }
}