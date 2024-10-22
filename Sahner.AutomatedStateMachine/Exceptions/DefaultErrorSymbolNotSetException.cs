using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class DefaultErrorSymbolNotSetException(string stateIdentifier, Exception innerException) : Exception($"The automation function of state {stateIdentifier} threw an exception that is not a TransientStateException. Set the DefaultErrorSymbol property to read the specified symbol regardless of the exceptions type, or throw a wrapping TransientStateException.", innerException)
    {
    }
}
