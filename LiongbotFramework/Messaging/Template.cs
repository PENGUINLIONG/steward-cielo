using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Liongbot.Messaging {
    /// <summary>
    /// Message placeholder for lazy injection of message object.
    /// </summary>
    public class Placeholder : Message {
        public override bool Equals(Message other) {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Message template that allow lazy injection of segments, so that message
    /// can be made flexibly.
    /// </summary>
    public class Template {
        private readonly Message[] _template;
        private readonly int[] _phPos; // Index position of placeholders.

        public Template(params Message[] template) {
            var temp = new List<Message>(template.Length);
            var tempPos = new List<int>();
            // Indices where immediate (non-lazy) messages have been inserted.
            var pos = 0;
            for (var i = 0; i < template.Length; ++i) {
                if (template[i] is Placeholder) {
                    tempPos.Add(pos);
                } else {
                    temp.Add(template[i]);
                    ++pos;
                }
            }
            _template = temp.ToArray();
            _phPos = tempPos.ToArray();
        }

        /// <summary>
        /// Make message.
        /// </summary>
        /// <param name="lazy">Messages lazily passed in to fill the
        /// placeholders.</param>
        /// <returns>Massage filled with variable messages.</returns>
        public Message MakeMessage(params Message[] lazy) {
            Contract.Requires(_phPos.Length == lazy.Length);

            // Immediate response for corner cases.
            if (_phPos.Length == 0) {
                return new Compound(_template);
            } else if (_phPos.Length == 1 && _template.Length == 0) {
                return lazy[0];
            } else if (_phPos.Length == 0 && _template.Length == 1) {
                return _template[0];
            }

            var msgCount = _phPos.Length + _template.Length;
            var rv = new List<Message>(msgCount);

            var curVar = 0; // Current variable index.
            for (var i = 0; i < _template.Length; ++i) {
                // When a segment should be placed in the current place.
                while (curVar < _phPos.Length && i == _phPos[curVar]) {
                    rv.Add(lazy[curVar]);
                    ++curVar;
                }
                rv.Add(_template[i]);
            }
            // For placeholders occur at the end.
            for (; curVar < lazy.Length; ++curVar) {
                rv.Add(lazy[curVar]);
            }
            return new Compound(rv);
        }
    }

    public abstract class UserTemplate {
        Template _inner;
        public UserTemplate() {
            _inner = new Template(TemplateMessage);
        }

        protected abstract Message[] TemplateMessage { get; }
        protected Message Image(string path) {
            return new Image(path);
        }
        protected Message Record(string path) {
            return new Record(path);
        }
        protected Message At(string qq) {
            return new At(qq);
        }
        protected Message Placeholder() {
            return new Placeholder();
        }
        
        public Message MakeMessage(params Message[] lazy) {
            return _inner.MakeMessage(lazy);
        }
    }
}
