using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IgniteAuth.Utilities
{
    public static class IntentHasher
    {
        // Normalize intent to ensure deterministic input
        public static string Canonicalize(string intent)
        {
            if (intent is null)
                return string.Empty;

            var normalized = Regex.Replace(
                intent.Trim(),
                @"\s+",
                " "
            );

            return normalized.ToLowerInvariant();
        }

        // Compute deterministic HMAC-SHA256 hash
        public static string ComputeHmacSha256Hex(
            string intent,
            byte[] secretKey)
        {
            var canonical = Canonicalize(intent);

            using var hmac = new HMACSHA256(secretKey);

            var bytes = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(canonical)
            );

            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // Compare an incoming intent against a registered hash
        public static bool VerifyIntent(
            string intent,
            string expectedHash,
            byte[] secretKey)
        {
            var computedHash =
                ComputeHmacSha256Hex(intent, secretKey);

            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(computedHash),
                Convert.FromHexString(expectedHash)
            );
        }
    }
}