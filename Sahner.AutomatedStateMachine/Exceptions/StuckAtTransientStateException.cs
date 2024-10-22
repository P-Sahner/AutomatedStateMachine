using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class StuckAtTransientStateException(string stateIdentifier)
        : Exception($"The state machine ended in the transient state {stateIdentifier} without a symbol to continue.")
    {
    }
}
