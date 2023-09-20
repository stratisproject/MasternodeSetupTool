using Moq;
using NBitcoin;

namespace MasternodeSetupTool.Tests
{
    public class Tests
    {
        private NetworkType networkType;
        private StateMachine stateMachine;
        private Mock<IStateHandler> stateHandler;

        [SetUp]
        public void Setup()
        {
            networkType = NetworkType.Testnet;
            stateHandler = new Mock<IStateHandler>();
            stateMachine = new StateMachine(networkType, stateHandler.Object);
        }

        [Test]
        public async Task SetupNode()
        {

        }
    }
}