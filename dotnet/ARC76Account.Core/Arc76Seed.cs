using System.Security.Cryptography;
using System.Text;

namespace ARC76Account.Core
{
    /// <summary>
    /// Shared ARC76 seed derivation (PBKDF2-HMAC-SHA256) used by every chain-specific
    /// ARC76 package (ARC76Account.Algorand, ARC76Account.Ethereum, ...). Public because each
    /// chain lives in its own assembly/package and needs to call this from outside
    /// ARC76Account.Core. Do not change the salt/input format, iteration count or hash size
    /// here - it is fixed by the ARC76 spec and any change breaks every derived address for
    /// every chain, for every existing user.
    /// </summary>
    public static class Arc76Seed
    {
        public const int HashSize = 32;
        private const int Iterations = 999999;

        public static byte[] Derive(string password, int slot)
        {
            byte[] salt = Encoding.ASCII.GetBytes($"ARC-0076-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{password}-{slot}-PBKDF2-999999");
            return Rfc2898DeriveBytes.Pbkdf2(input, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        }

        public static byte[] Derive(string email, string password, int slot)
        {
            byte[] salt = Encoding.ASCII.GetBytes($"ARC-0076-{email}-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{email}-{password}-{slot}-PBKDF2-999999");
            return Rfc2898DeriveBytes.Pbkdf2(input, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        }
    }
}
