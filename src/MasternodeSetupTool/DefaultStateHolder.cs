namespace MasternodeSetupTool
{
    public class DefaultStateHolder : IStateHolder
    {
        public StateMachine.State currentState_ = StateMachine.State.Begin;
        public StateMachine.State? nextState_;

        private bool repeatOnEndState;

        public DefaultStateHolder(bool repeatOnEndState = true)
        {
            this.repeatOnEndState = repeatOnEndState;
        }

        public StateMachine.State CurrentState
        {
            get
            {
                return this.currentState_;
            }

            private set
            {
                this.currentState_ = value;
            }
        }

        public StateMachine.State? NextState
        {
            get
            {
                return this.nextState_;
            }

            set
            {
                this.nextState_ = value;
            }
        }

        public void SwitchToNextState()
        {
            StateMachine.State? nextState = this.NextState;
            if (nextState != null)
            {
                if (nextState == StateMachine.State.End && this.repeatOnEndState) 
                {
                    nextState = StateMachine.State.Begin;
                }
                this.CurrentState = (StateMachine.State)nextState;
                this.NextState = null;
            }
        }
    }
}
