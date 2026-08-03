namespace TestARC76
{
    public class SolanaTests
    {
        [Test]
        public void GetPasswordAccountTest()
        {
            var account = ARC76Account.Solana.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            Assert.That(account.PublicKey.ToString(), Is.EqualTo("4NZNrQAS9gLtqcutnCRfyy63M7XysEzgPP4hFGrutsne"));
            Assert.That(account.PrivateKey.ToString(), Is.EqualTo("3wvbZFR7njig8pAcSaHtzAVXYe779HjwrxCYiXRJS7shfWV6N3bNNynRijtz5WccRfDfhcHF5VTyq8yCv5HMAf34"));
        }
        [Test]
        public void GetEmailPasswordAccountTest()
        {
            var account = ARC76Account.Solana.ARC76.GetEmailAccount("email@example.com", "12345678901234567890123456789012345678901234567890");
            Assert.That(account.PublicKey.ToString(), Is.EqualTo("GcsDHS3b2hSUAWb2sU3UYvNWjudn4zas2h969q8C6P8r"));
            Assert.That(account.PrivateKey.ToString(), Is.EqualTo("5Vwqas2RYTc5iq4eN4cvx79AfZPY7ygvLt2AtQ5eMdzT9ZgeLDu57m2tpnyw7Z53TtZknyEs1qUMvfwhUkVeDLhC"));
        }
        [Test]
        public void GetPasswordAccountSlot1Test()
        {
            // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
            // (Medium finding: no test previously exercised any slot other than the default).
            var account = ARC76Account.Solana.ARC76.GetAccount("12345678901234567890123456789012345678901234567890", 1);
            Assert.That(account.PublicKey.ToString(), Is.EqualTo("D9cyFwbaGdDd1Lo3MJbL6VULN8r4p3c2jrWURTUV7kB"));
            Assert.That(account.PrivateKey.ToString(), Is.EqualTo("2WT1aN7vJse9uykpD6kgZs5zUovYKUxDKDEswPwEa9JnLgrEKseRQpxatFXfDHs9UohZB9A7qGRRZdBGzkWSi8K9"));
        }
    }
}
