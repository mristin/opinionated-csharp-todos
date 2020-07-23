using ArgumentException = System.ArgumentException;
using Regex = System.Text.RegularExpressions.Regex;

using SyntaxTree = Microsoft.CodeAnalysis.SyntaxTree;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;

using System.Collections.Generic;
using System.Linq;


namespace OpinionatedCsharpTodos
{
    public static class Inspection
    {
        public enum Status
        {
            Ok,
            DisallowedPrefix,
            NonMatchingSuffix
        }

        public class Record
        {
            public string Prefix { get; }
            public string Suffix { get; }
            public int Line { get; } // indexed at 0
            public int Column { get; } // indexed at 0
            public Status Status { get; }

            public Record(string prefix, string suffix, int line, int column, Status status)
            {
                if (line < 0) throw new ArgumentException($"Negative line: {line}");
                if (column < 0) throw new ArgumentException($"Negative column: {column}");

                Prefix = prefix;
                Suffix = suffix;
                Line = line;
                Column = column;
                Status = status;
            }
        }

        public class Rules
        {
            public readonly List<Regex> Prefixes;
            public readonly List<Regex> DisallowedPrefixes;
            public readonly List<Regex> Suffixes;

            /// <param name="prefixes">List of comment prefixes to be considered</param>
            /// <param name="disallowedPrefixes">List of prefixes which should not match</param>
            /// <param name="suffixes">
            /// Expected patterns that suffix should match whenever the prefix matches</param>
            public Rules(List<Regex> prefixes, List<Regex> disallowedPrefixes, List<Regex> suffixes)
            {
                Prefixes = prefixes;
                DisallowedPrefixes = disallowedPrefixes;
                Suffixes = suffixes;
            }
        }

        public static class Text
        {
            public class Result
            {
                public readonly string Prefix;
                public readonly string Suffix;
                public readonly Status Status;

                public Result(string prefix, string suffix, Status status)
                {
                    Prefix = prefix;
                    Suffix = suffix;
                    Status = status;
                }
            }


            /// <summary>
            /// Inspects the trivia given as its text content.
            /// </summary>
            /// <param name="text">String representation of the trivia</param>
            /// <param name="rules">Rules of the inspection</param>
            /// <returns>Inspection result or null if the prefix did not match</returns>
            public static Result? Inspect(string text, Rules rules)
            {
                text = text.Trim();

                if (text.StartsWith("//"))
                {
                    text = text.Substring(2);
                }
                else if (text.StartsWith("/*"))
                {
                    if (!text.EndsWith("*/"))
                    {
                        throw new ArgumentException($"Unexpected comment: {text}");
                    }

                    text = text.Substring(2, text.Length - 4);
                }
                else
                {
                    // Trivia is not a comment.
                    return null;
                }

                // Trim again due to the white-space around the comment symbols
                text = text.Trim();

                foreach (var prefixRe in rules.Prefixes)
                {
                    var prefixMatch = prefixRe.Match(text);
                    if (prefixMatch.Success)
                    {
                        string prefix = prefixMatch.Value;
                        string suffix = text.Substring(prefix.Length);

                        foreach (var suffixRe in rules.Suffixes)
                        {
                            var suffixMatch = suffixRe.Match(suffix);
                            if (suffixMatch.Success)
                            {
                                return new Result(prefix, suffix, Status.Ok);
                            }
                        }

                        return new Result(prefix, suffix, Status.NonMatchingSuffix);
                    }
                }

                foreach (var disallowedPrefix in rules.DisallowedPrefixes)
                {
                    var match = disallowedPrefix.Match(text);
                    if (match.Success)
                    {
                        string prefix = match.Value;
                        string suffix = text.Substring(prefix.Length);
                        return new Result(prefix, suffix, Status.DisallowedPrefix);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Inspects the syntax tree and reports the comments starting with the expected prefixes.
        /// </summary>
        /// <param name="tree">Parsed syntax tree</param>
        /// <param name="rules">Rules of the inspection</param>
        /// <returns>List of comments starting with the given prefixes</returns>
        public static IEnumerable<Record> Inspect(SyntaxTree tree, Rules rules)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var irrelevantRecord = new Record("", "", 0, 0, Status.Ok);

            IEnumerable<Record> records =
                root.DescendantTrivia()
                    // Beware: ToString() is an expensive operation on Syntax Nodes and
                    // involves some complex logic and a string builder!
                    // Hence we convert the trivia to string only at this single place.
                    .Select((trivia) => (trivia, trivia.ToString()))
                    .Select(
                        ((SyntaxTrivia, string) t) =>
                        {
                            var (trivia, triviaAsString) = t;

                            var result = Text.Inspect(triviaAsString, rules);
                            if (result == null)
                            {
                                return irrelevantRecord;
                            }

                            var span = tree.GetLineSpan(trivia.Span);
                            var position = span.StartLinePosition;
                            var line = position.Line;
                            var column = position.Character;

                            return new Record(result.Prefix, result.Suffix, line, column, result.Status);
                        })
                    .Where((record) => record != irrelevantRecord);

            return records;
        }
    }
}