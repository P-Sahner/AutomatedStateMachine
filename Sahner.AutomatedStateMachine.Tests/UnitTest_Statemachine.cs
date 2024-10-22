using Sahner.AutomatedStateMachine.Exceptions;

namespace Sahner.AutomatedStateMachine.Tests
{
    class UnitTest_StateMachine
    {
        private AsyncStateMachine sm;

        [SetUp]
        public void Setup()
        {
            StateMachineBuilder builder = new();

            builder.AddState("q0");
            builder.AddState("q1");
            builder.AddState("q2");
            builder.AddState("q3", async (object[]? parameters) =>
            {
                //Some long delay
                await Task.Delay(60);
                if (parameters != null && parameters.Length > 0 && parameters[0] != null)
                {
                    return (string)parameters[0];
                }
                else
                {
                    return "down";
                }
            });
            builder.AddState("q4");

            builder.AddTransition("q0", "up", "q1");
            builder.AddTransition("q1", "up", "q2");
            builder.AddTransition("q2", "up", "q3");
            builder.AddTransition("q3", "up", "q4");

            builder.AddTransition("q4", "down", "q3");
            builder.AddTransition("q3", "down", "q2");
            builder.AddTransition("q2", "down", "q1");
            builder.AddTransition("q1", "down", "q0");

            sm = builder.BuildAsyncStateMachine("q0");
        }

