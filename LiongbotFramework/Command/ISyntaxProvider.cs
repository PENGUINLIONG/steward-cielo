using System.Collections.Generic;

namespace Liongbot.Command {
    /// <summary>
    /// Interface for all command syntax providers.
    /// </summary>
    public interface ISyntaxProvider {
        /// <summary>
        /// Make names for arguments using arguments' defined name. The original
        /// name is separated into words by algorithm.
        /// </summary>
        /// <param name="words">Separated words from the original name.</param>
        /// <returns>Actual name that will be used for business.</returns>
        string NameArgument(IEnumerable<string> words);
        /// <summary>
        /// Make names for commands using commands' defined name. The original
        /// name is separated into words by algorithm.
        /// </summary>
        /// <param name="words">Separated words from the original name.</param>
        /// <returns>Actual name that will be used for business.</returns>
        string NameCommand(IEnumerable<string> words);
        /// <summary>
        /// Determine whether two strings are syntactically equal under current
        /// syntax. For instance, upper case and lower case are considered equal
        /// in case-insensitive syntax.
        /// </summary>
        /// <returns>True if the strings are syntactically equal; false
        /// otherwise.</returns>
        bool SyntacticEqual(string a, string b);
        /// <summary>
        /// Parse a line of command into arguments. You should remind that the
        /// first argument is always the name of command invoked.
        /// </summary>
        /// <param name="injector">Argument injector for instanciation control.
        /// </param>
        /// <param name="args">Argument segments.</param>
        /// <returns>True if the arguments are successfully parsed; false
        /// otherwise.</returns>
        bool Parse(ArgumentInjector injector, ArgumentPack args);
    }
}
