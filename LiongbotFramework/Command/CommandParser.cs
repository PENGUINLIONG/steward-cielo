using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Liongbot.Messaging;

namespace Liongbot.Command {
    /// <summary>
    /// Class that help processing commands.
    /// </summary>
    public class CommandParser<T> where T : struct {
        public CommandProfile CommandProfile { get; private set; }
        public ISyntaxProvider SyntaxProvider => CommandProfile.SyntaxProvider;
        public CommandParser(ISyntaxProvider sp) {
            CommandProfile = CommandProfile.Of<T>(sp);
        }
        public CommandParser(CommandProfile cp) {
            CommandProfile = cp;
        }

        public bool MatchName(Message message) {
            return CommandProfile.MatchName(message);
        }

        /// <summary>
        /// Unprotectedly parse a command if and only if the command has a same
        /// name as the profile record, but you will be able to catch exceptions
        /// occured inside. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public T Parse(Message msg) =>
            Parse(ArgumentPack.Process(msg.ToString()));
        /// <summary>
        /// Unprotectedly parse a command if and only if the command has a same
        /// name as the profile record, but you will be able to catch exceptions
        /// occured inside. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="cmd">Command string.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public T Parse(string cmd) =>
            Parse(new ArgumentPack(cmd));
        /// <summary>
        /// Unprotectedly parse a command if and only if the command has a same
        /// name as the profile record, but you will be able to catch exceptions
        /// occured inside. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public T Parse(ArgumentPack args) {
            if (!CommandProfile.MatchName(args)) {
                throw new CommandException("Names mismatched.");
            }
            var aj = new ArgumentInjector(CommandProfile);
            CommandProfile.SyntaxProvider.Parse(aj, args);
            return (T)aj.ArgumentObject;
        }
        /// <summary>
        /// Try to parse a command if and only if the command has a same name as
        /// the profile record. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public bool TryParse(Message msg, out T rv) =>
            TryParse(msg.ToString(), out rv);
        /// <summary>
        /// Try to parse a command if and only if the command has a same name as
        /// the profile record. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="cmd">Command string.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public bool TryParse(string cmd, out T rv) {
            if (ArgumentPack.TryProcess(cmd, out ArgumentPack args)) {
                return TryParse(args, out rv);
            } else {
                rv = default(T);
                return false;
            }
        }
        /// <summary>
        /// Try to parse a command if and only if the command has a same name as
        /// the profile record. Here, `args` and `cmd` represent the same idea.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>The parsed command struct on success; null otherwise.
        /// </returns>
        public bool TryParse(ArgumentPack args, out T rv) {
            if (!CommandProfile.MatchName(args)) {
                rv = default(T);
                return false;
            }
            try {
                var aj = new ArgumentInjector(CommandProfile);
                CommandProfile.SyntaxProvider.Parse(aj, args);
                rv = (T)aj.ArgumentObject;
                return true;
            } catch (Exception) {
                rv = default(T);
                return false;
            }
        }
    }
}
