using Environment = System.Environment;
using Path = System.IO.Path;
using File = System.IO.File;

using NUnit.Framework;

namespace OpinionatedCsharpTodos.Tests
{
    public class ProgramTests
    {
        [Test]
        public void TestNoCommandLineArguments()
        {
            using var consoleCapture = new ConsoleCapture();

            int exitCode = Program.MainWithCode(new string[0]);

            string nl = Environment.NewLine;

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual($"Option '--inputs' is required.{nl}{nl}", consoleCapture.Error());
        }

        [Test]
        public void TestInvalidCommandLineArguments()
        {
            using var consoleCapture = new ConsoleCapture();

            int exitCode = Program.MainWithCode(new[] { "--invalid-arg" });

            string nl = Environment.NewLine;

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual(
                $"Option '--inputs' is required.{nl}" +
                $"Unrecognized command or argument '--invalid-arg'{nl}{nl}",
                consoleCapture.Error());
        }

        [Test]
        public void TestWithNonCodeInput()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, "this is not parsable C# code.");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestWithValidTodo()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// TODO (mristin, 2020-07-20): Do something!{nl}");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual($"{path}:0:0:TODO (mristin, 2020-07-20): Do something!{nl}",
                consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestWithValidTodoAndCaseInsensitive()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// todo (mristin, 2020-07-20): Do something!{nl}");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path, "--case-insensitive" });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual($"{path}:0:0:todo (mristin, 2020-07-20): Do something!{nl}",
                consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestWithValidTodoVerbose()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// TODO (mristin, 2020-07-20): Do something!{nl}");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path, "--verbose" });


            Assert.AreEqual("", consoleCapture.Error());

            Assert.AreEqual($"OK, 1 todo(s): {path}{nl}" +
                            $"{path}:0:0:TODO (mristin, 2020-07-20): Do something!{nl}",
                consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestWithInvalidTodo()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path,
                $"// TODO (mristin): Do something!{nl}" +
                $"// DONT-CHECK-IN");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual(
            $"FAILED: {path}{nl}" +
            $" * Line 0, column 0: invalid suffix (see --suffixes):  (mristin): Do something!{nl}" +
            $" * Line 1, column 0: disallowed prefix (see --disallowed-prefixes): DONT-CHECK-IN{nl}" +
            $"One or more TODOs were invalid. Please see above.{nl}",
            consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(1, exitCode);
        }

        [Test]
        public void TestWithInvalidAndValidTodo()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path,
                $"// TODO (mristin): Do something!{nl}" +
                "// TODO (mristin, 2020-07-20): Do something else!");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual(
                $"FAILED: {path}{nl}" +
                $" * Line 0, column 0: invalid suffix (see --suffixes):  (mristin): Do something!{nl}" +
                $"One or more TODOs were invalid. Please see above.{nl}",
                consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(1, exitCode);
        }

        [Test]
        public void TestCaseSensitivity()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// todo (mristin, 2020-07-20): Do something!{nl}");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path, "--verbose" });

            Assert.AreEqual($"FAILED: {path}{nl}" +
                            $" * Line 0, column 0: disallowed prefix (see --disallowed-prefixes): todo{nl}" +
                            $"One or more TODOs were invalid. Please see above.{nl}",
                consoleCapture.Error());
            Assert.AreEqual("",
                consoleCapture.Output());
            Assert.AreEqual(1, exitCode);
        }

        [Test]
        public void TestWithInvalidAndValidTodoAndVerbose()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string pathNotOk = Path.Join(tmpdir.Path, "NotOk.cs");
            File.WriteAllText(pathNotOk, $"// TODO (mristin): Do something!{nl}");

            string pathOk = Path.Join(tmpdir.Path, "Ok.cs");
            File.WriteAllText(pathOk, "// TODO (mristin, 2020-07-20): Do something else!");

            int exitCode = Program.MainWithCode(new[] { "--inputs", pathNotOk, pathOk, "--verbose" });

            Assert.AreEqual(
                $"FAILED: {pathNotOk}{nl}" +
                $" * Line 0, column 0: invalid suffix (see --suffixes):  (mristin): Do something!{nl}" +
                $"One or more TODOs were invalid. Please see above.{nl}",
                consoleCapture.Error());
            Assert.AreEqual($"OK, 1 todo(s): {pathOk}{nl}", consoleCapture.Output());
            Assert.AreEqual(1, exitCode);
        }

        [Test]
        public void TestExclude()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string pathNotOkButExcluded = Path.Join(tmpdir.Path, "NotOk.cs");
            File.WriteAllText(pathNotOkButExcluded, $"// TODO (mristin): Do something, but excluded!{nl}");

            string pathOk = Path.Join(tmpdir.Path, "Ok.cs");
            File.WriteAllText(pathOk, "// TODO (mristin, 2020-07-20): Do something else!");

            int exitCode = Program.MainWithCode(new[] { "--inputs", pathOk, "--excludes", pathNotOkButExcluded });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual($"{pathOk}:0:0:TODO (mristin, 2020-07-20): Do something else!{nl}",
                consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestReportToStdout()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// TODO (mristin, 2020-07-20): Do something!{nl}");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path, "--report-path", "-" });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual(
                $@"[
  {{
    ""path"": ""{path.Replace(@"\", @"\\")}"",
    ""records"": [
      {{
        ""prefix"": ""TODO"",
        ""suffix"": "" (mristin, 2020-07-20): Do something!"",
        ""line"": 0,
        ""column"": 0,
        ""status"": ""ok""
      }}
    ]
  }}
]
",
                consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestReportToFile()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, $"// TODO (mristin, 2020-07-20): Do something!{nl}");

            string reportPath = Path.Join(tmpdir.Path, "report.json");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path, "--report-path", reportPath });

            string expectedReport = $@"[
  {{
    ""path"": ""{path.Replace(@"\", @"\\")}"",
    ""records"": [
      {{
        ""prefix"": ""TODO"",
        ""suffix"": "" (mristin, 2020-07-20): Do something!"",
        ""line"": 0,
        ""column"": 0,
        ""status"": ""ok""
      }}
    ]
  }}
]
";
            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.IsTrue(File.Exists(reportPath));
            Assert.AreEqual(expectedReport, File.ReadAllText(reportPath));
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void TestPatternArguments()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path,
                $"// AAA BBB{nl}" +
                $"// CCC");

            int exitCode = Program.MainWithCode(
                new[]
                {
                    "--inputs", path,
                    "--prefix", "^AAA",
                    "--disallowed-prefix", "CCC",
                    "--suffix", "^ BBB"
                });

            Assert.AreEqual(
                "",
                consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(1, exitCode);

        }
    }
}