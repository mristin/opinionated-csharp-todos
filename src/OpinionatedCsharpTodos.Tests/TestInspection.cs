using Regex = System.Text.RegularExpressions.Regex;

using CSharpSyntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree;

using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace OpinionatedCsharpTodos.Tests
{
    public static class DefinedForTest
    {
        public static readonly Inspection.Rules Rules = new Inspection.Rules(
            new List<Regex> { new Regex(@"^TODO"), new Regex(@"^BUG") },
            new List<Regex> { new Regex(@"^DONT-CHECK-IN") },
            new List<Regex> { new Regex(@"^ \([^)]+, [0-9]{4}-[0-9]{2}-[0-9]{2}\): .") });
    }

    public class TextInspectTests
    {
        [TestCase("")]
        [TestCase("\t")]
        [TestCase("// Do something")]
        [TestCase("/* Do something */")]
        [TestCase("// SOME-VERY-WEIRD-TAG: do something")]
        [TestCase("// UNHANDLED: do something")]
        public void TestNoMatchedPrefix(string text)
        {
            var result = Inspection.Text.Inspect(text, DefinedForTest.Rules);

            Assert.IsNull(result);
        }

        [TestCase(
            "// TODO (mristin, 2020-07-20): Do something",
            "TODO", " (mristin, 2020-07-20): Do something")]
        [TestCase(
            "/* TODO (mristin, 2020-07-20): Do something */",
            "TODO", " (mristin, 2020-07-20): Do something")]
        [TestCase(
            @"/* 
TODO (mristin, 2020-07-20): Do something

This is a body.
*/",
            "TODO", @" (mristin, 2020-07-20): Do something

This is a body.")]
        [TestCase(
            "// BUG (mristin, 2020-07-20): Something doesn't work.",
            "BUG", " (mristin, 2020-07-20): Something doesn't work.")]
        public void TestOk(string text, string expectedPrefix, string expectedSuffix)
        {
            var result = Inspection.Text.Inspect(text, DefinedForTest.Rules);

            Assert.IsNotNull(result);
            Assert.AreEqual(Inspection.Status.Ok, result.Status);
            Assert.AreEqual(expectedPrefix, result.Prefix);
            Assert.AreEqual(expectedSuffix, result.Suffix);
        }

        [TestCase("// DONT-CHECK-IN", "DONT-CHECK-IN", "")]
        [TestCase("// DONT-CHECK-IN: do something", "DONT-CHECK-IN", ": do something")]
        [TestCase("// DONT-CHECK-IN (mristin, 2020-07-20): do something",
            "DONT-CHECK-IN", " (mristin, 2020-07-20): do something")]
        public void TestDisallowedPrefix(string text, string expectedPrefix, string expectedSuffix)
        {
            var result = Inspection.Text.Inspect(text, DefinedForTest.Rules);

            Assert.IsNotNull(result);
            Assert.AreEqual(Inspection.Status.DisallowedPrefix, result.Status);
            Assert.AreEqual(expectedPrefix, result.Prefix);
            Assert.AreEqual(expectedSuffix, result.Suffix);
        }

        [TestCase("// TODO", "TODO", "")]
        [TestCase("// TODO: Do something", "TODO", ": Do something")]
        public void TestNonMatchingSuffix(string text, string expectedPrefix, string expectedSuffix)
        {
            var result = Inspection.Text.Inspect(text, DefinedForTest.Rules);

            Assert.IsNotNull(result);
            Assert.AreEqual(Inspection.Status.NonMatchingSuffix, result.Status);
            Assert.AreEqual(expectedPrefix, result.Prefix);
            Assert.AreEqual(expectedSuffix, result.Suffix);
        }
    }

    public class InspectTests
    {
        [Test]
        public void TestEmpty()
        {
            string programText = "";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree, DefinedForTest.Rules).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void TestNotCode()
        {
            string programText = "This is no C# code, but it has a 'TODO (mristin, 2020-07-20): Do something'.";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree, DefinedForTest.Rules).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void TestValidCode()
        {
            string programText = @"
namespace SomeNamespace
{
    // TODO
    class SomeClass
    {
        // TODO: invalid

        // TODO (mristin, 2007-07-20): Do something

        // This is just a comment.
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree, DefinedForTest.Rules).ToList();

            var expectedRecords = new List<Inspection.Record>
            {
                new Inspection.Record("TODO", "", 3, 4, Inspection.Status.NonMatchingSuffix),
                new Inspection.Record("TODO", ": invalid", 6, 8,
                    Inspection.Status.NonMatchingSuffix),
                new Inspection.Record("TODO", " (mristin, 2007-07-20): Do something", 8, 8,
                    Inspection.Status.Ok)
            };

            Assert.AreEqual(expectedRecords.Count, records.Count);

            for (var i = 0; i < expectedRecords.Count; i++)
            {
                string label = $"Record {i}";
                Assert.AreEqual(expectedRecords[i].Prefix, records[i].Prefix, label);
                Assert.AreEqual(expectedRecords[i].Suffix, records[i].Suffix, label);
                Assert.AreEqual(expectedRecords[i].Line, records[i].Line, label);
                Assert.AreEqual(expectedRecords[i].Column, records[i].Column, label);
                Assert.AreEqual(expectedRecords[i].Status, records[i].Status, label);
            }
        }
    }
}