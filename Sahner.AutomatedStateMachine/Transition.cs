using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine
{
    public class Transition(string symbol, State successorState)
    {
        /// <summary>
        /// The unique identifier of this transition within a states adjacency list.
        /// </summary>
        public string Symbol { get; } = symbol;
        /// <summary>
        /// The successor state when firing this transition.
        /// </summary>
        public State SuccessorState { get; } = successorState;
    }
}
