using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    public class InvalidInitialStateException(string message) : Exception(message)
    {
    }
}
