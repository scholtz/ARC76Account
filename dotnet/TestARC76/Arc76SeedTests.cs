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
        public void DeriveIsCultureInvariantForNegativeSlots()
        {
            // Regression test - see audits/2026-08-04-audit-report.md (High finding): the
            // slot was previously interpolated with the current culture's number formatting,
            // so under cultures whose negative sign is not the ASCII '-' (sv-SE/fi-FI/lt-LT
            // use U+2212; fa-IR/ar-SA prepend an invisible directional mark) a negative slot
            // derived a different seed than the same inputs under en-US or in TypeScript.
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                var invariant = Arc76Seed.Derive(Password, -1);
                var invariantEmail = Arc76Seed.Derive(Email, Password, -1);
                foreach (var culture in new[] { "sv-SE", "fa-IR", "ar-SA", "fi-FI", "lt-LT" })
                {
                    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
                    Assert.That(Arc76Seed.Derive(Password, -1), Is.EqualTo(invariant), $"culture {culture}");
                    Assert.That(Arc76Seed.Derive(Email, Password, -1), Is.EqualTo(invariantEmail), $"culture {culture} (email)");
                }
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
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
