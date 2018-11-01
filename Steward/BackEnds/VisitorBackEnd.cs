using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Liongbot.Messaging;
using Liongbot.Dispatch;
using Liongbot.Command;
using Liongbot.Command.Attributes;
using System.Text;

namespace StewardCielo.BackEnds {
    public class VisitorBackEnd : IBackEnd {
        [Command(Name = "Visitor")]
        [Doc("Visitor declaration manager.")]
        public struct VisitorCommand {
            [FreeArg(Default = "")]
            public string Verb;
        }
        private CommandParser<VisitorCommand> _parser;

        public class VisitorEntryState {
            public string By;
            public string ById;
            public DateTime EstimatedArrivalTime;
        }
        public class VisitorState {
            public List<VisitorEntryState> Entries;
        }

        VisitorState _state = State.Get<VisitorState>();

        private void RemoveExpiredStates() {
            _state.Entries.RemoveAll(state => {
                var time = state.EstimatedArrivalTime + TimeSpan.FromHours(2);
                return time < DateTime.Now;
            });
        }
        public VisitorBackEnd(ISyntaxProvider _syntax) {
            _parser = new CommandParser<VisitorCommand>(_syntax);
            State.Persistence += (sender, args) => {
                RemoveExpiredStates();
                State.Store(_state);
            };
        }

        public BackEndMetadata Metadata => new BackEndMetadata {
            Author = "PENGUINLIONG",
            Description = "Declare visitors for everyone's safety.",
            FriendlyName = "Visitor Manager",
            Identity = Guid.Parse("CD4D994C-E966-47C4-8503-AF1B843EB565"),
            Version = "0.1.0",
        };

        public bool AcceptRaw => false;
        public bool ThreadSafe => true;

        public bool Launch() => true;
        public void ShutDown() { }

        public bool Preview(IncomingMessage msg) {
            return _parser.MatchName(msg.Message);
        }
        public bool Respond(IncomingMessage msg, out Message res) {
            if (_parser.TryParse(msg.Message, out VisitorCommand cmd)) {
                if (!string.IsNullOrEmpty(cmd.Verb) &&
                    DateTime.TryParse(cmd.Verb, out DateTime est)) {
                    VisitorEntryState entry = new VisitorEntryState {
                        By = msg.Metadata.UserName,
                        ById = msg.Metadata.UserId,
                        EstimatedArrivalTime = est,
                    };
                    State.Journal(entry);
                    _state.Entries.Add(entry);
                }
                res = PrintEntries();
                return true;
            }
            res = null;
            return false;
        }
        private string PrintEntries() {
            RemoveExpiredStates();
            StringBuilder sb = new StringBuilder();
            foreach (var entry in _state.Entries) {
                sb.AppendLine($"{entry.By} declared stranger visits at about {entry.EstimatedArrivalTime.ToShortTimeString()}");
            }
            return sb.ToString();
        }
    }
}
