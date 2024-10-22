using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine
{

    public class StateEnteredEventArgs(State fromState, string symbol)
    {
        public State FromState { get; } = fromState;
        public string Symbol { get; } = symbol;
    }

    public class StateLeaveEventArgs(State toState, string symbol)
    {
        public State ToState { get; } = toState;
        public string Symbol { get; } = symbol;
    }

    public class StateChangedEventArgs(State fromState, string symbol, State toState)
    {
        public State FromState { get; } = fromState;
        public string Symbol { get; } = symbol;
        public State ToState { get; } = toState;
    }

}
