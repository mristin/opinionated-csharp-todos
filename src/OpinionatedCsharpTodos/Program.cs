using Exception = System.Exception;
using InvalidOperationException = System.InvalidOperationException;
using Json = System.Text.Json;
using Console = System.Console;
using Environment = System.Environment;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;
using File = System.IO.File;
using Directory = System.IO.Directory;
using StreamWriter = System.IO.StreamWriter;
using TextWriter = System.IO.TextWriter;

// We can not cherry-pick imports from System.CommandLine since InvokeAsync is a necessary extension.
using System.CommandLine;


namespace OpinionatedCsharpTodos
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        private static readonly string[] DefaultPrefixes =
        {
            "^TODO", "^BUG", "^HACK"
        };

        private static readonly string[] DefaultDisallowedPrefixes =
        {
            "^DONT-CHECK-IN", "^Todo", "^todo", "^ToDo",
            "^Bug", "^bug", "^Hack", "^hack"
        };

        private static readonly string[] DefaultSuffixes =
        {
            @"^ \([^)]+, [0-9]{4}-[0-9]{2}-[0-9]{2}\): ."
        };

        private static Inspection.Rules? ParseRules(
            IEnumerable<string> prefixPatterns,
            IEnumerable<string> disallowedPrefixPatterns,
            IEnumerable<string> suffixPatterns,
            bool caseInsensitive)
        {
            var prefixRegexes = new List<Regex>();
            foreach (string pattern in prefixPatterns)
            {
                try
                {
                    prefixRegexes.Add(
                        caseInsensitive ? new Regex(pattern, RegexOptions.IgnoreCase) : new Regex(pattern));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Failed to parse a prefix pattern {pattern}: {error}");
                    return null;
                }
            }

            var disallowedPrefixRegexes = new List<Regex>();
            foreach (string pattern in disallowedPrefixPatterns)
            {
                try
                {
                    disallowedPrefixRegexes.Add(
                        caseInsensitive ? new Regex(pattern, RegexOptions.IgnoreCase) : new Regex(pattern));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Failed to parse a disallowed prefix pattern {pattern}: {error}");
                    return null;
                }
            }

            var suffixRegexes = new List<Regex>();
            foreach (string pattern in suffixPatterns)
            {
                try
                {
                    suffixRegexes.Add(
                        caseInsensitive ? new Regex(pattern, RegexOptions.IgnoreCase) : new Regex(pattern));
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Failed to parse a suffix pattern {pattern}: {error}");
                    return null;
                }
            }

            return new Inspection.Rules(prefixRegexes, disallowedPrefixRegexes, suffixRegexes);
        }

        private class FileRecords
        {
            public string Path { get; }
            public List<Inspection.Record> Records { get; }

            public FileRecords(string path, List<Inspection.Record> records)
            {
                Path = path;
                Records = records;
            }
        }

        private static (List<FileRecords>, bool) ScanPaths(
            IEnumerable<string> paths,
            Inspection.Rules rules,
            bool verbose)
        {
            var result = new List<FileRecords>();
            bool success = true;

            foreach (string path in paths)
            {
                string programText = File.ReadAllText(path);
                var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(programText);

                IEnumerable<Inspection.Record> records = Inspection.Inspect(tree, rules);

                var okRecords = new List<Inspection.Record>();
                var notOkRecords = new List<Inspection.Record>();
                foreach (var record in records)
                {
                    if (record.Status == Inspection.Status.Ok)
                    {
                        okRecords.Add(record);
                    }
                    else
                    {
                        notOkRecords.Add(record);
                    }
                }

                if (notOkRecords.Count == 0)
                {
                    if (verbose)
                        Console.WriteLine($"OK, {okRecords.Count} todo(s): {path}");
                }
                else
                {
                    success = false;
                    Console.Error.WriteLine($"FAILED: {path}");
                    foreach (var record in notOkRecords)
                    {
                        string message;
                        switch (record.Status)
                        {
                            case Inspection.Status.DisallowedPrefix:
                                message = $"disallowed prefix (see --disallowed-prefixes): {record.Prefix}";
                                break;
                            case Inspection.Status.NonMatchingSuffix:
                                message = $"invalid suffix (see --suffixes): {record.Suffix}";
                                break;
                            default:
                                throw new InvalidOperationException($"Unhandled case: {record.Status}");
                        }

                        Console.Error.WriteLine($" * Line {record.Line}, column {record.Column}: {message}");
                    }
                }

                if (okRecords.Count > 0)
                {
                    result.Add(new FileRecords(path, okRecords));
                }
            }

            return (result, success);
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class Arguments
        {
#pragma warning disable 8618
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            public string[] Inputs { get; set; }
            public string[]? Excludes { get; set; }
            public string[]? Prefixes { get; set; }
            public string[]? DisallowedPrefixes { get; set; }
            public string[]? Suffixes { get; set; }
            public bool CaseInsensitive { get; set; }
            public string? ReportPath { get; set; }
            public bool Verbose { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore 8618
        }

        private static int Scan(Arguments a)
        {
            Inspection.Rules? rules = ParseRules(
                a.Prefixes ?? DefaultPrefixes,
                a.DisallowedPrefixes ?? DefaultDisallowedPrefixes,
                a.Suffixes ?? DefaultSuffixes,
                a.CaseInsensitive);

            if (rules == null)
            {
                return 1;
            }

            string cwd = Directory.GetCurrentDirectory();
            IEnumerable<string> paths = Input.MatchFiles(
                cwd,
                new List<string>(a.Inputs),
                new List<string>(a.Excludes ?? new string[0]));

            (List<FileRecords> todoBank, bool success) = ScanPaths(paths, rules, a.Verbose);

            if (!success)
            {
                Console.Error.WriteLine("One or more TODOs were invalid. Please see above.");
                return 1;
            }

            if (a.ReportPath == null)
            {
                foreach (FileRecords fileRecords in todoBank)
                {
                    foreach (var record in fileRecords.Records)
                    {
                        Console.WriteLine(
                            $"{fileRecords.Path}:{record.Line}:{record.Column}:{record.Prefix + record.Suffix}");
                    }
                }
            }
            else
            {
                StreamWriter? streamWriter = null;

                TextWriter writer = Console.Out;
                if (a.ReportPath != "-")
                {
                    streamWriter = new StreamWriter(a.ReportPath);
                    writer = streamWriter;
                }

                try
                {
                    var options = new Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = Json.JsonNamingPolicy.CamelCase,
                    };
                    options.Converters.Add(
                        new Json.Serialization.JsonStringEnumConverter(Json.JsonNamingPolicy.CamelCase));

                    writer.WriteLine(Json.JsonSerializer.Serialize(todoBank, options));
                }
                finally
                {
                    streamWriter?.Close();
                }
            }

            return 0;
        }


        public static int MainWithCode(string[] args)
        {
            var rootCommand = new RootCommand(
                "Examines and collects the TODOs from your C# code.")
            {
                new Option<string[]>(
                        new[] {"--inputs", "-i"},
                        "Glob patterns of the files to be inspected")
                    { Required=true },

                new Option<string[]>(
                    new[] {"--excludes", "-e"},
                    "Glob patterns of the files to be excluded from inspection"),

                new Option<string[]?>(
                    new[]{"--prefixes"},
                    "Prefix regular expressions marking the TODOs. " +
                    $"[Default: {string.Join(" ", DefaultPrefixes)}]"
                ),

                new Option<string[]?>(
                    new[]{"--disallowed-prefixes"},
                    "Prefix regular expressions which should not occur. " +
                    $"[Default: {string.Join(" ", DefaultDisallowedPrefixes)}]"
                ),

                new Option<string[]?>(
                    new[]{"--suffixes"},
                    "Suffix regular expressions that TODOs must conform to. " +
                    $"[Default: {string.Join(" ", DefaultDisallowedPrefixes)}]"
                ),

                new Option<bool>(
                    new[]{"--case-insensitive"},
                    "If set, the regular expressions are applied as case-insensitive"),

                new Option<string>(
                    new[]{"--report-path"},
                    "If set, outputs the TODOs as a JSON (the path '-' denotes STDOUT)."),

                new Option<bool>(
                    new[]{"--verbose"},
                    "If set, makes the console output more verbose"
                )
            };

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create((Arguments a) => Scan(a));

            int exitCode = rootCommand.InvokeAsync(args).Result;
            return exitCode;
        }

        public static void Main(string[] args)
        {
            int exitCode = MainWithCode(args);
            Environment.ExitCode = exitCode;
        }
    }
}