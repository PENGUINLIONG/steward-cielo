using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Liongbot.Command.Attributes {
    /// <summary>
    /// Attribute that allows you directly parse command message into structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class CommandAttribute : Attribute {
        /// <summary>
        /// Name of the command.
        /// </summary>
        public string Name { get; set; } = null;
    }

    /// <summary>
    /// Attribute that instruct the parser how to parse named arguments. If
    /// neither an abbreviate nor a full name is provided, it's considered a
    /// freee argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ArgAttribute : Attribute {
        /// <summary>
        /// Single character argument abbreviate.
        /// </summary>
        public char Abbreviate { get; set; } = '\0';
        /// <summary>
        /// Full name of argument.
        /// </summary>
        public string Name { get; set; } = null;
        /// <summary>
        /// True if the current argument is a switch.
        /// </summary>
        public bool IsSwitch { get; set; } = false;
        /// <summary>
        /// Default value used when there is no explicit assignment.
        /// </summary>
        public object Default { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class FreeArgAttribute : Attribute {
        public object Default { get; set; }
    }

    /// <summary>
    /// Documentation for commands and fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
    public class DocAttribute : Attribute {
        public string Doc { get; private set; }
        public DocAttribute(string doc) {
            Doc = doc;
        }
    }
}
