using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class StateChangeHandlerException(Exception exception) : Exception("An exception occurred within a state change event handler.", exception)
    {
    }
}
