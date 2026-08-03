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
        private const int MinPasswordLength = 16;

        public static byte[] Derive(string password, int slot)
        {
            AssertPassword(password);
            // Salt has no user-controlled characters here (only the numeric slot), so ASCII vs
            // UTF-8 makes no difference - kept as UTF8 anyway for consistency with the
            // email-salted overload below and with every other ARC76 implementation.
            byte[] salt = Encoding.UTF8.GetBytes($"ARC-0076-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{password}-{slot}-PBKDF2-999999");
            return Rfc2898DeriveBytes.Pbkdf2(input, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        }

        public static byte[] Derive(string email, string password, int slot)
        {
            AssertPassword(password);
            // MUST be UTF8, not ASCII: the ARC-0076 reference implementation (and this
            // project's own TypeScript package) encodes the salt as UTF-8 throughout. Using
            // ASCII here previously silently mangled any non-ASCII character in `email` to '?'
            // in the salt only (not the input below), producing a seed that disagreed with
            // every other ARC76 implementation for the same inputs. See audits/2026-08-03-audit-report.md
            // (Critical finding) - do not revert this to Encoding.ASCII.
            byte[] salt = Encoding.UTF8.GetBytes($"ARC-0076-{email}-{slot}-PBKDF2-999999");
            byte[] input = Encoding.UTF8.GetBytes($"ARC-0076-{email}-{password}-{slot}-PBKDF2-999999");
            return Rfc2898DeriveBytes.Pbkdf2(input, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        }

        private static void AssertPassword(string password)
        {
            if (password is null || password.Length < MinPasswordLength)
            {
                throw new ArgumentException($"Password must be at least {MinPasswordLength} characters long", nameof(password));
            }
        }
    }
}
