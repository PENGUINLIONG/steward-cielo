using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;
using Liongbot.Command.Attributes;
using System.Diagnostics.Contracts;
using System.Collections.ObjectModel;
using Liongbot.Messaging;
using System.Text;

namespace Liongbot.Command {
    /// <summary>
    /// Description of named arguments.
    /// </summary>
    public class ArgumentInfo {
        public string Name { get; internal set; } // Full name or abbreviate.
        public bool IsSwitch { get; internal set; }
        public FieldInfo Field { get; internal set; }
    }

    /// <summary>
    /// Description of free arguments.
    /// </summary>
    public class FreeArgumentInfo {
        public int Position { get; internal set; }
        public bool IsListReceptor { get; internal set; }
        public FieldInfo Field { get; internal set; }
    }

    public class Documentation {
        public string Name { get; internal set; }
        public string Abbreviate { get; internal set; }
        public string Description { get; internal set; }
    }

    /// <summary>
    /// Description of fields' default values, null for using CLR's default
    /// value.
    /// </summary>
    public class FieldDefault {
        public FieldInfo Field { get; internal set; }
        public object Default { get; internal set; }
    }

    /// <summary>
    /// Profile of command with description about how literal arguments can be
    /// mapped into structure fields.
    /// </summary>
    public class CommandProfile {
        public string Name { get; private set; }
        public string Docs { get; private set; }
        public ISyntaxProvider SyntaxProvider { get; private set; }
        public IReadOnlyDictionary<string, ArgumentInfo> NamedArgs {
            get; private set;
        }
        public IReadOnlyList<FreeArgumentInfo> FreeArgs { get; private set; }
        public Type CommandType { get; private set; }
        public bool HasListReceptor { get => FreeArgs.Last().IsListReceptor; }
        private FieldDefault[] _fieldDefaults;

        public CommandProfile(Type cmd, ISyntaxProvider sp) {
            var cmdAttr = cmd.GetCustomAttribute<CommandAttribute>();
            Contract.Requires(cmdAttr != null);
            var cmdName = cmdAttr.Name;
            if (string.IsNullOrEmpty(cmdName)) {
                cmdName = cmd.Name;
            }
            Name = sp.NameCommand(SplitSymbolName(cmdName));
            SyntaxProvider = sp;
            var named = new List<ArgumentInfo>();
            var free = new List<FreeArgumentInfo>();
            var @default = new List<FieldDefault>();
            var docs = new List<Documentation>();
            foreach (var field in cmd.GetFields()) {
                _ = TryAsNamed(field, named, SyntaxProvider, @default, docs) ||
                    TryAsFree(field, free, @default, docs);
            }
            NamedArgs = new ReadOnlyDictionary<string, ArgumentInfo>(
                named.ToDictionary((info) => info.Name));
            FreeArgs = free.AsReadOnly();
            Docs = CompileDocumentation(FetchDoc(cmd), docs.AsReadOnly());
            _fieldDefaults = @default.ToArray();
            CommandType = cmd;
        }

        /// <summary>
        /// Determine whether the textual command represented by the argument
        /// pack is about this profile.
        /// </summary>
        /// <param name="args">Argument pack.</param>
        /// <returns>True if the first argument has a same name as the profiled
        /// command.</returns>
        public bool MatchName(ArgumentPack args) {
            return args.Count > 0 && args[0] == Name;
        }
        /// <summary>
        /// Determine whether the textual command represented by the message is
        /// about this profile.
        /// </summary>
        /// <param name="message">Command message.</param>
        /// <returns>True if the message has the profiled command's name
        /// preceeding.</returns>
        public bool MatchName(Message message) {
            return SyntaxProvider.SyntacticEqual(
                message.GetText(Name.Length),
                Name);
        }

        private string CompileDocumentation(
                string description,
                IEnumerable<Documentation> docs) {
            var sb = new StringBuilder();
            sb.AppendLine(description);
            sb.Append("USAGE: ");
            sb.Append(Name);
            sb.AppendLine(" OPTIONS");
            sb.AppendLine("OPTIONS:");
            foreach (var doc in docs) {
                if (doc.Abbreviate != null && doc.Name != null) {
                    sb.AppendLine(string.Format("  {0}, {1} {2}",
                                  doc.Abbreviate, doc.Name, doc.Description));
                } else {
                    if (doc.Abbreviate != null) {
                        sb.AppendLine($"  {doc.Abbreviate} {doc.Description}");
                    } else {
                        sb.AppendLine($"  {doc.Name} {doc.Description}");
                    }
                }
            }
            return sb.ToString();
        }

        public object Instantiate() {
            var obj = Activator.CreateInstance(CommandType);
            foreach (var arg in _fieldDefaults) {
                if (arg.Default == null) {
                    // Default value not specified, use the CLR default instead.
                    arg.Field.SetValue(
                        obj,
                        Activator.CreateInstance(arg.Field.FieldType));
                } else {
                    arg.Field.SetValue(obj, arg.Default);
                }
            }
            return obj;
        }

