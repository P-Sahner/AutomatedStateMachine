# AutomatedStateMachine
This repository provides an implementation of an asynchronous,
deterministic state machine with support for transient states,
automatic behavior and custom exception handling.
It allows separating the control flow logic from the
applications business logic.
The state machine sequentially processes input symbols,
transitions between states,
and invokes automation functions of transient states.

## Key Concept

	*   The Statemachine processes string symbols like a deterministic finite automaton.
	*   A transient state is a special state which has an automation function attached to it.
	*   When entering a transient state, its automation function is executed.
	*   The automation functions result is then read as input symbol.
    *   The returned symbol has priority and is read before any other may be.

## Key Features

	*   Transient States: Allow automatic behavior and complex workflows.
	*   Thread-Safe Execution: Calls to state transitions are serialized, ensuring that only one transition happens at a time.
	*   Advanced Exception Handling: The TransientStateException allows controlled state changes in response to errors.
	*   Async-Await Pattern: The task-based asynchronous pattern allows non-blocking operation.

## Getting Started

### Installation via NuGet:
```
dotnet add package Sahner.AutomatedStateMachine
```

### Basic Usage

#### Constructing state machines:

```
StateMachineBuilder builder = new();

//Non- transient states can easily be added as bulk
builder.AddStates("initial", "failure", "final")
    //Add a transient one
    .AddState("busy", async parameters =>
    {
        //Perform some work
        bool result = await Work();
        return result ? "success" : "error";
    })
//Add transitions
.AddTransitions(
    ("initial", "begin", "busy"),
    ("busy", "success", "final"),
    ("busy", "error", "failure"),
    ("failure", "retry", "busy")
);

//Build the state machine, given its initial state
AsyncStateMachine stateMachine = builder.BuildAsyncStateMachine("initial");
```

#### Reading Symbols and Performing Transitions

Perform an asynchronous read operation:
```
await stateMachine.ReadSymbolAsync("begin");
//Now in state final or failure
```
Or a synchronous one if you need:
```
stateMachine.ReadSymbolAsync("begin").Wait()
//Now in state final or failure
```

#### Basic Exception Handling

```
try
{
    //Read a symbol
    await stateMachine.ReadSymbolAsync("invalidSymbol");
}
catch (NoTransitionForSymbolException)
{
    //Usually no need to check the state machines current state.
    //Thrown if it can not find a transition for the given symbol
}
catch (AggregateException ex) { 
    //If more than one exception occurred, they are wrapped in an AggregateException
}
```

#### Defining Transient States

An automation function can be attached to a state during the build process.
Either inline like before or as an explicit function:
Lets say there is a class with the following function:
```
private async Task<string> AutomationFunction(object[]? parameters)
{
    var result = await Work();
    return result ? "success" : "error";
}
```
We can then use it as follows:
```
StateMachineBuilder builder = new();

builder.AddState("busy", AutomationFunction);
```

#### Passing Parameters
It is possible to pass parameters to the automation function:
```
private async Task<string> AutomationFunction(object[]? parameters)
{
    var parameter0 = (string)parameters![0];
    var parameter1 = (string)parameters![1];
    var result = await Work();
    return result ? "success" : "error";
}

private async void ReadWithParameter(){
    //Read a symbol
    await stateMachine.ReadSymbolAsync("begin", "parameter content 0", "parameter content 1");
}
```

#### Exceptions in Transient States

When an uncaught exception occurs within
the automation function of a transient state,
the state machine does not get a symbol to continue
and gets stuck at a transient state,
unable to read further symbols.
To still allow proper exception handling,
throw a TransientStateException:

```
throw new TransientStateException("symbol");
//Or:
throw new TransientStateException("symbol", "message");
//Or:
throw new TransientStateException("symbol", "message", innerExeption);
```
It could look like this:

```
private async Task<string> AutomationFunction(object[]? parameters)
{
    try
    {
        await Work();
        return "success";
    }
    catch (Exception innerEx)
    {
        throw new TransientStateException("error", "message", innerEx);
    }
}
```
Alternatively set the DefaultErrorSymbol property.
Every time an exception not of type TransientStateException is thrown,
this predefined symbol is read:
```
StateMachineBuilder builder = new();
//Add states and transitions...
AsyncStateMachine stateMachine = builder.BuildAsyncStateMachine("initial", "default error symbol");
```

### Responding to State Changes

There are three basic events to perform certain actions
if a specific state was entered or left.
Those events are suited e.g. for logging
or to perform changes on the UI, if existent,
like enabling or disabling control elements. 

```
stateMachine.OnStateChanged += (AsyncStateMachine sender, StateChangedEventArgs e) =>
{
    Console.WriteLine($"State change from: {e.FromState} to {e.ToState} by symbol {e.Symbol}.");
};

stateMachine.States["initial"].Entered += (State sender, StateEnteredEventArgs e) =>
{
    Console.WriteLine($"The initial state was entered from {e.FromState} by symbol {e.Symbol}");
};

stateMachine.States["initial"].Leave += (State sender, StateLeaveEventArgs e) =>
{
    Console.WriteLine($"The initial state was left to {e.ToState} by symbol {e.Symbol}");
};
```


### Larger Abstract Example

This larger example retries some action a given number of times
before throwing an exception.
It makes use of the possibility to concatenate transient states.
More complex use cases would probably include
more different transient states than this example.

```
public class RetryExample
{
    private int retries;
    private readonly int maxRetries;
    private readonly AsyncStateMachine stateMachine;
    public RetryExample(int maxRetries) { 
        retries = maxRetries;
        this.maxRetries = maxRetries;

        //Construct state machine

        StateMachineBuilder builder = new();

        builder
            .AddStates("initial", "failure", "finish")
            .AddState("trying", Trying)
            .AddTransitions(
                ("initial", "try", "trying"),
                ("trying", "success", "finish"),
                ("trying", "error", "failure"),
                ("trying", "retry", "trying")
                );


        stateMachine = builder.BuildAsyncStateMachine("initial");
    }

    private async Task<string> Trying(object[]? parameters)
    {
        try
        {
            //Perform something that could fail instead
            await Task.Delay(1000);
            //Success
            return "success";
        }
        catch (Exception ex)
        {
            //Failed, reduce remaining retries
            retries--;
            if (retries > 0)
            {
                //Ignore the exception and retry
                return "retry";
            }
            else
            {
                //No more retries
                throw new TransientStateException("error", $"Failed to perform the action {maxRetries} times.", ex);
            }
        }
    }

    public async Task BeginTrying()
    {
        try
        {
            await stateMachine.ReadSymbolAsync("try");
        }
        catch (NoTransitionForSymbolException)
        {
            //Already done, current state is failure or finish
        }
        catch (Exception ex)
        {
            //The final exception if failed.
            //Catch and process here or just propagate
        }
    }
}
```

## License

This repository is licensed under the MIT License. See the LICENSE file for more details.

## 

Feel free to open issues or submit pull requests to contribute to this project!