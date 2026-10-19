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
        [Test]
        public void GetPasswordAccountSlot1Test()
        {
            // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
            // (Medium finding: no test previously exercised any slot other than the default).
            var account = ARC76Account.Algorand.ARC76.GetAccount("12345678901234567890123456789012345678901234567890", 1);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("AMOLO4ICDKCOLFVJXAVIQ23TA27P3LW4AHJP54LLNV4JPIAFKBYLH3SGVQ"));
        }

        private const string Password = "12345678901234567890123456789012345678901234567890";

        [Test]
        public void GetFalconAccountKnownAnswerTest()
        {
            var account = ARC76Account.Algorand.ARC76.GetFalconAccount(Password);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("BGIJDJONAAYPYPJSZX6EWMH24VMU7CDAOUNCPCIXQ4KYYJG3QMXWNVWOWE"));
        }
        [Test]
        public void GetEmailFalconAccountKnownAnswerTest()
        {
            var account = ARC76Account.Algorand.ARC76.GetEmailFalconAccount("email@example.com", Password);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("TD5NFW5MEPGAT4S2AIN6XK5DIPJQMCOVN3272HJUJPPG3TB52MNFESSRXA"));
        }
        [Test]
        public void GetFalconAccountSlot1KnownAnswerTest()
        {
            var account = ARC76Account.Algorand.ARC76.GetFalconAccount(Password, 1);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo("CV2LVHMWDQ625SKQOWQHYZN453MZ22R3V2DO5HFU6UDUZVDKGZJES4HFC4"));
        }
        [Test]
        public void FalconAccountIsDeterministicAndDistinctFromEd25519()
        {
            var a = ARC76Account.Algorand.ARC76.GetFalconAccount(Password);
            var b = ARC76Account.Algorand.ARC76.GetFalconAccount(Password);
            var ed = ARC76Account.Algorand.ARC76.GetAccount(Password);
            Assert.That(a.Address.EncodeAsString(), Is.EqualTo(b.Address.EncodeAsString()));
            Assert.That(a.Address.EncodeAsString(), Is.Not.EqualTo(ed.Address.EncodeAsString()));
            Assert.That(ARC76Account.Algorand.ARC76.GetFalconAccount(Password, 1).Address.EncodeAsString(), Is.Not.EqualTo(a.Address.EncodeAsString()));
            Assert.That(ARC76Account.Algorand.ARC76.GetEmailFalconAccount("a@b.c", Password).Address.EncodeAsString(), Is.Not.EqualTo(a.Address.EncodeAsString()));
        }
        [Test]
        public void FalconAccountMnemonicRoundTripsAndSigns()
        {
            var account = ARC76Account.Algorand.ARC76.GetFalconAccount(Password);
            var restored = global::Algorand.Algod.Model.FalconAccount.FromMnemonic(account.ToMnemonic());
            Assert.That(restored.Address.EncodeAsString(), Is.EqualTo(account.Address.EncodeAsString()));
            var msg = new byte[] { 1, 2, 3 };
            Assert.That(account.SignPQRawBytes(msg).Verify(msg), Is.True);
        }
    }
}
