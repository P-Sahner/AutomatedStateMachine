using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class AutomationFunctionEmptyResultException(string stateIdentifier) : Exception($"The state machine ended in the transient state {stateIdentifier} without a symbol to continue. The automation function returned a null or empty string result.")
    {
    }
}
