using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Liongbot.Command {
    /// <summary>
    /// Manager of argument injection where suitable.
    /// </summary>
    public class ArgumentInjector {
        public CommandProfile Profile { get; }
        public object ArgumentObject { get; }
        private int _freeArgPos = 0;

        /// <summary>
        /// Entry to a named argument.
        /// </summary>
        public class NamedEntry {
            internal ArgumentInfo _ref;
            internal object _inst;

            /// <summary>
            /// True is the current entry is a switch.
            /// </summary>
            public bool IsSwitch { get => _ref.IsSwitch; }
            /// <summary>
            /// Inject value.
            /// </summary>
            /// <param name="value">Value to be injected.</param>
            /// <returns>True if the value is injected successfully; false
            /// otherwise.</returns>
            public bool Inject(string value) =>
                IsSwitch ? false : SafeSet(
                    field: _ref.Field,
                    target: _inst,
                    value: value,
                    convertedType: _ref.Field.FieldType);
            /// <summary>
            /// Set a switch on.
            /// </summary>
            /// <returns>If the switch is successfully set on. If not, it might
            /// be a named argument, and you should try `Inject` instead.
            /// </returns>
            public bool SwitchOn() =>
                !IsSwitch ? false : SafeSet(
                    field: _ref.Field,
                    target: _inst,
                    value: true,
                    convertedType: typeof(bool));
        }

        /// <summary>
        /// Unwrap an nullable type. For example, `int?` will turn `int` while
        /// `int` is still `int`.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static Type UnwrapNullable(Type type) =>
            type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) ?
                Nullable.GetUnderlyingType(type) : type;

        /// <summary>
        /// Try to set the field of target object. Give up if an exception
        /// occurred.
        /// </summary>
        /// <param name="field">Field to be assigned with value.</param>
        /// <param name="target">Target command structure.</param>
        /// <param name="value">Value that will be assigned into the
        /// corresponding field.</param>
        /// <param name="convertedType">Type the value should be converted into,
        /// because `value` can be anything at the moment.</param>
        /// <returns>True on success; false otherwise.</returns>
        private static bool SafeSet(
                FieldInfo field,
                object target,
                object value,
                Type convertedType) {
            try {
                field.SetValue(
                    target,
                    Convert.ChangeType(value, UnwrapNullable(convertedType)));
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public ArgumentInjector(CommandProfile profile) {
            Profile = profile;
            ArgumentObject = profile.Instantiate();
        }

        /// <summary>
        /// Try to access to an entry to a named argument.
        /// </summary>
        /// <param name="name">The name of the named argument.</param>
        /// <returns>The entry on success, `null` otherwise.</returns>
        public NamedEntry GetNamedEntry(string name) =>
            Profile.NamedArgs.TryGetValue(name, out ArgumentInfo info) ?
                new NamedEntry { _ref = info, _inst = ArgumentObject } : null;

        /// <summary>
        /// Append a free argument to the free argument queue, the queue is in
        /// order of declaration of the fields attributed with `FreeArg`.
        /// </summary>
        /// <param name="value">Value of the next free argument.</param>
        /// <returns>true if the free argument is successfully appended; false
        /// otherwise. **The syntax provider MUST give up injection immediately
        /// on failure.**</returns>
        public bool AppendFreeArgument(string value) {
            if (_freeArgPos < Profile.FreeArgs.Count) {
                var info = Profile.FreeArgs[_freeArgPos];
                // Check if we have met the list receptor.
                if (info.IsListReceptor) {
                    // See if it's the first time we meet a free argument that
                    // need to be pushed into the receptor.
                    var list = info.Field.GetValue(ArgumentObject);
                    var listType = info.Field.FieldType;
                    var elmType = listType.GetGenericArguments()[0];
                    SafeAddToList(listType, list, value, elmType);
                } else {
                    // No need to tackle with the receptor. Easy.
                    SafeSet(
                        field: info.Field,
                        target: ArgumentObject,
                        value: value,
                        convertedType: info.Field.FieldType);
                    // Increase only when the free argument list is not yet
                    // saturated.
                    ++_freeArgPos;
                }
                return true;
            }
            // Remaining free args has no where to go but the list receptor, but
            // there is no list receptor! Considered failure.
            return false;
        }

        /// <summary>
        /// Try to add an item to the free argument list receptor. Give up on
        /// failure.
        /// </summary>
        /// <param name="listType">Type of the list that will be added with a
        /// new item.</param>
        /// <param name="list">List that will be added with a new item.</param>
        /// <param name="value">Value (item) to be added.</param>
        /// <param name="convertedType">The type that `value` should be
        /// converted into. The value is still a string at the moment.</param>
        /// <returns>True if the item is added successfully; false otherwise.
        /// </returns>
        private bool SafeAddToList(
                Type listType,
                object list,
                string value,
                Type convertedType) {
            var method = listType.GetMethod("Add");
            try {
                method.Invoke(list, new object[] {
                     Convert.ChangeType(value, UnwrapNullable(convertedType))
                });
            } catch (Exception) {
                return false;
            }
            return true;
        }
    }
}
