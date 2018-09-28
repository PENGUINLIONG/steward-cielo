using System;
using System.Collections.Generic;
using System.Linq;

namespace Liongbot.Command.SyntaxProviders {
    public class SimpleSyntaxProvider : ISyntaxProvider {
        public bool Parse(ArgumentInjector injector, ArgumentPack args) {
            foreach (var arg in args.Skip(1)) {
                var pair = arg.Split('=', 2);
                if (pair.Length == 2) {
                    var entry = injector.GetNamedEntry(pair[0]);
                    // Skip if there is no corresponding entry.
                    if (entry == null) continue;
                    // Paired input => named artugemnts.
                    if (pair[1] == "true") {
                        // Switch syntax, if one of the value is set true.
                        if (!entry.SwitchOn()) {
                            return false;
                        }
                    } else if (pair[1] == "false") {
                        continue;
                    } else {
                        // If it's not a switch, it is a named argument.
                        if (!entry.Inject(pair[1]))
                           return false;
                    }
                } else {
                    // Non-pair input => free argument.
                    if (!injector.AppendFreeArgument(pair[0])) {
                        return false;
                    }
                }
            }
            return true;
        }

        public string NameArgument(IEnumerable<string> words) =>
            string.Join('-', SyntaxProviderUtillities.ToAsciiLower(words));

        public string NameCommand(IEnumerable<string> words) =>
            "/" +
            string.Join('-', SyntaxProviderUtillities.ToAsciiLower(words));

        public bool SyntacticEqual(string a, string b) =>
            SyntaxProviderUtillities.CaseInsensitiveEqual(a, b);
    }
}
