using System.Collections.Generic;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// RFC 4180 파서와 라이터의 왕복 정확성을 검증합니다.
    /// </summary>
    public sealed class DelimitedTextRoundTripTests
    {
        [Test]
        public void Parse_SplitsPlainFields()
        {
            List<List<string>> rows = DelimitedTextParser.Parse("a,b,c", ',', null);

            Assert.AreEqual(1, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows[0]);
        }

        [Test]
        public void Parse_KeepsDelimiterInsideQuotes()
        {
            List<List<string>> rows = DelimitedTextParser.Parse("\"a,b\",c", ',', null);

            CollectionAssert.AreEqual(new[] { "a,b", "c" }, rows[0]);
        }

        [Test]
        public void Parse_KeepsNewLineInsideQuotes()
        {
            List<List<string>> rows = DelimitedTextParser.Parse("\"a\nb\",c", ',', null);

            Assert.AreEqual(1, rows.Count);
            CollectionAssert.AreEqual(new[] { "a\nb", "c" }, rows[0]);
        }

        [Test]
        public void Parse_UnescapesDoubledQuotes()
        {
            List<List<string>> rows = DelimitedTextParser.Parse("\"say \"\"hi\"\"\",b", ',', null);

            CollectionAssert.AreEqual(new[] { "say \"hi\"", "b" }, rows[0]);
        }

        [Test]
        public void Parse_RecordsCrLfNewLine()
        {
            TableFileOptions options = new();
            DelimitedTextParser.Parse("a\r\nb", ',', options);

            Assert.AreEqual("\r\n", options.NewLine);
            Assert.IsFalse(options.EndsWithNewLine);
        }

        [Test]
        public void Parse_RecordsTrailingNewLine()
        {
            TableFileOptions options = new();
            List<List<string>> rows = DelimitedTextParser.Parse("a\nb\n", ',', options);

            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(options.EndsWithNewLine);
        }

        [Test]
        public void Parse_KeepsTrailingEmptyField()
        {
            TableFileOptions options = new();
            List<List<string>> rows = DelimitedTextParser.Parse("박지훈\t32\t", '\t', options);

            Assert.AreEqual(1, rows.Count);
            CollectionAssert.AreEqual(new[] { "박지훈", "32", "" }, rows[0]);
            Assert.IsFalse(options.EndsWithNewLine);
        }

        [Test]
        public void Document_PadsJaggedRows()
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse("a,b,c\nd", ',', null));

            Assert.AreEqual(3, document.ColumnCount);
            Assert.AreEqual(string.Empty, document.GetCell(1, 2));
        }

        [Test]
        public void Write_DoesNotQuoteWhenUnnecessary()
        {
            List<List<string>> rows = new() { new List<string> { " a ", "b" } };
            string text = DelimitedTextWriter.Write(rows, ',', "\n", false);

            Assert.AreEqual(" a ,b", text);
        }

        [Test]
        public void Write_QuotesOnlyFieldsThatNeedIt()
        {
            List<List<string>> rows = new() { new List<string> { "a,b", "c\"d", "plain" } };
            string text = DelimitedTextWriter.Write(rows, ',', "\n", false);

            Assert.AreEqual("\"a,b\",\"c\"\"d\",plain", text);
        }

        [Test]
        public void RoundTrip_PreservesExactText()
        {
            const string original = "이름,설명\n박지훈,\"쉼표, 따옴표\"\" 그리고\n줄바꿈\"\n";

            TableFileOptions options = new();
            List<List<string>> rows = DelimitedTextParser.Parse(original, ',', options);

            string text = DelimitedTextWriter.Write(rows, ',', options.NewLine, options.EndsWithNewLine);

            Assert.AreEqual(original, text);
        }

        [Test]
        public void RoundTrip_PreservesTsvWithoutFinalNewLine()
        {
            const string original = "박지훈\t32\t";

            TableFileOptions options = new();
            TableDocument document = new();
            document.Format = TableFormat.Tsv;
            document.FileOptions = options;
            document.SetContent(DelimitedTextParser.Parse(original, '\t', options));

            string text = DelimitedTextWriter.Write(
                document.GetRows(),
                '\t',
                options.NewLine,
                options.EndsWithNewLine);

            Assert.AreEqual(original, text);
        }
    }
}
