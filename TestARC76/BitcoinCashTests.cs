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
    }
}
