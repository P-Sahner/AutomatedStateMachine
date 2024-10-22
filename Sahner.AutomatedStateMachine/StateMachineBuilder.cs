using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sahner.AutomatedStateMachine.State;
using Sahner.AutomatedStateMachine.Exceptions;
using System.Data;


namespace Sahner.AutomatedStateMachine
{
    public class StateMachineBuilder
    {

        #region Header

        #region TypeDefinitions

        #endregion

        #region Events

        #endregion

        #region Properties

        #endregion

        #region PrivateFields
        private readonly List<Tuple<string, StateAutomationFunction?>> states = [];
        private readonly List<Tuple<string, string, string>> transitions = [];
        #endregion

        #region Initialization

        #endregion

        #endregion

        #region PrivateFunctions

        #endregion

        #region PublicFunctions
        /// <summary>
        /// Adds a new state to the state machine using the given unique identifier.
        /// If given a non-null automation function, the state becomes transient.
        /// <br/>
        /// <example>
        /// Example usage:
        /// <br/>
        /// Add a simple state:
        /// <code>
        /// StateMachineBuilder builder = new();
        /// builder.AddState("initial");
        /// </code>
        /// Add a state with an automation function:
        /// <code>
        /// StateMachineBuilder builder = new();
        /// builder.AddState("busy", async (object[]? parameters) =>
        /// {
        ///     //Some work
        ///     await Task.Delay(60););
        ///     return "success";
        /// });
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="identifier">The states identifier. Must be unique per state machine.</param>
        /// <param name="automationFunction"></param>
        public StateMachineBuilder AddState(string identifier, StateAutomationFunction? automationFunction = null)
        {
            states.Add(new Tuple<string, StateAutomationFunction?>(identifier, automationFunction));
            return this;
        }
        /// <summary>
        /// Adds non-transient states to the state machine using the given unique identifiers.
        /// </summary>
        /// <param name="identifiers"></param>
        public StateMachineBuilder AddStates(params string[] identifiers)
        {
            states.AddRange(identifiers.Select(identifier => new Tuple<string, StateAutomationFunction?>(identifier,null)));
            return this;
        }

        /// <summary>
        /// Adds states to the state machine using the given unique identifiers.
        /// </summary>
        /// <param name="identifiers"></param>
        public StateMachineBuilder AddStates(params (string identifier, StateAutomationFunction? automationFunction)[] states)
        {
            this.states.AddRange(states.Select(state => new Tuple<string, StateAutomationFunction?>(state.identifier, state.automationFunction)));
            return this;
        }

        /// <summary>
        /// Adds transitions to the state machine. Their symbols have to be unique for each state.
        /// </summary>
        public StateMachineBuilder AddTransitions(params (string sourceStateIdentifier, string symbol, string targetStateIdentifier)[] transitions)
        {
            this.transitions.AddRange(
                transitions.Select(transition =>
                    new Tuple<string, string, string>(transition.sourceStateIdentifier, transition.symbol, transition.targetStateIdentifier)
                    )
                );
            return this;
        }

        /// <summary>
        /// Adds a new transition to the state machine. Its symbol has to be unique for each state.
        /// </summary>
        public StateMachineBuilder AddTransition(string sourceStateIdentifier, string symbol, string targetStateIdentifier)
        {
            transitions.Add(new Tuple<string, string, string>(sourceStateIdentifier, symbol, targetStateIdentifier));
            return this;
        }
        /// <summary>
        /// Builds an instance of the <see cref="AsyncStateMachine"/> given
        /// the previously defined states and transitions.
        /// A validation regarding duplicates is performed
        /// and an exception thrown if duplicate identifiers were found.
        /// </summary>
        /// <param name="initialStateIdentifier">
        /// The initial states identifier.
        /// If not added to the list of states an exception is thrown.
        /// </param>
        /// <param name="defaultErrorSymbol">
        /// See <see cref="AsyncStateMachine.DefaultErrorSymbol"/> for more details.
        /// </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public AsyncStateMachine BuildAsyncStateMachine(string initialStateIdentifier, string defaultErrorSymbol = "")
        {
            //Instantiate states from identifiers.
            Dictionary<string, State> newStates;
            try
            {
                newStates  = states.Select(stateTuple => new State(stateTuple.Item1, stateTuple.Item2)).ToDictionary(state => state.Identifier);
            }
            catch (ArgumentException ex)
            {
                throw new Exception("Duplicate state identifier found.", ex);
            }

            //Get and validate the initial state
            if(!newStates.TryGetValue(initialStateIdentifier, out State? value)) throw new InvalidInitialStateException("Initial state not added to the StateMachineBuilders state list.");
            State initialState = value;
            if (initialState.IsTransient) {
                throw new InvalidInitialStateException("The initial state may not be transient.");
            }

            //Group transitions by target state and add the corresponding states instance
            IEnumerable<Tuple<string, string, State>> targetedTransitions = transitions.GroupBy(t => t.Item3).SelectMany(group =>
            {
                if (!newStates.TryGetValue(group.Key, out State? value)) throw new KeyNotFoundException($"No state '{group.Key}' was added. Transitions pointing to it can not be added.");
                State targetState = value;
                return group.Select(transitionTuple => new Tuple<string, string, State>(transitionTuple.Item1, transitionTuple.Item2, targetState));
            });
            //Add the transitions to their source states
            targetedTransitions.GroupBy(t => t.Item1).ToList().ForEach(group =>
            {
                //Get the source state
                if (!newStates.TryGetValue(group.Key, out State? value)) throw new KeyNotFoundException($"The transitions source state '{group.Key}' does not exist.");
                State sourceState = value;

                //Instantiate transitions
                try
                {
                    Dictionary<string, Transition> newTransitions = group.Select(transitionTuple => new Transition(transitionTuple.Item2, transitionTuple.Item3)).ToDictionary(transition => transition.Symbol);
                    sourceState.SetTransitions(newTransitions);
                }
                catch (ArgumentException ex)
                {
                    throw new Exception($"Duplicate transition symbol found for state '{sourceState.Identifier}'.", ex);
                }
            });

            return new AsyncStateMachine(newStates, initialState, defaultErrorSymbol);
        }
        #endregion






    }
}
