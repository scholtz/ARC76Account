using System.Security.Cryptography;
using System.Text;

namespace ARC76Account.Core
{
    /// <summary>
    /// Shared ARC76 seed derivation (PBKDF2-HMAC-SHA256) used by every chain-specific
    /// ARC76 implementation. Do not change the salt/input format, iteration count or hash
    /// size here - it is fixed by the ARC76 spec and any change breaks every derived
    /// address for every chain, for every existing user.
    /// </summary>
    internal static class Arc76Seed
    {
        internal const int HashSize = 32;
        private const int Iterations = 999999;

        internal static byte[] Derive(string password, int slot)
        {
            byte[] salt = Encoding.ASCII.GetBytes($"ARC-0076-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{password}-{slot}-PBKDF2-999999");
            Rfc2898DeriveBytes pbkdf2 = new(input, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }

        internal static byte[] Derive(string email, string password, int slot)
        {
            byte[] salt = Encoding.ASCII.GetBytes($"ARC-0076-{email}-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{email}-{password}-{slot}-PBKDF2-999999");
            Rfc2898DeriveBytes pbkdf2 = new(input, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }
    }
}
