namespace TestARC76
{
    public class AVMTests
    {
        [Test]
        public void GetPasswordAccountTest()
        {
            var account = ARC76Account.Algorand.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("GIMN3RFDRRWFD7KIFSVRMXC4JXYD5ZXKT7KSN57OG4RAZPPC3L3YDM46NQ"));
        }
        [Test]
        public void GetEmailPasswordAccountTest()
        {
            var account = ARC76Account.Algorand.ARC76.GetEmailAccount("email@example.com", "12345678901234567890123456789012345678901234567890");
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("5AHWQJ5D52K4GRW4JWQ5GMR53F7PDSJEGT4PXVFSBQYE7VXDVG3WSPWSBM"));
        }
    }
}
