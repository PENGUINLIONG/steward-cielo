using System;
using System.Runtime.Serialization;

namespace Liongbot.Command {
    /// <summary>
    /// Exception occurred during resolution of command structure, parsing of
    /// command and value injection.
    /// </summary>
    class CommandException : Exception {
        public CommandException() { }
        public CommandException(string message) : base(message) { }
        public CommandException(
            string message,
            Exception innerException) : base(message, innerException) { }
        protected CommandException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
