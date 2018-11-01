using System;
using Liongbot.Command;
using Liongbot.Command.Attributes;
using Liongbot.Dispatch;
using Liongbot.Messaging;

namespace StewardCielo.BackEnds {
    public class SummaryMessage : UserTemplate {
        protected override Message[] TemplateMessage => new Message[] {
            "left-dry: ", Placeholder(), "\n" +
            "right-dry: ", Placeholder(), "\n" +
            "left-wash: ", Placeholder(), "\n" +
            "right-wash: ", Placeholder()
        };
    }
    public class AlreadyOccupiedMessage : UserTemplate {
        protected override Message[] TemplateMessage => new Message[] {
            "The machine `", Placeholder(), "` is already occupied by ",
            Placeholder(), ". It will be available at ", Placeholder(), "."
        };
    }
    [Command(Name = "Laundry")]
    [Doc("Laundry occupancy manager.")]
    public struct LaundryCommand {
        [FreeArg(Default = "")]
        [Doc("`occupy`, or `help` to print this message.")]
        public string Verb;
        [FreeArg(Default = "")]
        [Doc("`left-dry`, `right-dry`, `left-wash` or `right-wash`.")]
        public string Machine;
        [FreeArg(Default = 40)]
        public int Minutes;
    }

    public class LaundryBackEnd : IBackEnd {
        public class MachineState {
            public DateTime From;
            public DateTime Thru;
            public string By;
            public string ById;
        }
        public class LaundryState {
            public MachineState LeftDry = new MachineState();
            public MachineState RightDry = new MachineState();
            public MachineState LeftWash = new MachineState();
            public MachineState RightWash = new MachineState();
        };

        private static LaundryState _state = State.Get<LaundryState>();
        private static readonly SummaryMessage _summary = new SummaryMessage();
        private static readonly AlreadyOccupiedMessage _alreadyOccupied = new AlreadyOccupiedMessage();
        private static CommandParser<LaundryCommand> _parser;

        public LaundryBackEnd(ISyntaxProvider _syntax) {
            _parser = new CommandParser<LaundryCommand>(_syntax);
            State.Persistence += (sender, args) => State.Store(_state);
        }

        public BackEndMetadata Metadata => new BackEndMetadata {
            Identity = Guid.Parse("B4FF9546-CC99-4CDC-81E6-049295044C97"),
            FriendlyName = "Laundry",
            Author = "Liong",
            Version = "0.1.0",
            Description = "This washing machine is occupied!"
        };

        public bool AcceptRaw => false;
        public bool ThreadSafe => true;

        public bool Launch() => true;
        public void ShutDown() { }

        public bool Preview(IncomingMessage msg) {
            return _parser.MatchName(msg.Message);
        }
        public bool Respond(IncomingMessage msg, out Message res) {
            if (_parser.TryParse(msg.Message, out LaundryCommand cmd)) {
                switch (cmd.Verb.ToLower()) {
                case "help":
                    res = "To query the occupation status:\n" + 
                          "    /laundry\n" +
                          "To occupy an machine:\n" +
                          "    /laundry occupy left-dry\n" +
                          "By default, the time for each use is set to 40. To specify a more detailed number of minutes, which is shown on the washing machine's display:\n" +
                          "    /laundry occupy right-wash 30\n" +
                          "To release an unintentionally occupied machine:" +
                          "    /laundry release left-wash";
                    break;
                case "occupy": lock (_state) {
                    res = OccupyMachine(cmd.Machine, msg.Metadata, cmd.Minutes);
                    break;
                }
                case "release": lock (_state) {
                    res = ReleaseMachine(cmd.Machine, msg.Metadata.UserName);
                    break;
                }
                case "status":
                    res = Summarize();
                    break;
                default:
                    res = Summarize();
                    break;
                }
                return true;
            } else {
                res = null;
                return false;
            }
        }

        private Message OccupyMachine(string machineName, MessageMetadata meta, int minutes) {
            var state = GetMachineState(machineName);
            var now = DateTime.Now;
            if (CheckMachine(state)) {
                state.By = meta.UserName;
                state.ById = meta.UserId;
                state.From = now;
                state.Thru = now.AddMinutes(minutes);
                return Summarize();
            } else {
                return AlreadyOccupied(machineName, state);
            }
        }
        private Message ReleaseMachine(string machineName, string userName) {
            var state = GetMachineState(machineName);
            if (state is null) {
                return Summarize();
            }
            if (state.By == userName) {
                if (!CheckMachine(state)) {
                    state.Thru = DateTime.MinValue;
                }
                return Summarize();
            } else {
                return "You are not the current user.";
            }
        }

        private MachineState GetMachineState(string name) {
            switch (name) {
            case "left-dry": return _state.LeftDry;
            case "right-dry": return _state.RightDry;
            case "left-wash": return _state.LeftWash;
            case "right-wash": return _state.RightWash;
            default: return null;
            }
        }


        private bool CheckMachine(MachineState state) => state == null || state.Thru < DateTime.Now;

        private string SummarizeState(MachineState state) {
            if (CheckMachine(state)) {
                return "ready";
            } else {
                return $"occupied by {state.By} til {state.Thru.ToShortTimeString()}";
            }
        }
        private Message Summarize() {
            return _summary.MakeMessage(SummarizeState(_state.LeftDry),
                                        SummarizeState(_state.RightDry),
                                        SummarizeState(_state.LeftWash),
                                        SummarizeState(_state.RightWash));
        }
        private Message AlreadyOccupied(string name, MachineState state) {
            return _alreadyOccupied.MakeMessage(name, state.By, state.Thru.ToShortTimeString());
        }
    }
}
