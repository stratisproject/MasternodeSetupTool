using static MasternodeSetupTool.StateMachine;

namespace MasternodeSetupTool;

public interface IStateHolder
{
    State CurrentState { get; }
    State? NextState { get; set; }

    void SwitchToNextState();
}