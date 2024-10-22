using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sahner.AutomatedStateMachine.Exceptions;

namespace Sahner.AutomatedStateMachine
{
    public class State
    {

        #region Header

        #region TypeDefinitions
        /// <summary>
        /// Used to automate the behavior of state machines.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>An input symbol that is read before other ones may.</returns>
        public delegate Task<string> StateAutomationFunction(params object[]? parameters);
        #endregion

        #region Events
        public delegate void StateEnteredHandler(State sender, StateEnteredEventArgs e);
        /// <summary>
        /// Gets raised when the state was entered and before the automation function is called.
        /// Throwing inside a handler of this event will let the <see cref="AsyncStateMachine.ReadSymbolAsync(string, object[])"/>
        /// function throw the corresponding exception.
        /// If the following state is transient, its automation function will be executed first and the exception is thrown afterwards.
        /// </summary>
        public event StateEnteredHandler? Entered;
        internal void OnEntered(State fromState, string symbol)
        {
            Entered?.Invoke(this, new StateEnteredEventArgs(fromState, symbol));
        }

        public delegate void StateLeaveHandler(State sender, StateLeaveEventArgs e);
        /// <summary>
        /// Gets raised when the state was left.
        /// Throwing inside a handler of this event will let the <see cref="AsyncStateMachine.ReadSymbolAsync(string, object[])"/>
        /// function throw the corresponding exception.
        /// If the following state is transient, its automation function will be executed first and the exception is thrown afterwards.
        /// </summary>
        public event StateLeaveHandler? Leave;
        internal void OnLeave(State toState, string symbol)
        {
            Leave?.Invoke(this, new StateLeaveEventArgs(toState, symbol));
        }


        #endregion

        #region Properties
        /// <summary>
        /// The unique identifier of this state within a state machine.
        /// </summary>
        public string Identifier { get; }
        /// <summary>
        /// The list of transitions possible when in this state.
        /// </summary>
        private Dictionary<string, Transition> _transitions;
        /// <summary>
        /// An immutable list of transitions that are possible when this state is the current state.
        /// </summary>
        public ImmutableDictionary<string, Transition> Transitions { get { return _transitions.ToImmutableDictionary(); } }
        /// <summary>
        /// When not null, this function is called each time after the state was entered.
        /// The result is then interpreted as sequel input symbol and
        /// read by the state machine before any other input.
        /// Exceptions should be wrapped in a <see cref="TransientStateException"/> 
        /// or the property <see cref="AsyncStateMachine.DefaultErrorSymbol"/> should be set.
        /// </summary>
        public StateAutomationFunction? AutomationFunction { get; }
        /// <summary>
        /// Indicates if this state has an automation function.
        /// </summary>
        public bool IsTransient { get { return AutomationFunction != null; } }
        #endregion

        #region PrivateFields

        #endregion

        #region Initialization
        internal State(string identifier, StateAutomationFunction? automationFunction = null)
        {
            Identifier = identifier;
            _transitions = [];
            AutomationFunction = automationFunction;
        }

        internal void SetTransitions(Dictionary<string, Transition> transitions)
        {
            _transitions = transitions;
        }
        #endregion

        #endregion

        #region PrivateFunctions

        #endregion

        #region PublicFunctions

        #endregion

    }
}
