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
    }
}
