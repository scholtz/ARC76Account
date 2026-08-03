using Algorand.Algod.Model.Transactions;
using Algorand;
using ARC76Account.Utils;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestARC76
{
    public class ARC76SignerTests
    {
        private ARC76Signer signer;
        private Algorand.Algod.Model.Account account;
        [SetUp]
        public void Setup()
        {
            var options = GetOptionsMonitor(new ARC76Account.Model.Config.ARC76Config("12345678901234567890123456789012345678901234567890"));
            account = ARC76Account.Algorand.ARC76.GetAccount("12345678901234567890123456789012345678901234567890");
            signer = new ARC76Signer(options);
        }

        [TearDown]
        public void TearDown()
        {
            account.Dispose();
        }
        private IOptionsMonitor<ARC76Account.Model.Config.ARC76Config> GetOptionsMonitor(ARC76Account.Model.Config.ARC76Config appConfig)
        {
            var optionsMonitorMock = new Mock<IOptionsMonitor<ARC76Account.Model.Config.ARC76Config>>();
            optionsMonitorMock.Setup(o => o.CurrentValue).Returns(appConfig);
            return optionsMonitorMock.Object;
        }

        [Test]
        public void SigningTest()
        {
            Assert.That(signer.getAddress(), Is.EqualTo(account.Address));
            var transParams = new Algorand.Algod.Model.TransactionParametersResponse() { ConsensusVersion = "https://github.com/algorandfoundation/specs/tree/abd3d4823c6f77349fc04c3af7b1e99fe4df699f", Fee = 0, GenesisHash = Convert.FromBase64String("wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8="), GenesisId = "mainnet-v1.0", LastRound = 1, MinFee = 1000 };
            var tx = PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account.Address, account.Address, 0, "", transParams);
            var signed = signer.Sign(new Transaction[] { tx });
            Assert.That(signed.Count(), Is.EqualTo(1));
            Assert.That(signed.First().Tx, Is.EqualTo(tx));
            Assert.That(Convert.ToBase64String(signed.First().Sig.Bytes), Is.EqualTo("JP3gbSao+5Vbt3fyDO0dJbEXGtBWVohf1cwJH1YLbrli3mPQi5H+V4Um2GtixZfUkNxgm/bXP1P/NyF6H9JlDA=="));

        }
    }
}
