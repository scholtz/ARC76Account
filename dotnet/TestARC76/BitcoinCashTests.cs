using NBitcoin;
using NBitcoin.Altcoins;

namespace TestARC76
{
    public class BitcoinCashTests
    {
        private static readonly Network Network = BCash.Instance.Mainnet;

        [Test]
        public void GetPasswordAccountTest()
        {
            var key = ARC76Account.BitcoinCash.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network);
            Assert.That(address.ToString(), Is.EqualTo("bitcoincash:qr34393e7nat5476magek26aul9q3w3lxc4hph4ljz"));
            Assert.That(key.GetWif(Network).ToString(), Is.EqualTo("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"));
        }
        [Test]
        public void GetEmailPasswordAccountTest()
        {
            var key = ARC76Account.BitcoinCash.ARC76.GetEmailAccount("email@example.com", "12345678901234567890123456789012345678901234567890");
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network);
            Assert.That(address.ToString(), Is.EqualTo("bitcoincash:qp6fcyedrugd3l2d4yvzptn9p00hdqup5g7zx5pytw"));
            Assert.That(key.GetWif(Network).ToString(), Is.EqualTo("L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL"));
        }
        [Test]
        public void GetPasswordAccountSlot1Test()
        {
            // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
            // (Medium finding: no test previously exercised any slot other than the default).
            var key = ARC76Account.BitcoinCash.ARC76.GetAccount("12345678901234567890123456789012345678901234567890", 1);
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network);
            Assert.That(address.ToString(), Is.EqualTo("bitcoincash:qpllmp6d3d9ewjya3s5s08ssvmnhdr7heuv0dvkuds"));
            Assert.That(key.GetWif(Network).ToString(), Is.EqualTo("KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA"));
        }

        // Extends the literal tests above with the cross-language vectors in
        // test-vectors/arc76-vectors.json (shared with Python/TypeScript).
        [TestCaseSource(typeof(Vectors), nameof(Vectors.BitcoinCashCases))]
        public void MatchesSharedVector(Vector v)
        {
            var key = v.Email is null
                ? ARC76Account.BitcoinCash.ARC76.GetAccount(v.Password, v.Slot)
                : ARC76Account.BitcoinCash.ARC76.GetEmailAccount(v.Email, v.Password, v.Slot);
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network);
            Assert.That(address.ToString(), Is.EqualTo(v.Str("address")));
            Assert.That(key.GetWif(Network).ToString(), Is.EqualTo(v.Str("wif")));
        }
    }
}