        public static CommandProfile Of<T>(ISyntaxProvider sp) =>
            new CommandProfile(typeof(T), sp);

        private static string FetchDoc(Type cmd) {
            var attr = cmd.GetCustomAttribute<DocAttribute>();
            return attr == null ? string.Empty : attr.Doc;
        }
        private static string FetchDoc(FieldInfo field) {
            var attr = field.GetCustomAttribute<DocAttribute>();
            return attr == null ? string.Empty : attr.Doc;
        }

        private static bool TryAsFree(
                FieldInfo field,
                List<FreeArgumentInfo> info,
                List<FieldDefault> @default,
                List<Documentation> docs) {
            var attr = field.GetCustomAttribute<FreeArgAttribute>();
            if (attr == null) return false;
            // Array receptor is only allowed to be the last free argument
            // receptor.
            if (info.Count > 0 && info.Last().IsListReceptor) {
                throw new CommandException(
                    "Only one list receptor is allowed.");
            }
            var type = field.FieldType;
            info.Add(new FreeArgumentInfo() {
                Position = info.Count,
                // An list that is the last free argument in amapped structure
                // and it will receive all the remaining free arguments.
                IsListReceptor = type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(List<>),
                Field = field,
            });
            docs.Add(new Documentation {
                Name = string.Format($"(Free#{info.Count})"),
                Description = FetchDoc(field)
            });
            @default.Add(MakeStringSafeFieldDefault(field, attr.Default));
            return true;
        }

        private static bool TryAsNamed(
                FieldInfo field,
                List<ArgumentInfo> info,
                ISyntaxProvider sp,
                List<FieldDefault> @default,
                List<Documentation> docs) {
            var attr = field.GetCustomAttribute<ArgAttribute>();
            if (attr == null) return false;
            if (attr.IsSwitch && field.FieldType != typeof(bool)) {
                throw new CommandException("Switch argument must be boolean.");
            }

            var doc = new Documentation();

            var needAutoName = true;
            if (attr.Abbreviate != '\0') {
                var nm = sp.NameArgument(new[] { attr.Abbreviate.ToString() });
                info.Add(new ArgumentInfo {
                    Name = nm,
                    IsSwitch = attr.IsSwitch,
                    Field = field,
                });
                needAutoName = false;
                doc.Abbreviate = nm;
            }
            if (!string.IsNullOrEmpty(attr.Name)) {
                var nm = sp.NameArgument(SplitSymbolName(attr.Name));
                info.Add(new ArgumentInfo {
                    Name = nm,
                    IsSwitch = attr.IsSwitch,
                    Field = field,
                }); 
                needAutoName = false;
                doc.Name = nm;
            }
            // If no user defined name is provided, the argument will be named
            // automatically after it's declared name.
            if (needAutoName) {
                var nm = sp.NameArgument(SplitSymbolName(field.Name));
                info.Add(new ArgumentInfo {
                    Name = nm,
                    IsSwitch = attr.IsSwitch,
                    Field = field,
                });
                doc.Name = nm;
            }
            docs.Add(doc);
            @default.Add(MakeStringSafeFieldDefault(field, attr.Default));
            return true;
        }

        /// <summary>
        /// Workaround since string has no default constructor.
        /// </summary>
        private static FieldDefault MakeStringSafeFieldDefault(
                FieldInfo field,
                object def) =>
            new FieldDefault {
                Field = field,
                Default = (field.FieldType == typeof(string) && def == null) ?
                    String.Empty : def
            };

        /// <summary>
        /// Split symbol name (command name and argument name) into words.
        /// </summary>
        /// <param name="name">Continuous capitalized camel name.</param>
        /// <returns>Splitted words.
        /// 
        /// ```
        /// "DBAdmin" -> "DB", "Admin"
        /// "DbbAdmin" -> "Dbb", "Admin"
        /// "DBBAdmin" -> "DB", "B", "Admin"
        /// ```
        /// </returns>
        public static string[] SplitSymbolName(string name) {
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(name[0] >= 'A' && name[0] <= 'Z');

            List<string> rv = new List<string>();
            var beg = 0;
            var upperCount = 0; // Number of sequential upper met.
            var lastLower = false;
            var i = 0;
            for (; beg + i < name.Length; ++i) {
                char c = name[beg + i];
                if (c >= 'A' && c <= 'Z') {
                    if (lastLower || upperCount == 2) {
                        rv.Add(name.Substring(beg, i));
                        beg += i;
                        i = 0;
                        upperCount = 0;
                    }
                    ++upperCount;
                    lastLower = false;
                } else {
                    if (upperCount == 2) {
                        rv.Add(name.Substring(beg, i - 1));
                        ++beg;
                        i = 1;
                        upperCount = 1;
                    }
                    upperCount = 0;
                lastLower = true;
                }
            }
            rv.Add(name.Substring(beg, i));
            return rv.ToArray();
        }
    }
}
