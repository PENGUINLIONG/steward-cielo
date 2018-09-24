using System;
using Liongbot.Command;
using Liongbot.Command.Attributes;
using Liongbot.Dispatch;
using Liongbot.Messaging;

namespace StewardCielo.Backends {
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
    }

    public class LaundryBackEnd : IBackEnd {
        public class MachineState {
            public DateTime Thru;
            public string By;
        }
        public class LaundryState {
            public MachineState LeftDry;
            public MachineState RightDry;
            public MachineState LeftWash;
            public MachineState RightWash;
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
                    res = _parser.CommandProfile.Docs;
                    return true;
                case "occupy":
                    lock (_state) {
                        switch(cmd.Machine) {
                        case "left-dry":
                            if (CheckMachine(_state.LeftDry)) {
                                _state.LeftDry = new MachineState {
                                    By = msg.Metadata.UserName,
                                    Thru = DateTime.Now.AddMinutes(45),
                                };
                                res = Summarize();
                            } else {
                                res = AlreadyOccupied("left-dry", _state.LeftDry);
                            }
                            return true;
                        case "right-dry":
                            if (CheckMachine(_state.RightDry)) {
                                _state.RightDry = new MachineState {
                                    By = msg.Metadata.UserName,
                                    Thru = DateTime.Now.AddMinutes(45),
                                };
                                res = Summarize();
                            } else {
                                res = AlreadyOccupied("right-dry", _state.RightDry);
                            }
                            return true;
                        case "left-wash":
                            if (CheckMachine(_state.LeftWash)) {
                                _state.LeftWash = new MachineState {
                                    By = msg.Metadata.UserName,
                                    Thru = DateTime.Now.AddMinutes(30),
                                };
                                res = Summarize();
                            } else {
                                res = AlreadyOccupied("left-wash", _state.LeftWash);
                            }
                            return true;
                        case "right-wash":
                            if (CheckMachine(_state.RightWash)) {
                                _state.RightWash = new MachineState {
                                    By = msg.Metadata.UserName,
                                    Thru = DateTime.Now.AddMinutes(30),
                                };
                                res = Summarize();
                            } else {
                                res = AlreadyOccupied("right-wash", _state.RightWash);
                            }
                            return true;
                        default:
                            break;
                        }
                    }
                    res = "..What?\n" + _parser.CommandProfile.Docs;
                    return true;
                default:
                    res = Summarize();
                    return true;
                }
            } else {
                res = null;
                return false;
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
