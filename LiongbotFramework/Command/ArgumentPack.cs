using Liongbot.Command.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Liongbot.Command {
    /// <summary>
    /// A sequential pack of processed arguments. It's the least processed form
    /// of arguments. It's not recommended to use this type for processing
    /// anyway.
    /// 
    /// Notice that the first argument is always the name of the command
    /// invoked.
    /// </summary>
    public struct ArgumentPack : IEnumerable<string> {
        string[] _inner;
        public ArgumentPack(string cmd) {
            _inner = TryProcessImpl(cmd) ??
                throw new CommandException("Invalid argument format");
        }
        internal ArgumentPack(string[] args) {
            _inner = args;
        }

        public int Count { get => _inner.Length; }

        /// <summary>
        /// Process command and divide a line of command into command and
        /// arguments, without protection from exceptions.
        /// </summary>
        /// <param name="cmd">The line of command to be processed.</param>
        /// <returns>Pack of arguments while command name is also considered
        /// arguemnt.</returns>
        public static ArgumentPack Process(string cmd) {
            var rv = TryProcessImpl(cmd);
            if (rv == null) {
                throw new CommandException("Unable to process argument text.");
            } else {
                return new ArgumentPack(rv);
            }
        }
        /// <summary>
        /// Process command and divide a line of command into command and
        /// arguments.
        /// </summary>
        /// <param name="cmd">The line of command to be processed.</param>
        /// <returns>Pack of arguments while command name is also considered
        /// arguemnt.</returns>
        public static bool TryProcess(string cmd, out ArgumentPack args) {
            var rv = TryProcessImpl(cmd);
            if (rv == null) {
                args = new ArgumentPack { _inner = null };
                return false;
            } else {
                args = new ArgumentPack(rv);
                return true;
            }
        }

        private static char[] SEPARATORS = new char[] { ' ', '\t', '\r', '\n' };
        private static string[] TryProcessImpl(string cmd) {
            var args = new List<string>();
            var beg = 0;
            var lastQuote = '\0';

            for (var i = 0; beg + i < cmd.Length; ++i) {
                var c = cmd[beg + i];
                // Quoted strings are packed as a whole.
                if (c == '\'' || c == '\"' || c == '`') {
                    // There is no opening quote before.
                    if (lastQuote == '\0') {
                        lastQuote = c;
                        args.AddRange(cmd.Substring(beg, i)
                            .Split(SEPARATORS,
                                StringSplitOptions.RemoveEmptyEntries));
                        beg += i + 1; // The char next to the quote.
                        i = 0; // Reset length indicator.
                    }
                    // Quote opened, we are trying closing it.
                    else {
                        // Close the quotes and pack text in quotes as a whole.
                        if (c == lastQuote) {
                            lastQuote = '\0';
                            args.Add(cmd.Substring(beg, i));
                            beg += i + 1;
                            i = 0; // Reset length indicator.
                        }
                        // Quotes doesn't match => ignore.
                    }
                }
            }
            // Now, we check whether the quote is still open. (It shouldn't)
            if (lastQuote != '\0') {
                return null;
            }
            // Add all remaining args if necessary.
            if (beg != cmd.Length) {
                args.AddRange(cmd.Substring(beg)
                    .Split(SEPARATORS,
                        StringSplitOptions.RemoveEmptyEntries));
            }
            return args.ToArray();
        }

        public string this[int i] {
            get => _inner[i];
        }

        public IEnumerator<string> GetEnumerator() {
            return ((IEnumerable<string>)_inner).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<string>)_inner).GetEnumerator();
        }
    }
}
