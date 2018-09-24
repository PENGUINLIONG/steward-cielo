using Liongbot.Command;
using Liongbot.Command.Attributes;
using Liongbot.Messaging;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Liongbot.Dispatch {
    [Command(Name = "Dispatcher")]
    [Doc("Dispatcher remote controller/monitor.")]
    struct DispatcherCommand {
        [FreeArg]
        [Doc("Operation code. Can be `enable`, `disable`, `help` or `stat`")]
        public string Operation;
        [FreeArg]
        [Doc("Argument. For en-/dis-able operation it's the GUID of back end;" +
             " for stat it can be `defail` to instruct this back end to " +
             "provide you with more information.")]
        public string Argument;
    }

    class DispatcherBackEnd : IBackEnd {
        private ISyntaxProvider _sp;
        private CommandParser<DispatcherCommand> _parser;
        private Dispatcher _ref;
        public DispatcherBackEnd(Dispatcher d, ISyntaxProvider sp) {
            Contract.Requires(sp != null);
            _sp = sp;
            _parser = new CommandParser<DispatcherCommand>(sp);
            _ref = d;
        }

        public BackEndMetadata Metadata => new BackEndMetadata {
            Author = "PENGUINLIONG",
            Description = "Monitor and control over dispatcher via commands.",
            FriendlyName = "Dispatcher",
            Identity = Guid.Parse("C988AA07-DC17-43A3-86B4-525265B9DDC2"),
            Version = "0.1.0"
        };

        public bool AcceptRaw => false;
        public bool ThreadSafe => true;

        public bool Launch() => true;

        public bool Preview(IncomingMessage msg) =>
            _parser.MatchName(msg.Message);

        public bool Respond(IncomingMessage msg, out Message res) {
            if (_parser.TryParse(msg.Message, out DispatcherCommand cmd)) {
                switch (cmd.Operation) {
                case "enable": {
                    if (Guid.TryParse(cmd.Argument, out Guid guid)) {
                        if (guid == Metadata.Identity) {
                            res = "???";
                        } else {
                            _ref.Enable(guid);
                            res = $"\u2705 {cmd.Argument}";
                        }
                        return true;
                    }
                    break;
                }
                case "disable": {
                    if (Guid.TryParse(cmd.Argument, out Guid guid)) {
                        if (guid == Metadata.Identity) {
                            res = "\u3010\u3081\u3050\u308b\u3011\n\u300c" +
                                "\u3046\u308f\u3041\u3001\u3053\u306e\u4eba" +
                                "\u30c0\u30fc\u30e1\u3060\u3041\u300d";
                        } else {
                            _ref.Disable(guid);
                            res = $"\u274e {cmd.Argument}";
                        }
                        return true;
                    }
                    break;
                }
                case "help": {
                    res = _parser.CommandProfile.Docs;
                    return true;
                }
                case "stat": {
                    lock (_ref) {
                        if (cmd.Argument == "detail") {
                            res = string.Join('\n',
                                from back in _ref._backEnds
                                select string.Format(
                                    "{{{0}}}\n{1} - {2}({3}) @{4}\n{5}",
                                    back.Metadata.Identity,
                                    back.IsEnabled ? "\u2605" : "\u274e",
                                    back.Metadata.FriendlyName,
                                    back.Metadata.Version,
                                    back.Metadata.Author,
                                    back.Metadata.Description));
                        } else {
                            res = string.Join('\n',
                                from back in _ref._backEnds
                                select string.Format(
                                    "{0} - {1} @{2}\n{3}",
                                    back.IsEnabled ? "\u2605" : "\u274e",
                                    back.Metadata.FriendlyName,
                                    back.Metadata.Author,
                                    back.Metadata.Description));
                        }
                        return true;
                    }
                }
                }
            }
            res = null;
            return false;
        }

        public void ShutDown() { }
    }
}
