using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Liongbot.Command.SyntaxProviders
{
    public class SyntaxProviderUtillities {
        private static bool IsUpper(char c) =>
            c >= 'a' && c <= 'z';
        private static bool IsLower(char c) =>
            c >= 'A' && c <= 'Z';

        // Note `char.ToLower` and etc. consider the upper-to-lower conversion
        // in ANY LANUGAGE supported by Unicode.
        private static char CharToAsciiLower(char c) =>
            (char)(IsLower(c) ? c - 'A' + 'a' : c);
        private static char CharToAsciiUpper(char c) =>
            (char)(IsUpper(c) ? c - 'a' + 'A' : c);

        private static string ToAsciiLowerImpl(string word) {
            if (word.Length < 2) return word;
            var rv = new char[word.Length];
            for (int i = 0; i < rv.Length; ++i) {
                rv[i] = CharToAsciiLower(word[i]);
            }
            return new string(rv);
        }
        private static string ToAsciiUpperImpl(string word) {
            if (word.Length < 2) return word;
            var rv = new char[word.Length];
            for (int i = 0; i < rv.Length; ++i) {
                rv[i] = CharToAsciiUpper(word[i]);
            }
            return new string(rv);
        }
        private static string CapitalizeImpl(string word) {
            // `DB` is also accepted as it's okay to have two-char abbreviate
            // upper for both char.
            if (word.Length < 2) {
                return word;
            } else if (word.Length == 2) {
                return new string(new[] { CharToAsciiUpper(word[0]), word[1] });
            } else {
                var rv = new char[word.Length];
                rv[0] = CharToAsciiUpper(word[0]);
                for (int i = 1; i < rv.Length; ++i) {
                    rv[i] = CharToAsciiLower(word[i]);
                }
                return new string(rv);
            }
        }

        public static IEnumerable<string> ToAsciiLower(
                IEnumerable<string> words) =>
            from word in words select ToAsciiLowerImpl(word);
        public static IEnumerable<string> ToAsciiUpper(
                IEnumerable<string> words) =>
            from word in words select ToAsciiUpperImpl(word);
        public static IEnumerable<string> Capitalize(
                IEnumerable<string> words) =>
            from word in words select CapitalizeImpl(word);

        public static bool CaseInsensitiveEqual(string a, string b) =>
            a.Zip(b, (x, y) => x == y).All((x) => x);
    }
}
