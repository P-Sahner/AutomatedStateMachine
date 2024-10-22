using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahner.AutomatedStateMachine.Exceptions
{
    /// <summary>
    /// When thrown inside an automation function,
    /// the exceptions attached <see cref="Symbol"/> property
    /// is read as input symbol by the state machine.
    /// The corresponding transition is performed and the exception is thrown afterwards.
    /// Following transient states will also be executed
    /// and their potential exceptions accumulated and thrown afterwards.
    /// </summary>
    public class TransientStateException : Exception
    {
        /// <summary>
        /// The symbol to read by the state machine when catching the exception.
        /// </summary>
        public string Symbol { get; private set; }
        public TransientStateException(string symbol)
        {
            Symbol = symbol;
        }
        public TransientStateException(string symbol, string message) : base(message)
        {
            Symbol = symbol;
        }
        public TransientStateException(string symbol, string message, Exception inner) : base(message, inner)
        {
            Symbol = symbol;
        }
    }
}
