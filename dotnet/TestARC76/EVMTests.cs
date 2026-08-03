namespace TestARC76
{
    public class EVMTests
    {

        [Test]
        public void GetPasswordAccountTest()
        {
            var account = ARC76Account.Ethereum.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            Assert.That(account.GetPublicAddress(), Is.EqualTo("0x6fD4983EaC3F958d1BF2009be4FCc0E8792A8AC6"));
            Assert.That(account.GetPrivateKey(), Is.EqualTo("0x9365ff3d966fdb3b8765e27007677440e67382ed4503c2d00a39638548e13c22"));
        }
        [Test]
        public void GetEmailPasswordAccountTest()
        {
            var account = ARC76Account.Ethereum.ARC76.GetEmailAccount("email@example.com", "12345678901234567890123456789012345678901234567890");
            Assert.That(account.GetPublicAddress(), Is.EqualTo("0x0b6CEF2E7844F27019Fce92205F5FedF684c6926"));
            Assert.That(account.GetPrivateKey(), Is.EqualTo("0xe1078b9af5d2b58abc819135f6b8f358a3a36dd04d4707eb51f481d766a86915"));
        }
        [Test]
        public void GetPasswordAccountSlot1Test()
        {
            // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
            // (Medium finding: no test previously exercised any slot other than the default).
            var account = ARC76Account.Ethereum.ARC76.GetAccount("12345678901234567890123456789012345678901234567890", 1);
            Assert.That(account.GetPublicAddress(), Is.EqualTo("0x90a4b3436c28dBD111130683bacd4948B06A4544"));
            Assert.That(account.GetPrivateKey(), Is.EqualTo("0x4b698752e1f979bd51179faec7e59dcf7552d2724dd5a0b7a024d0ca536e4130"));
        }
    }
}
