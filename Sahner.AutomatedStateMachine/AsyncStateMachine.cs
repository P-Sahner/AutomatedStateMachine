using Sahner.AutomatedStateMachine.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine
{
    public class AsyncStateMachine
    {
        #region Header

        #region TypeDefinitions
        protected class QueueSymbol
        {
            public string Symbol = "";
            public object[] Parameters = [];
            public QueueSymbol(string symbol) => Symbol = symbol;
            public QueueSymbol(string symbol, object[] parameters)
            {
                Symbol = symbol;
                Parameters = parameters;
            }
        }
        #endregion

        #region Events

        public delegate void StateChangedHandler(AsyncStateMachine sender, StateChangedEventArgs e);
        /// <summary>
        /// Gets raised when a state change was performed (including self-loops).
        /// Is risen before the automation function is called.
        /// Throwing inside a handler of this event will let the
        /// <see cref="ReadSymbolAsync(string, object[])"/> function
        /// throw the corresponding exception.
        /// If the following state is transient, its automation function will be executed first
        /// and the exception is thrown afterwards.
        /// </summary>
        public event StateChangedHandler? OnStateChanged;

        protected void RaiseOnStateChanged(State fromState, string symbol, State toState)
        {
            OnStateChanged?.Invoke(this, new StateChangedEventArgs(fromState, symbol, toState));
        }

        #endregion

        #region Properties
        /// <summary>
        /// The list of all possible states this instance can be in.
        /// </summary>
        private Dictionary<string, State> _states { get; }
        /// <summary>
        /// An immutable list of the state machines states.
        /// </summary>
        public ImmutableDictionary<string, State> States { get { return _states.ToImmutableDictionary(); } }
        /// <summary>
        /// The state this state machine is currently in.
        /// </summary>
        public State CurrentState { get; protected set; }
        /// <summary>
        /// This flag indicates whether an automation function is currently being executed.
        /// </summary>
        public bool IsTransientStateRunning { get; protected set; }
        /// <summary>
        /// If this property is unequal to the empty string,
        /// throwing an exception that is not a <see cref="TransientStateException"/>
        /// inside an automation function of a transient state will let
        /// the state machine read its content as symbol instead.
        /// This eliminates the need to wrap every exception in a <see cref="TransientStateException"/>.
        /// </summary>
        public string DefaultErrorSymbol { get; }
        #endregion

        #region PrivateFields
        /// <summary>
        /// A semaphore used as implicit FIFO-queue for scheduling symbols to read.
        /// </summary>
        protected SemaphoreSlim ExecutionSemaphore = new(1, 1);
        #endregion

        #region Initialization
        /// <summary>
        /// Instances are constructed using the <see cref="StateMachineBuilder"/>.
        /// </summary>
        internal AsyncStateMachine(Dictionary<string, State> states, State initialState, string defaultErrorSymbol = "")
        {
            _states = states;
            CurrentState = initialState;
            DefaultErrorSymbol = defaultErrorSymbol;
        }
        #endregion

        #endregion

        #region PrivateFunctions

        #endregion

        #region PublicFunctions
        /// <summary>
        /// Lets the state machine read a symbol and perform a state change
        /// similar to deterministic finite automatons.
        /// If the target state is transient (has a non-null automation function),
        /// its automation function is called after the state was entered.
        /// The functions return value is then read as further input symbol and
        /// processed before any pending reading operation.
        /// If the sequel state is also transient, the behavior applies recursively.
        /// <br/>
        /// If a <see cref="TransientStateException"/> is thrown
        /// inside an automation function, its 
        /// <see cref="TransientStateException.Symbol"/> property is read similar to
        /// a normal string return value. When reaching the first non-transient state,
        /// the exception is thrown. If multiple exceptions occurred,
        /// they are collected in an <see cref="AggregateException"/>.
        /// <br/>
        /// If the <see cref="DefaultErrorSymbol"/> property is not set,
        /// throwing any exception type other than <see cref="TransientStateException"/>,
        /// will cause a <see cref="DefaultErrorSymbolNotSetException"/> and the state machine
        /// stays in the transient state without reading any symbol.
        /// At this state any further attempt to read a symbol
        /// will throw a <see cref="StuckAtTransientStateException"/>.
        /// When the <see cref="TransientStateException.Symbol"/> property is the empty string 
        /// or an automation functions return value is the empty string,
        /// the state machine remains in the transient state and
        /// a <see cref="NoTransitionForSymbolException"/> exception is thrown.
        /// Further attempts to read a symbol
        /// will throw a <see cref="StuckAtTransientStateException"/>.
        /// <br/>
        /// Any exception thrown in an event handler will be wrapped
        /// in a <see cref="StateChangeHandlerException"/>.
        /// <br/>
        /// Calls to this function are managed in an implicit FIFO queue,
        /// serializing its behavior and providing thread-safety.
        /// </summary>
        /// <param name="symbol">The symbol to read.</param>
        /// <param name="parameters">The parameters to pass to the automation function.</param>
        /// <returns></returns>
        /// <exception cref="NoTransitionForSymbolException"></exception>
        /// <exception cref="StuckAtTransientStateException"></exception>
        /// <exception cref="StateChangeHandlerException"></exception>
        /// <exception cref="AggregateException"></exception>
        /// <exception cref="DefaultErrorSymbolNotSetException"></exception>
        /// <exception cref="TransientStateException"></exception>
        public async Task ReadSymbolAsync(string symbol, params object[] parameters)
        {
            //Implements implicit FIFO-queueing using the (1,1) semaphore
            await ExecutionSemaphore.WaitAsync();

            try
            {
                //Only one thread from here

                //First check if the current state is transient.
                if (CurrentState.IsTransient)
                {
                    //Stuck, abort
                    //Semaphore is released in finally
                    throw new StuckAtTransientStateException(CurrentState.Identifier);
                }

                //Start with our given symbol
                QueueSymbol? nextSymbol = new(symbol, parameters);

                //Accumulate thrown exceptions
                List<Exception> exceptions = [];

                //Perform the state change and execute automation function(s)
                while (nextSymbol != null && nextSymbol.Symbol != "")
                {
                    //Save the current state for the state changed events
                    State sourceState = CurrentState;

                    //Find the transition matching the symbol, if not found throw an exception.
                    if (!CurrentState.Transitions.ContainsKey(nextSymbol.Symbol)) {
                        //Instead of throwing directly, leave the loop and throw together with other exceptions afterwards.
                        exceptions.Add(new NoTransitionForSymbolException(CurrentState.Identifier, nextSymbol.Symbol));
                        break;
                    }
                    Transition? matchingTransition = CurrentState.Transitions[nextSymbol.Symbol];

                    //Raise the OnLeave event of the current state
                    try { CurrentState.OnLeave(matchingTransition.SuccessorState, nextSymbol.Symbol); }
                    catch (Exception ex)
                    {
                        //If an exception is thrown, we store it and throw it later to allow the state change to be performed.
                        exceptions.Add(new StateChangeHandlerException(ex));
                    }

                    //Perform the actual state change
                    CurrentState = matchingTransition.SuccessorState;

                    //Raise the OnEntered event of the subsequent state
                    try { CurrentState.OnEntered(sourceState, nextSymbol.Symbol); }
                    catch (Exception ex)
                    {
                        //If an exception is thrown, we store it and throw it later
                        //to allow the automation function to be executed if the state is transient.
                        exceptions.Add(new StateChangeHandlerException(ex));
                    }

                    //Raise the state machines state changed event
                    try { RaiseOnStateChanged(sourceState, nextSymbol.Symbol, CurrentState); }
                    catch (Exception ex)
                    {
                        //If an exception is thrown, we store it and throw it later
                        //to allow the automation function to be executed if the state is transient.
                        exceptions.Add(new StateChangeHandlerException(ex));
                    }

                    //Execute the automation function if the current state is transient

                    if (CurrentState.IsTransient)
                    {
                        //Set the running flag.
                        IsTransientStateRunning = true;
                        //The symbol returned by the automation function or its exception.
                        string readSymbol = "";

                        try
                        {
                            //Execute the automation function
                            //It is not NULL because of the CurrentState.IsTransient check
                            readSymbol = await CurrentState.AutomationFunction!.Invoke(nextSymbol.Parameters);
                        }
                        catch (TransientStateException ex)
                        {
                            //A controlled exception was thrown, use the exceptions symbol as next one.
                            readSymbol = ex.Symbol;
                            //Allow the run to continue
                            exceptions.Add(ex);
                        }
                        catch (Exception ex)
                        {
                            //An unknown exception type was thrown.

                            if (DefaultErrorSymbol != "")
                            {
                                //Interpret the default symbol as errors symbol property
                                readSymbol = DefaultErrorSymbol;
                                //Allow the run to continue
                                exceptions.Add(ex);
                            }
                            else
                            {
                                //No known symbol to continue
                                //Instead of throwing directly, leave the loop and throw together with other exceptions afterwards.
                                exceptions.Add(new DefaultErrorSymbolNotSetException(CurrentState.Identifier, ex));
                                break;
                            }
                        }
                        finally
                        {
                            //Clear the running flag
                            IsTransientStateRunning = false;
                        }

                        //Check if a symbol unequal to the empty string was returned
                        if (readSymbol == null || readSymbol == "")
                        {
                            //Instead of throwing directly, leave the loop and throw together with other exceptions afterwards.
                            exceptions.Add(new AutomationFunctionEmptyResultException(CurrentState.Identifier));
                            break;
                        }

                        //Read the automation functions return value as next input symbol
                        nextSymbol = new QueueSymbol(readSymbol);
                    }
                    else
                    {
                        //Static state
                        //Clear the nextSymbol variable to stop the execution
                        nextSymbol = null;
                    }

                }

                //Done. Throw exceptions if occurred.
                if (exceptions.Count > 1)
                {
                    throw new AggregateException($"Multiple exceptions occurred during the state change for symbol '{symbol}'.", exceptions);
                }
                else if(exceptions.Count == 1)
                {
                    throw exceptions[0];
                }
            }
            finally {
                //Release the semaphore
                //The try finally ensures, the semaphore is held until the AggregateException is thrown.
                ExecutionSemaphore.Release();
            }
        }
        #endregion
    }
}
