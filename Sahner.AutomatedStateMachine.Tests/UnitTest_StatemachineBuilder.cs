using Sahner.AutomatedStateMachine.Exceptions;

namespace Sahner.AutomatedStateMachine.Tests
{
    public class UnitTest_StateMachineBuilder
    {
        [SetUp]
        public void Setup()
        {
        }
        /// <summary>
        /// Tests adding a single state
        /// </summary>
        [Test]
        public void Test_AddSingleState()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.Multiple(() =>
            {
                Assert.That(sm.States, Has.Count.EqualTo(1));
                Assert.That(sm.States.ContainsKey("state"));
            });

        }
        /// <summary>
        /// Tests adding multiple non-transient states
        /// </summary>
        [Test]
        public void Test_AddMultipleStatesBulk()
        {
            StateMachineBuilder builder = new();

            builder.AddStates("state1", "state2", "state3");

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state1");
            Assert.Multiple(() =>
            {
                Assert.That(sm.States, Has.Count.EqualTo(3));
                Assert.That(sm.States.ContainsKey("state1"));
                Assert.That(sm.States.ContainsKey("state2"));
                Assert.That(sm.States.ContainsKey("state3"));
            });

        }

        /// <summary>
        /// Test setting the initial state
        /// </summary>
        [Test]
        public void Test_InitialStateSet()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddState("state2");

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.Multiple(() =>
            {
                Assert.That(sm.States, Has.Count.EqualTo(2));
                Assert.That(sm.CurrentState.Identifier, Is.EqualTo("state"));
            });
        }
        /// <summary>
        /// Test if the automation function gets attached to the correct state
        /// </summary>
        /// <returns></returns>
        [Test]
        public void Test_AutomationFunction()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddState("stateT", (object[]? parameters) => { return Task.FromResult("delta"); });

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.Multiple(() =>
            {
                Assert.That(sm.States["stateT"].IsTransient, Is.True);
                Assert.That(sm.States["stateT"].AutomationFunction, Is.Not.Null);
            });

            Assert.Multiple(async () =>
            {
                Assert.That(await sm.States["stateT"].AutomationFunction.Invoke(null), Is.EqualTo("delta"));
                Assert.That(sm.States["state"].IsTransient, Is.False);
            });

        }

        /// <summary>
        /// Tests adding multiple transient and non-transient states
        /// </summary>
        [Test]
        public void Test_AddMultipleStatesAutomationFunction()
        {
            StateMachineBuilder builder = new();

            builder.AddStates(("state", null), ("stateT", (object[]? parameters) => { return Task.FromResult("delta"); }));

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.Multiple(() =>
            {
                Assert.That(sm.States, Has.Count.EqualTo(2));
                Assert.That(sm.States.ContainsKey("state"));
                Assert.That(sm.States.ContainsKey("stateT"));
            });
            Assert.Multiple(() =>
            {
                Assert.That(sm.States["stateT"].IsTransient, Is.True);
                Assert.That(sm.States["stateT"].AutomationFunction, Is.Not.Null);
            });

            Assert.Multiple(async () =>
            {
                Assert.That(await sm.States["stateT"].AutomationFunction.Invoke(null), Is.EqualTo("delta"));
                Assert.That(sm.States["state"].IsTransient, Is.False);
            });

        }

        /// <summary>
        /// Test if an exception is thrown when the initial state is not contained in the states list.
        /// </summary>
        [Test]
        public void Test_InitialStateNotContained()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");

            var ex = Assert.Throws<InvalidInitialStateException>(() => builder.BuildAsyncStateMachine("stateX"));
            Assert.That(ex.Message, Is.EqualTo("Initial state not added to the StateMachineBuilders state list."));

        }
        /// <summary>
        /// Test if an exception is thrown if the initial state is transient.
        /// </summary>
        [Test]
        public void Test_InitialStateNotTransient()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state", (object[]? parameters) => { return Task.FromResult("delta"); });

            var ex = Assert.Throws<InvalidInitialStateException>(() => builder.BuildAsyncStateMachine("state"));
            Assert.That(ex.Message, Is.EqualTo("The initial state may not be transient."));

        }
        /// <summary>
        /// Test if an exception is thrown if multiple states share the same identifier.
        /// </summary>
        [Test]
        public void Test_DuplicateStateDetected()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddState("state");

            var ex = Assert.Throws<Exception>(() => builder.BuildAsyncStateMachine("state"));
            Assert.That(ex.Message, Is.EqualTo("Duplicate state identifier found."));

        }
        /// <summary>
        /// Test to add a single transition
        /// </summary>
        [Test]
        public void Test_AddSingleTransition()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddTransition("state","delta","state");

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Transitions, Has.Count.EqualTo(1));
                Assert.That(sm.CurrentState.Transitions["delta"], Is.Not.Null);
            });
            Assert.That(sm.CurrentState.Transitions["delta"].SuccessorState.Identifier, Is.EqualTo("state"));

        }
        /// <summary>
        /// Test to add multiple transitions
        /// </summary>
        [Test]
        public void Test_AddMultipleTransitionsBulk()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state1");
            builder.AddState("state2");

            builder.AddTransitions(("state1", "delta", "state1"), ("state1", "sigma", "state2"));

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state1");
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Transitions, Has.Count.EqualTo(2));
                Assert.That(sm.CurrentState.Transitions["delta"], Is.Not.Null);
                Assert.That(sm.CurrentState.Transitions["sigma"], Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(sm.CurrentState.Transitions["delta"].SuccessorState.Identifier, Is.EqualTo("state1"));
                Assert.That(sm.CurrentState.Transitions["sigma"].SuccessorState.Identifier, Is.EqualTo("state2"));
            });

        }
        /// <summary>
        /// Test if an exception is thrown if the transition points to a non existing state.
        /// </summary>
        [Test]
        public void Test_AddInvalidTransitionEnd()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddTransition("state", "delta", "stateX");

            var ex = Assert.Throws<KeyNotFoundException>(() => builder.BuildAsyncStateMachine("state"));
            Assert.That(ex.Message, Is.EqualTo("No state 'stateX' was added. Transitions pointing to it can not be added."));

        }
        /// <summary>
        /// Test if an exception is thrown if the transition starts at a non existing state.
        /// </summary>
        [Test]
        public void Test_AddInvalidTransitionStart()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddTransition("stateX", "delta", "state");

            var ex = Assert.Throws<KeyNotFoundException>(() => builder.BuildAsyncStateMachine("state"));
            Assert.That(ex.Message, Is.EqualTo("The transitions source state 'stateX' does not exist."));

        }
        /// <summary>
        /// Test if an exception is thrown if two transitions share the same symbol fa a state.
        /// </summary>
        [Test]
        public void Test_AddDuplicateTransition()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            builder.AddTransition("state", "delta", "state");
            builder.AddTransition("state", "delta", "state");

            var ex = Assert.Throws<Exception>(() => builder.BuildAsyncStateMachine("state"));
            Assert.That(ex.Message, Is.EqualTo("Duplicate transition symbol found for state 'state'."));

        }
        /// <summary>
        /// Test if multiple states are added correctly.
        /// </summary>
        [Test]
        public void Test_AddMultipleStates()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state");
            for (int i = 0; i < 10; i++)
            {
                builder.AddState("state" + i);
            }

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state");
            Assert.That(sm.States, Has.Count.EqualTo(11));
            Assert.That(sm.States.ContainsKey("state"));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(sm.States["state" + i], Is.Not.Null);
            }


        }
        /// <summary>
        /// Test if multiple transitions are added correctly.
        /// </summary>
        [Test]
        public void Test_AddMultipleTransitions()
        {
            StateMachineBuilder builder = new();

            builder.AddState("state0");
            for (int i = 1; i < 10; i++)
            {
                builder.AddState("state" + i);
                builder.AddTransition("state" + (i - 1), "" + i, "state" + i);
            }

            AsyncStateMachine sm = builder.BuildAsyncStateMachine("state0");
            Assert.That(sm.States, Has.Count.EqualTo(10));
            
            for (int i = 0; i < 9; i++)
            {
                var state = sm.States["state" + i];
                Assert.That(state, Is.Not.Null);
                Assert.That(state.Transitions["" + (i + 1)].SuccessorState.Identifier, Is.EqualTo("state" + (i + 1)));
            }


        }

    }
}