        #region FundamentalOperations
        /// <summary>
        /// Tests a simple state change.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Test_ReadSingleSymbolAsync()
        {
            await sm.ReadSymbolAsync("up");
            Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
        }
        /// <summary>
        /// Test if an exception is thrown if no transition is found for a given symbol.
        /// </summary>
        [Test]
        public void Test_ReadWrongSymbol()
        {
            var ex = Assert.ThrowsAsync<NoTransitionForSymbolException>(() => sm.ReadSymbolAsync("apple"));
            Assert.That(ex.Message, Is.EqualTo("State 'q0' does not contain a transition for symbol 'apple'."));

        }
        /// <summary>
        /// Test if the automation function gets passed the correct parameters.
        /// </summary>
        [Test]
        public void Test_ReadSymbolAutomatedStatePassParameters()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up", "up"));
            Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));

        }
        #endregion

        #region AsynchronousBehaviour
        /// <summary>
        /// Tests if entering a transient state correctly sets
        /// and unsets the <see cref="AsyncStateMachine.IsTransientStateRunning"/>
        /// property.
        /// </summary>
        [Test]
        public void Test_ReadSymbolAutomatedStateTiming()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Task t = sm.ReadSymbolAsync("up", "up");
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q3"));
                Assert.That(sm.IsTransientStateRunning);
            });
            t.Wait();
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));
                Assert.That(!sm.IsTransientStateRunning);
            });

        }
        /// <summary>
        /// Tests if parallel access is correctly enqueued.
        /// </summary>
        [Test]
        public void Test_ReadSymbolAutomatedStateQueueing()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Task t = sm.ReadSymbolAsync("up", "up");
            Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q3"));
            Task t2 = sm.ReadSymbolAsync("down");
            Assert.That(sm.IsTransientStateRunning);
            Task.WaitAll(t, t2);
            t.Wait();
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q2"));
                Assert.That(!sm.IsTransientStateRunning);
            });

        }
        #endregion

        #region Events

        /// <summary>
        /// Tests if the state changed event gets raised.
        /// </summary>
        [Test]
        public void Test_EventStateChangedRisen()
        {
            List<StateChangedEventArgs> receivedEvents = [];

            sm.OnStateChanged += delegate (AsyncStateMachine sender, StateChangedEventArgs e)
            {
                receivedEvents.Add(e);
            };

            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            sm.ReadSymbolAsync("up", "up").Wait();
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));
                Assert.That(receivedEvents, Has.Count.EqualTo(4));
            });
            Assert.Multiple(() =>
            {
                Assert.That(receivedEvents[0].FromState.Identifier, Is.EqualTo("q0"));
                Assert.That(receivedEvents[0].Symbol, Is.EqualTo("up"));
                Assert.That(receivedEvents[0].ToState.Identifier, Is.EqualTo("q1"));
                Assert.That(receivedEvents[3].ToState.Identifier, Is.EqualTo("q4"));
            });

        }
        /// <summary>
        /// Tests if the state entered event gets raised.
        /// </summary>
        [Test]
        public void Test_EventStateEnterRisen()
        {
            List<StateEnteredEventArgs> receivedEvents = [];

            sm.States["q1"].Entered += delegate (State sender, StateEnteredEventArgs e)
            {
                receivedEvents.Add(e);
            };

            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
                Assert.That(receivedEvents, Has.Count.EqualTo(1));
            });
            Assert.Multiple(() =>
            {
                Assert.That(receivedEvents[0].FromState.Identifier, Is.EqualTo("q0"));
                Assert.That(receivedEvents[0].Symbol, Is.EqualTo("up"));
            });

        }
        /// <summary>
        /// Tests if the state leave event gets raised.
        /// </summary>
        [Test]
        public void Test_EventStateLeaveRisen()
        {
            List<StateLeaveEventArgs> receivedEvents = [];

            sm.States["q0"].Leave += delegate (State sender, StateLeaveEventArgs e)
            {
                receivedEvents.Add(e);
            };

            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
                Assert.That(receivedEvents, Has.Count.EqualTo(1));
            });

            Assert.Multiple(() =>
            {
                Assert.That(receivedEvents[0].Symbol, Is.EqualTo("up"));
                Assert.That(receivedEvents[0].ToState.Identifier, Is.EqualTo("q1"));
            });

        }
        /// <summary>
        /// Tests if those events are raised in the correct order
        /// </summary>
        [Test]
        public void Test_EventOrdering()
        {

            //Old leave, new enter, state changed
            int orderCounter = 0;

            sm.States["q0"].Leave += delegate (State sender, StateLeaveEventArgs e)
            {
                Assert.That(orderCounter, Is.EqualTo(0));
                orderCounter = 1;
            };

            sm.States["q1"].Entered += delegate (State sender, StateEnteredEventArgs e)
            {
                Assert.That(orderCounter, Is.EqualTo(1));
                orderCounter = 2;
            };

            sm.OnStateChanged += delegate (AsyncStateMachine sender, StateChangedEventArgs e)
            {
                Assert.That(orderCounter, Is.EqualTo(2));
                orderCounter = 3;
            };

            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
                Assert.That(orderCounter, Is.EqualTo(3));
            });

        }

        #endregion

        #region ExceptionsInEvents

        /// <summary>
        /// Tests if throwing an exception inside the state changed event is correctly recognized.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateChanged()
        {
            sm.OnStateChanged += delegate (AsyncStateMachine sender, StateChangedEventArgs e)
            {
                throw new Exception("Test");
            };
            var ex = Assert.ThrowsAsync<StateChangeHandlerException>(() => sm.ReadSymbolAsync("up"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
            });
        }

        /// <summary>
        /// Tests if throwing an exception inside the state entered event is correctly recognized.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateEntered()
        {
            sm.States["q1"].Entered += delegate (State sender, StateEnteredEventArgs e)
            {
                throw new Exception("Test");
            };

            var ex = Assert.ThrowsAsync<StateChangeHandlerException>(() => sm.ReadSymbolAsync("up"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
            });
        }

        /// <summary>
        /// Tests if throwing an exception inside the state leave event is correctly recognized.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateLeave()
        {
            sm.States["q0"].Leave += delegate (State sender, StateLeaveEventArgs e)
            {
                throw new Exception("Test");
            };
            var ex = Assert.ThrowsAsync<StateChangeHandlerException>(() => sm.ReadSymbolAsync("up"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q1"));
            });
        }

        /// <summary>
        /// Tests if transient states are executed even if exceptions were thrown in the state changed event
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateChangedBeforeTransientState()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q2"));
            sm.OnStateChanged += delegate (AsyncStateMachine sender, StateChangedEventArgs e)
            {
                throw new Exception("Test");
            };
            var ex = Assert.ThrowsAsync<AggregateException>(() => sm.ReadSymbolAsync("up", "up"));

            Assert.That(ex.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(ex.InnerExceptions[0].InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerExceptions[0].InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));
            });
        }

        /// <summary>
        /// Tests if transient states are executed even if exceptions were thrown in their state entered events
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateEnteredOfTransientState()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            //Now in q2
            sm.States["q3"].Entered += delegate (State sender, StateEnteredEventArgs e)
            {
                throw new Exception("Test");
            };
            var ex = Assert.ThrowsAsync<StateChangeHandlerException>(() => sm.ReadSymbolAsync("up", "up"));

            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));
            });
        }

        /// <summary>
        /// Tests if transient states are executed even if exceptions were thrown in their state leave events
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Test]
        public void Test_ThrowInStateLeaveOfTransientState()
        {
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            Assert.DoesNotThrowAsync(() => sm.ReadSymbolAsync("up"));
            //Now in q2
            sm.States["q3"].Leave += delegate (State sender, StateLeaveEventArgs e)
            {
                throw new Exception("Test");
            };
            var ex = Assert.ThrowsAsync<StateChangeHandlerException>(() => sm.ReadSymbolAsync("up", "up"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("Test"));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("q4"));
            });
        }
        #endregion

        #region AutomationFunctionExceptions
        /// <summary>
        /// Tests if throwing a <see cref="TransientStateException"/>
        /// lets the state machine read the correct symbol.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_ThrowTransientStateExceptionInAutomationFunction()
        {
            //Setup
            AsyncStateMachine errorSuccessStateMachine;
            StateMachineBuilder builder = new();

            builder.AddState("initial");
            builder.AddState("busy", (object[]? parameters) =>
            {
                throw new TransientStateException("error", "Test");
            });

            builder.AddState("successful");
            builder.AddState("failure");

            builder.AddTransition("initial", "start", "busy");
            builder.AddTransition("busy", "success", "successful");
            builder.AddTransition("busy", "error", "failure");

            errorSuccessStateMachine = builder.BuildAsyncStateMachine("initial");

            var ex = Assert.ThrowsAsync<TransientStateException>(() => errorSuccessStateMachine.ReadSymbolAsync("start"));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("Test"));
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("failure"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });
        }

        /// <summary>
        /// Tests if throwing a non <see cref="TransientStateException"/>
        /// lets the state machine enter a stuck state.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_ThrowNonTransientStateExceptionInAutomationFunction()
        {
            //Setup
            AsyncStateMachine errorSuccessStateMachine;
            StateMachineBuilder builder = new();

            builder.AddState("initial");
            builder.AddState("busy", (object[]? parameters) =>
            {
                throw new Exception("error123");
            });

            builder.AddState("successful");
            builder.AddState("failure");

            builder.AddTransition("initial", "start", "busy");
            builder.AddTransition("busy", "success", "successful");
            builder.AddTransition("busy", "error", "failure");

            errorSuccessStateMachine = builder.BuildAsyncStateMachine("initial");

            var ex = Assert.ThrowsAsync<DefaultErrorSymbolNotSetException>(() => errorSuccessStateMachine.ReadSymbolAsync("start"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException.Message, Is.EqualTo("error123"));
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });

            var ex2 = Assert.ThrowsAsync<StuckAtTransientStateException>(() => errorSuccessStateMachine.ReadSymbolAsync("qqq"));
            Assert.Multiple(() =>
            {
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });
        }

        /// <summary>
        /// Tests if throwing a <see cref="TransientStateException"/>
        /// lets the state machine enter a stuck state.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_ThrowTransientStateExceptionWithEmptySymbolInAutomationFunction()
        {
            //Setup
            AsyncStateMachine errorSuccessStateMachine;
            StateMachineBuilder builder = new();

            builder.AddState("initial");
            builder.AddState("busy", (object[]? parameters) =>
            {
                throw new TransientStateException("", "error123");
            });

            builder.AddState("successful");
            builder.AddState("failure");

            builder.AddTransition("initial", "start", "busy");
            builder.AddTransition("busy", "success", "successful");
            builder.AddTransition("busy", "error", "failure");

            errorSuccessStateMachine = builder.BuildAsyncStateMachine("initial");

            var ex = Assert.ThrowsAsync<AggregateException>(() => errorSuccessStateMachine.ReadSymbolAsync("start"));
            Assert.That(ex.InnerExceptions, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerExceptions[0].Message, Is.EqualTo("error123"));
                Assert.That(ex.InnerExceptions[1], Is.InstanceOf(typeof(AutomationFunctionEmptyResultException)));
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });

            var ex2 = Assert.ThrowsAsync<StuckAtTransientStateException>(() => errorSuccessStateMachine.ReadSymbolAsync("qqq"));
            Assert.Multiple(() =>
            {
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });
        }

        /// <summary>
        /// Tests if returning an empty string
        /// lets the state machine enter a stuck state.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_ReturnEmptyStringInAutomationFunction()
        {
            //Setup
            AsyncStateMachine errorSuccessStateMachine;
            StateMachineBuilder builder = new();

            builder.AddState("initial");
            builder.AddState("busy", (object[]? parameters) =>
            {
                return Task.FromResult("");
            });

            builder.AddState("successful");
            builder.AddState("failure");

            builder.AddTransition("initial", "start", "busy");
            builder.AddTransition("busy", "success", "successful");
            builder.AddTransition("busy", "error", "failure");

            errorSuccessStateMachine = builder.BuildAsyncStateMachine("initial");

            var ex = Assert.ThrowsAsync<AutomationFunctionEmptyResultException>(() => errorSuccessStateMachine.ReadSymbolAsync("start"));
            Assert.Multiple(() =>
            {
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });

            var ex2 = Assert.ThrowsAsync<StuckAtTransientStateException>(() => errorSuccessStateMachine.ReadSymbolAsync("qqq"));
            Assert.Multiple(() =>
            {
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("busy"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
            });
        }

        /// <summary>
        /// Tests if throwing a non <see cref="TransientStateException"/>
        /// lets the state machine transition successfully when the
        /// <see cref="AsyncStateMachine.DefaultErrorSymbol"/> property is set.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_ThrowNonTransientStateExceptionInAutomationFunctionWithDefaultErrorSymbolSet()
        {
            //Setup
            AsyncStateMachine errorSuccessStateMachine;
            StateMachineBuilder builder = new();

            builder.AddState("initial");
            builder.AddState("busy", (object[]? parameters) =>
            {
                throw new Exception("Test");
            });

            builder.AddState("successful");
            builder.AddState("failure");

            builder.AddTransition("initial", "start", "busy");
            builder.AddTransition("busy", "success", "successful");
            builder.AddTransition("busy", "error", "failure");

            errorSuccessStateMachine = builder.BuildAsyncStateMachine("initial", "error");

            var ex = Assert.ThrowsAsync<Exception>(() => errorSuccessStateMachine.ReadSymbolAsync("start"));
            Assert.Multiple(() =>
            {
                Assert.That(errorSuccessStateMachine.CurrentState.Identifier, Is.EqualTo("failure"));
                Assert.That(errorSuccessStateMachine.IsTransientStateRunning, Is.False);
                Assert.That(ex.Message, Is.EqualTo("Test"));
            });

        }

        #endregion

        #region LinkedTransientStates
        /// <summary>
        /// Tests if multiple linked transient states are executed after each other.
        /// </summary>
        [Test]
        public void Test_LinkTransientStates()
        {
            StateMachineBuilder builder = new();
            AsyncStateMachine circularStateMachine;

            int callCount = 0;

            async Task<string> upFkt(object[]? parameters)
            {
                //Some tasks delay
                await Task.Delay(5);
                callCount++;
                return "up";
            }

            builder.AddState("q0");
            builder.AddState("q1", upFkt);
            builder.AddState("q2", upFkt);
            builder.AddState("q3", upFkt);
            builder.AddState("q4", upFkt);

            builder.AddTransition("q0", "up", "q1");
            builder.AddTransition("q1", "up", "q2");
            builder.AddTransition("q2", "up", "q3");
            builder.AddTransition("q3", "up", "q4");
            builder.AddTransition("q4", "up", "q0");

            circularStateMachine = builder.BuildAsyncStateMachine("q0");

            Assert.DoesNotThrowAsync(() => circularStateMachine.ReadSymbolAsync("up"));
            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4));
                Assert.That(circularStateMachine.CurrentState.Identifier, Is.EqualTo("q0"));
            });

        }
        /// <summary>
        /// Tests the exception handling if multiple linked transient states are executed after each other.
        /// </summary>
        /// <exception cref="TransientStateException"></exception>
        [Test]
        public void Test_LinkTransientStatesException()
        {
            StateMachineBuilder builder = new();
            AsyncStateMachine circularStateMachine;

            int callCount = 0;

            async Task<string> upFkt(object[]? parameters)
            {
                //Some tasks delay
                await Task.Delay(5);
                callCount++;
                throw new TransientStateException("up", callCount.ToString());

            }

            builder.AddState("q0");
            builder.AddState("q1", upFkt);
            builder.AddState("q2", upFkt);
            builder.AddState("q3", upFkt);
            builder.AddState("q4", upFkt);

            builder.AddTransition("q0", "up", "q1");
            builder.AddTransition("q1", "up", "q2");
            builder.AddTransition("q2", "up", "q3");
            builder.AddTransition("q3", "up", "q4");
            builder.AddTransition("q4", "up", "q0");

            circularStateMachine = builder.BuildAsyncStateMachine("q0");

            var ex = Assert.ThrowsAsync<AggregateException>(() => circularStateMachine.ReadSymbolAsync("up"));
            Assert.That(ex.InnerExceptions, Has.Count.EqualTo(4));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(ex.InnerExceptions[i].Message, Is.EqualTo((i + 1).ToString()));
            }

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4));
                Assert.That(circularStateMachine.CurrentState.Identifier, Is.EqualTo("q0"));
            });

        }

        #endregion

    }
}
