using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Liongbot.Command.SyntaxProviders;
using Liongbot.Messaging;

namespace Liongbot.Dispatch {
    class BackEndInfo {
        public IBackEnd Inner { get; set; }
        public BackEndMetadata Metadata { get; set; }
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
        public object SyncRoot{ get; set; }

        public void Enable() => IsEnabled = true;
        public void Disable() => IsEnabled = false;
    }

    public class Dispatcher {
        internal List<BackEndInfo> _backEnds;
        private List<IFrontEnd> _frontEnds;
        public bool IsLaunched { get; private set; }
        public IComposer Composer { get; private set; }
        // This value is cached on lauch, if no back end need message
        // decomposition (all back end declared `AcceptRaw`), then message will
        // not be decomposed at all.
        private bool _rawOnly = true;

        public Dispatcher(IComposer composer) {
            Composer = composer;
            _backEnds = new List<BackEndInfo>();
            var inner = new DispatcherBackEnd(this, new SimpleSyntaxProvider());
            _backEnds.Add(new BackEndInfo {
                Inner = inner,
                Metadata = inner.Metadata,
                Priority = int.MaxValue,
                IsEnabled = false,
            });
            _frontEnds = new List<IFrontEnd>();
        }

        /// <summary>
        /// Register a back end to this dispatcher. If the dispatcher is in
        /// launched state, the provided backend will not be added.</summary>
        /// <param name="frontEnd">Back end to be added.</param>
        /// <returns>True is the back end is added successfully; false
        /// otherise.</returns>
        public bool AddBackEnd(IBackEnd backEnd, int priority = 0) {
            if (!IsLaunched) {
                _backEnds.Add(new BackEndInfo {
                    Inner = backEnd,
                    Metadata = backEnd.Metadata,
                    Priority = priority,
                    IsEnabled = false,
                    SyncRoot = backEnd.ThreadSafe ? null : new object(),
                });
                return true;
            } else {
                return false;
            }
        }
        /// <summary>
        /// Register a front end to this dispatcher. If the dispatcher is in
        /// launched state, the provided front end will not be added.</summary>
        /// <param name="frontEnd">Front end to be added.</param>
        /// <returns>True is the front end is added successfully; false
        /// otherise.</returns>
        public bool AddFrontEnd(IFrontEnd frontEnd) {
            if (!IsLaunched) {
                frontEnd.MessageReceived += (sender, args) => {
                    Task.Run(() => {
                        Message response = null;
                        // Check if there is a valid response.
                        if (Forward(args.RawMessage,
                                    args.Metadata,
                                    out response) &&
                            response != null) {
                            // Send the response back to front end.
                            sender.Send(args.Metadata,
                                        Composer.Compose(response));
                        } else {
                            sender.Discard(args.Metadata);
                        }
                        // No response... Let it go.
                    });
                };
                _frontEnds.Add(frontEnd);
                return true;
            } else {
                return false;
            }
        }

        public bool LaunchImpl() {
            var tasks = new Task[_backEnds.Count];
            try {
                // We want to backends to be sorted from high to low by
                // priority.
                _backEnds.Sort((a, b) => b.Priority - a.Priority);
                for (int i = 0; i < _backEnds.Count; ++i) {
                    var backInfo = _backEnds[i];
                    _rawOnly = _rawOnly && backInfo.Inner.AcceptRaw;
                    tasks[i] = Task.Run(() =>
                        backInfo.IsEnabled = backInfo.Inner.Launch());
                }
            } catch (Exception) {
                ShutDownImpl();
                return false;
            }
            Task.WaitAll(tasks);
            IsLaunched = true;
            return true;
        }
        public bool Launch() {
            lock (this) {
                return LaunchImpl();
            }
        }
        public void ShutDownImpl() {
            var tasks = new Task[_backEnds.Count];
            for (int i = 0; i < _backEnds.Count; ++i) {
                var backInfo = _backEnds[i];
                backInfo.IsEnabled = false;
                tasks[i] = Task.Run(() => backInfo.Inner.ShutDown());
            }
            Task.WaitAll(tasks);
            IsLaunched = false;
        }
        public void ShutDown() {
            lock (this) {
                ShutDownImpl();
            }
        }
        /// <summary>
        /// Allow forwarding message to back end identified by guid.
        /// </summary>
        /// <param name="guid">GUID of the plugin.</param>
        /// <returns>True if the back end is enabled; false on failure.
        /// </returns>
        public bool Enable(Guid guid) {
            lock (this) {
                if (!IsLaunched) return false;
                var back = _backEnds
                    .FirstOrDefault((info) => info.Metadata.Identity == guid);
                if (back == null) {
                    return false;
                } else {
                    back.Enable();
                    return true;
                }
            }
        }
        /// <summary>
        /// Prevent forwarding message to back end identified by guid.
        /// </summary>
        /// <param name="guid">GUID of the plugin.</param>
        /// <returns>True if the back end is disabled; false on failure.
        /// </returns>
        public bool Disable(Guid guid) {
            lock (this) {
                if (!IsLaunched) return false;
                var back = _backEnds
                    .FirstOrDefault((info) => info.Metadata.Identity == guid);
                if (back == null) {
                    return false;
                } else {
                    back.Disable();
                    return true;
                }
            }
        }

        private bool Forward(string raw,
                             MessageMetadata meta,
                             out Message res) {
            Message decomposed = null;
            // Decompose only when there is someone declared not raw-accepting.
            if (!_rawOnly) {
                decomposed = Composer.Decompose(raw);
            } 
            foreach (var back in _backEnds) {
                // Only work on enabled back ends.
                if (!back.IsEnabled) continue;
                IncomingMessage msg;
                if (back.Inner.AcceptRaw) {
                    msg = new IncomingMessage {
                        Message = new Raw(raw),
                        Metadata = meta
                    };
                } else {
                    msg = new IncomingMessage {
                        Message = decomposed,
                        Metadata = meta
                    };
                }
                if (back.SyncRoot == null) {
                    if (back.Inner.Preview(msg)) {
                        return back.Inner.Respond(msg, out res);
                    }
                } else {
                    lock (back.SyncRoot) {
                        if (back.Inner.Preview(msg)) {
                            return back.Inner.Respond(msg, out res);
                        }
                    }
                }
            }
            res = null;
            return false;
        }
    }
}
