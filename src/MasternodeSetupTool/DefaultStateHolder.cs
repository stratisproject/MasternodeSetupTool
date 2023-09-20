using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasternodeSetupTool
{
    public class DefaultStateHolder : IStateHolder
    {
        public StateMachine.State currentState_;
        public StateMachine.State? nextState_;

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
                this.CurrentState = (StateMachine.State)nextState;
                this.NextState = null;
            }
        }
    }
}
