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
        // Cross-language vectors covering several canonical salts (0-6) - the salt search depends on an
        // Ed25519 point test that is re-implemented in the Python and TypeScript packages.
        [TestCase(6, "SQUKPUF5WNY2Y5VWPKEDVM2KGH2LPML6PIQMOFIRBTMHJTEC4BXLEPM4XI", 4)]
        [TestCase(7, "LWTOIX3ZCF4UDNFTS4A5S7436MT3QTFIFVSLQGVLODOYTVT6BVUUKGI2NU", 1)]
        [TestCase(9, "U6MYURHZFMNYLTUTOLBAHD7LNMT4WQ572GUHZOW7RJFGD6CYVDJNHDY2AU", 6)]
        [TestCase(18, "QR3WQTKMSN6IW7SIM3OD3WXA4G57AG3YOBKTKH4R2UAU32KABR2ZA4LMD4", 2)]
        public void GetFalconAccountParityVectors(int slot, string address, int salt)
        {
            var account = ARC76Account.Algorand.ARC76.GetFalconAccount("parity-password-" + slot, slot);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo(address));
            Assert.That((int)account.Salt, Is.EqualTo(salt));
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

        // Extends the literal tests above with the cross-language vectors in
        // test-vectors/arc76-vectors.json (shared with Python/TypeScript).
        [TestCaseSource(typeof(Vectors), nameof(Vectors.AlgorandCases))]
        public void MatchesSharedVector(Vector v)
        {
            var account = v.Email is null
                ? ARC76Account.Algorand.ARC76.GetAccount(v.Password, v.Slot)
                : ARC76Account.Algorand.ARC76.GetEmailAccount(v.Email, v.Password, v.Slot);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo(v.Str("address")));
        }

        [TestCaseSource(typeof(Vectors), nameof(Vectors.FalconCases))]
        public void FalconMatchesSharedVector(Vector v)
        {
            var account = v.Email is null
                ? ARC76Account.Algorand.ARC76.GetFalconAccount(v.Password, v.Slot)
                : ARC76Account.Algorand.ARC76.GetEmailFalconAccount(v.Email, v.Password, v.Slot);
            Assert.That(account.Address.EncodeAsString(), Is.EqualTo(v.Str("address")));
            if (v.Raw.TryGetProperty("salt", out var salt))
                Assert.That((int)account.Salt, Is.EqualTo(salt.GetInt32()));
        }
    }
}
