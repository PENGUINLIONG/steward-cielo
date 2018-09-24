using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Liongbot.Messaging {
    /// <summary>
    /// Components of a CoolQ message. A message should not store state through
    /// any mean.
    /// </summary>
#pragma warning disable CS0659 // Just don't use any message as collection key.
    public abstract class Message : IEquatable<Message> {
#pragma warning restore CS0659
        public static implicit operator Message(string text) =>
            new Text(text) as Message;
        /// <summary>
        /// Get text content for a required Length. The actual length might
        /// exceed the number. If there is no enough content the returned text
        /// might be shorter than required.
        /// </summary>
        /// <param name="length">Number of chars needed for output. A negatice
        /// length indicates that the entire string is returned.</param>
        /// <returns>Textual content of message of length `requiredLength` at
        /// most.</returns>
        public virtual string GetText(int length) =>
            length != 0 ? "\uFFFD" : string.Empty;

        public override string ToString() {
            return GetText(-1);
        }
        public static readonly Message Empty = "";

        /// <summary>
        /// Make a message by formatting. E.g., `Yes{0}Ok` is formatted with
        /// `{ Image }` to be Compound{ "Yes", Image, "Ok" }.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="msgs">Messages.</param>
        /// <returns>Formatted message.</returns>
        public static Message Format(string format, params Message[] msgs) {
            if (string.IsNullOrEmpty(format)) {
                // Empty message.
                return Empty;
            }
            var rv = new List<Message>();
            var sb = new StringBuilder();
            var leftPos = -2;
            var rightPos = -1;
            for (var i = 0; i < format.Length; ++i) {
                var c = format[i];
                if (c == '{') {
                    if (rightPos > 0) {
                        throw new ArgumentException("Invalid format stirng.");
                    }
                    if (leftPos == i - 1) {
                        // Escape "{{".
                        sb.Append('{');
                        leftPos = -1;
                    } else {
                        // Found '{'.
                        leftPos = i;
                    }
                } else if (c == '}') {
                    if (leftPos >= 0) {
                        var index = format.Substring(leftPos + 1,
                                                     i - leftPos - 1);
                        leftPos = -1;
                        var msg = msgs[Convert.ToInt32(index)];
                        if (msg is Text text) {
                            sb.Append(text.Content);
                        } else {
                            if (sb.Length > 0) {
                                rv.Add(sb.ToString());
                                sb.Clear();
                            }
                            rv.Add(msg);
                        }
                    } else if (rightPos == i - 1) {
                        // Escape "}}".
                        sb.Append('}');
                        rightPos = -1;
                    } else {
                        rightPos = i;
                    }
                } else {
                    if (rightPos > 0) {
                        throw new ArgumentException("Invalid format stirng.");
                    } else if (leftPos >= 0) {
                        continue;
                    }
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) {
                rv.Add(sb.ToString());
            }
            if (rv.Count > 1) {
                return new Compound(rv);
            } else {
                return rv[0];
            }
        }

        private StringBuilder EscapeBrackets(string str) {
            return new StringBuilder(str)
                .Replace("{", "{{")
                .Replace("}", "}}");
        }
        public (string, Message[]) ToFormat() {
            if (this is Compound cmp) {
                var sb = new StringBuilder();
                var list = new List<Message>();
                foreach (var seg in cmp.Segments) {
                    if (seg is Text text) {
                        sb.Append(EscapeBrackets(text.Content));
                        continue;
                    } else {
                        sb.Append("{");
                        sb.Append(list.Count);
                        sb.Append("}");
                        list.Add(seg);
                    }
                }
                return (sb.ToString(), list.ToArray());
            } else if (this is Text txt) {
                return (EscapeBrackets(txt.Content).ToString(),
                        new Message[] { });
            } else {
                return ("{0}", new[] { this });
            }
        }

        public override bool Equals(object obj) =>
            obj is Message msg && (this as IEquatable<Message>).Equals(msg);

        /// <summary>
        /// Check if the current instance equals the other one from referential
        /// or non-referential aspect.
        /// </summary>
        /// <param name="other">Instance to be compared.</param>
        /// <returns>True if the other instance is considered equal to this
        /// instance.</returns>
        public abstract bool Equals(Message other);
    }

    /// <summary>
    /// Base class of all messages where a physical file is involved.
    /// </summary>
    public abstract class FileMessage : Message {
        public FileMessage(string path) {
            Path = path;
        }

        /// <summary>
        /// Absolute path to local file.
        /// </summary>
        public string Path {
            get; private set;
        }
    }

    // (liong) Use `new Text(string.Empty)` for empty message.

    /// <summary>
    /// Plain text.
    /// </summary>
    public class Text : Message {
        public Text() {
            Content = string.Empty;
        }
        public Text(string content) {
            Content = content;
        }

        public string Content { get; private set; }

        public override string GetText(int length) =>
            length < 0 ? Content :
                Content.Substring(0, Math.Min(length, Content.Length));

        public override bool Equals(Message other) =>
            other != null && other is Text txt && Content == txt.Content;
    }

    /// <summary>
    /// A @ message that refer to an existing member in group chat.
    /// </summary>
    public class At : Message {
        public string Referee { get; private set; }

        public At(string qq) {
            Contract.Requires(CheckFormat(qq));
            Referee = qq;
        }

        // All QQ number strings contain only digits.
        private bool CheckFormat(string qq) {
            foreach (var c in qq) {
                if (!Char.IsDigit(c)) {
                    return false;
                }
            }
            return true;
        }
        public override string GetText(int length) {
            if (length < 0) {
                return string.Concat("@" + Referee);
            } else if (length == 0) {
                return string.Empty;
            } else {
                return string.Concat(
                    "@",
                    Referee.Substring(0, Math.Min(length - 1, Referee.Length)));
            }
        }

        public override bool Equals(Message other) =>
            other != null && other is At at && Referee == at.Referee;
    }

    /// <summary>
    /// Inline image file.
    /// </summary>
    public class Image : FileMessage {
        public Image(string path) : base(path) { }

        public override bool Equals(Message other) {
            if (other != null && other is Image img) {
                return Path == img.Path;
            } else {
                return false;
            }
        }
    }

    /// <summary>
    /// Audio record.
    /// </summary>
    public class Record : FileMessage {
        public Record(string path) : base(path) { }

        public override bool Equals(Message other) =>
            other != null && other is Record rec && Path == rec.Path;
    }

    /// <summary>
    /// Message that is composited by multiple message segments.
    /// </summary>
    public class Compound : Message {
        public Message[] Segments { get; private set; }
        public Compound(params Message[] msgs)
            : this(msgs as IEnumerable<Message>) {
        }
        public Compound(IEnumerable<Message> msgs) {
            var segs = new List<Message>();
            foreach (var msg in msgs) {
                // Flatten message to prevent too-deep recursion.
                if (msg is Compound) {
                    segs.AddRange((msg as Compound).Segments);
                } else {
                    segs.Add(msg);
                }
            }
            Segments = segs.ToArray();
        }

        public override string GetText(int length) {
            if (length < 0) {
                var sb = new StringBuilder();
                return string.Concat(from seg in Segments
                                     select seg.GetText(length));
            } else {
                var sb = new StringBuilder(length);
                foreach (var seg in Segments) {
                    var temp = seg.GetText(length);
                    sb.Append(temp);
                    length -= temp.Length;
                    if (length <= 0) break;
                }
                return sb.ToString();
            }
        }

        public override bool Equals(Message other) =>
            other != null && other is Compound cmp &&
            Segments.Length == cmp.Segments.Length &&
            (from a in Segments from b in cmp.Segments
             select a.Equals(b)).All((x) => x);
    }

    /// <summary>
    /// Raw message. Composer MUST NOT be escape any characters in the content.
    /// </summary>
    public class Raw : Message {
        public string Content { get; private set; }
        public Raw(string content) {
            Content = content;
        }
        public override string GetText(int length) =>
            length < 0 ? Content :
                Content.Substring(0, Math.Min(length, Content.Length));

        public override bool Equals(Message other) =>
            other != null && other is Raw raw && Content == raw.Content;
    }
}
