using ARC76Account.Core;

namespace TestARC76
{
    public class Arc76SeedTests
    {
        private const string Password = "12345678901234567890123456789012345678901234567890";
        private const string Email = "email@example.com";

        [Test]
        public void DeriveIsDeterministic()
        {
            var a = Arc76Seed.Derive(Password, 0);
            var b = Arc76Seed.Derive(Password, 0);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Length, Is.EqualTo(32));
        }

        [Test]
        public void DeriveDiffersPerSlot()
        {
            var slot0 = Arc76Seed.Derive(Password, 0);
            var slot1 = Arc76Seed.Derive(Password, 1);
            Assert.That(slot0, Is.Not.EqualTo(slot1));
        }

        [Test]
        public void DeriveWithEmailDiffersFromPasswordOnly()
        {
            var withoutEmail = Arc76Seed.Derive(Password, 0);
            var withEmail = Arc76Seed.Derive(Email, Password, 0);
            Assert.That(withoutEmail, Is.Not.EqualTo(withEmail));
        }

        [Test]
        public void DeriveRejectsShortPassword()
        {
            // Regression test - see audits/2026-08-03-audit-report.md (Medium finding: .NET
            // previously enforced no minimum password length at all, unlike the TypeScript
            // package's 16-character floor).
            Assert.Throws<ArgumentException>(() => Arc76Seed.Derive("short", 0));
            Assert.Throws<ArgumentException>(() => Arc76Seed.Derive(Email, "short", 0));
        }

        [Test]
        public void DeriveWithNonAsciiEmailMatchesUtf8Encoding()
        {
            // Regression test for the Critical finding in audits/2026-08-03-audit-report.md:
            // the email-salted overload previously encoded the salt as ASCII, silently
            // mangling non-ASCII characters (e.g. 'é' -> '?') while the input string correctly
            // used UTF-8 - producing a seed that disagreed with the ARC-0076 reference
            // implementation and with this project's own TypeScript package for the same
            // inputs. Two different non-ASCII emails must still derive different seeds (the
            // bug did not cause collisions, since the input string was always correct - only
            // determinism/spec-compliance was broken).
            var cafeAccent1 = Arc76Seed.Derive("café@example.com", Password, 0);
            var cafeAccent2 = Arc76Seed.Derive("cafè@example.com", Password, 0);
            Assert.That(cafeAccent1, Is.Not.EqualTo(cafeAccent2));
        }
    }
}
