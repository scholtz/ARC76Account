using NBitcoin;

namespace TestARC76
{
    public class BitcoinTests
    {
        [Test]
        public void GetPasswordAccountTest()
        {
            var key = ARC76Account.Bitcoin.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
            Assert.That(address.ToString(), Is.EqualTo("bc1qudvfvw05l2a90kkl2xdjkh08egyt50ekkprkas"));
            Assert.That(key.GetWif(Network.Main).ToString(), Is.EqualTo("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"));
        }
        [Test]
        public void GetEmailPasswordAccountTest()
        {
            var key = ARC76Account.Bitcoin.ARC76.GetEmailAccount("email@example.com", "12345678901234567890123456789012345678901234567890");
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
            Assert.That(address.ToString(), Is.EqualTo("bc1qwjwpxtglzrv06ndfrqs2uegtmamg8qdzy3mghk"));
            Assert.That(key.GetWif(Network.Main).ToString(), Is.EqualTo("L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL"));
        }
        [Test]
        public void GetPasswordAccountSlot1Test()
        {
            // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
            // (Medium finding: no test previously exercised any slot other than the default).
            var key = ARC76Account.Bitcoin.ARC76.GetAccount("12345678901234567890123456789012345678901234567890", 1);
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
            Assert.That(address.ToString(), Is.EqualTo("bc1q0l7cwnvtfwt538vv9yreuyrxuamgl470fkl8j0"));
            Assert.That(key.GetWif(Network.Main).ToString(), Is.EqualTo("KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA"));
        }

        // Extends the literal tests above with the cross-language vectors in
        // test-vectors/arc76-vectors.json (shared with Python/TypeScript).
        [TestCaseSource(typeof(Vectors), nameof(Vectors.BitcoinCases))]
        public void MatchesSharedVector(Vector v)
        {
            var key = v.Email is null
                ? ARC76Account.Bitcoin.ARC76.GetAccount(v.Password, v.Slot)
                : ARC76Account.Bitcoin.ARC76.GetEmailAccount(v.Email, v.Password, v.Slot);
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
            Assert.That(address.ToString(), Is.EqualTo(v.Str("address")));
            Assert.That(key.GetWif(Network.Main).ToString(), Is.EqualTo(v.Str("wif")));
        }
    }
}
