using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class NoTransitionForSymbolException(string stateIdentifier, string symbol) :
        InvalidOperationException($"State '{stateIdentifier}' does not contain a transition for symbol '{symbol}'.")
    {
        public string StateIdentifier { get; } = stateIdentifier;
        public string Symbol { get; } = symbol;
    }
}
