using Moq;
using NBitcoin;

namespace MasternodeSetupTool.Tests
{
    public class Tests
    {
        private NetworkType networkType;

        private Mock<IRegistrationService> registrationService;
        private Mock<IStateHandler> stateHandler;

        private IStateHolder stateHolder;
        private StateMachine stateMachine;

        [SetUp]
        public void Setup()
        {
            this.networkType = NetworkType.Testnet;

            this.registrationService = new Mock<IRegistrationService>();
            this.stateHandler = new Mock<IStateHandler>();

            this.stateHolder = new DefaultStateHolder();
            this.stateMachine = new StateMachine(networkType, stateHandler.Object, registrationService.Object, stateHolder);
        }

        [Test]
        public async Task ShouldAskForEULA()
        {
            this.stateHolder.NextState = StateMachine.State.SetupMasterNode_Eula;

            this.stateHandler.Setup(h => h.OnAskForEULA().Result).Returns(true);

            await this.stateMachine.TickAsync();

            Assert.That(this.stateHolder.NextState, Is.Not.Null);

            Assert.That(this.stateHolder.NextState, Is.Not.EqualTo(StateMachine.State.SetupMasterNode_Eula));
            Assert.That(this.stateHolder.NextState, Is.EqualTo(StateMachine.State.Setup_KeyPresent));
        }
    }
}