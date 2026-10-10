using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 파일 형식(구분 기호·따옴표·감싸는 규칙·레거시 인코딩)을 읽고 쓰는 규칙과 .meta 기록을 검증합니다.
    /// </summary>
    public sealed class TableFileFormatTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), $"tabular-editor-format-{Guid.NewGuid():N}.csv");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        private static List<List<string>> Parse(string text, char delimiter, char quote)
        {
            return DelimitedTextParser.Parse(text, delimiter, new TableFileOptions { Delimiter = delimiter, Quote = quote });
        }

        private static string Write(string[][] cells, TableFileOptions options)
        {
            TableDocument document = new();
            List<List<string>> rows = new();

            foreach (string[] row in cells)
                rows.Add(new List<string>(row));

            document.SetContent(rows);
            return DelimitedTextWriter.Write(document, options);
        }

        [Test]
        public void Test_Parse_SingleQuoteKeepsDelimiterInside()
        {
            List<List<string>> rows = Parse("'a;b';c", ';', '\'');

            CollectionAssert.AreEqual(new[] { "a;b", "c" }, rows[0]);
        }

        [Test]
        public void Test_Parse_NoQuoteTreatsQuotesLiterally()
        {
            List<List<string>> rows = Parse("\"a,b\",c", ',', TableFileOptions.NoQuote);

            CollectionAssert.AreEqual(new[] { "\"a", "b\"", "c" }, rows[0]);
        }

        [Test]
        public void Test_Write_MinimalQuotesOnlyWhenNeeded()
        {
            string text = Write(new[] { new[] { "a", "b;c" } }, new TableFileOptions { Delimiter = ';' });

            Assert.AreEqual("a;\"b;c\"", text);
        }

        [Test]
        public void Test_Write_AlwaysQuotesEveryCell()
        {
            string text = Write(new[] { new[] { "a", "" } }, new TableFileOptions { QuoteMode = TableQuoteMode.Always });

            Assert.AreEqual("\"a\",\"\"", text);
        }

        [Test]
        public void Test_Write_NeverQuotes()
        {
            string text = Write(new[] { new[] { "a,b", "c" } }, new TableFileOptions { QuoteMode = TableQuoteMode.Never });

            Assert.AreEqual("a,b,c", text);
        }

        [Test]
        public void Test_Write_CustomQuoteEscapesItself()
        {
            string text = Write(new[] { new[] { "it's" } }, new TableFileOptions { Quote = '\'' });

            Assert.AreEqual("'it''s'", text);
        }

        [Test]
        public void Test_Load_LegacyEncodingFromOverride()
        {
            Encoding cp949 = TableEncodings.TryCreate(949);
            Assume.That(cp949, Is.Not.Null, "이 런타임은 CP949를 지원하지 않습니다.");

            File.WriteAllBytes(_tempPath, cp949.GetBytes("이름;값\n고양이;1\n"));

            TableFileFormatOverride format = new() { EncodingCodePage = 949, Delimiter = ';' };
            TableDocument document = TableDocumentIO.Load(_tempPath, format, out _);

            Assert.AreEqual("고양이", document.GetCell(1, 0));
            Assert.AreEqual(949, document.FileOptions.Encoding.CodePage);
            Assert.AreEqual(';', document.FileOptions.Delimiter);
        }

        [Test]
        public void Test_Serialize_RoundTripsLegacyEncoding()
        {
            Encoding cp949 = TableEncodings.TryCreate(949);
            Assume.That(cp949, Is.Not.Null, "이 런타임은 CP949를 지원하지 않습니다.");

            TableDocument document = new();
            document.SetContent(new List<List<string>> { new() { "한글", "값" } });
            document.FileOptions = new TableFileOptions { Encoding = cp949, Delimiter = ';', EndsWithNewLine = true };

            File.WriteAllBytes(_tempPath, TableDocumentIO.Serialize(document));

            TableDocument loaded = TableDocumentIO.Load(_tempPath, new TableFileFormatOverride { EncodingCodePage = 949, Delimiter = ';' }, out _);

            Assert.AreEqual("한글", loaded.GetCell(0, 0));
        }

        [Test]
        public void Test_Override_KeepsOnlyWhatFileCannotTell()
        {
            TableFileOptions defaults = new() { Delimiter = ',' };
            Assert.IsTrue(TableFileFormatOverride.From(defaults, TableFormat.Csv).IsEmpty);

            TableFileOptions utf8Bom = new() { Encoding = new UTF8Encoding(true) };
            Assert.IsTrue(TableFileFormatOverride.From(utf8Bom, TableFormat.Csv).IsEmpty);

            TableFileOptions tsvOnCsv = new() { Delimiter = '\t' };
            Assert.AreEqual('\t', TableFileFormatOverride.From(tsvOnCsv, TableFormat.Csv).Delimiter);
            Assert.IsNull(TableFileFormatOverride.From(tsvOnCsv, TableFormat.Tsv).Delimiter);
        }

        [Test]
        public void Test_UserData_FormatRoundTripsAndKeepsHeaderRows()
        {
            string withHeader = TableAssetSettings.FormatHeaderRowCount(2);

            TableFileFormatOverride format = new()
            {
                EncodingCodePage = 949,
                Delimiter = ';',
                Quote = TableFileOptions.NoQuote,
                QuoteMode = TableQuoteMode.Never,
            };

            string userData = TableAssetSettings.FormatFileFormat(withHeader, format);
            TableFileFormatOverride parsed = TableAssetSettings.ParseFileFormat(userData);

            Assert.AreEqual(2, TableAssetSettings.ParseHeaderRowCount(userData));
            Assert.AreEqual(949, parsed.EncodingCodePage);
            Assert.AreEqual(';', parsed.Delimiter);
            Assert.AreEqual(TableFileOptions.NoQuote, parsed.Quote);
            Assert.AreEqual(TableQuoteMode.Never, parsed.QuoteMode);
        }

        [Test]
        public void Test_UserData_OldHeaderOnlyDataReadsAutoFormat()
        {
            TableFileFormatOverride parsed = TableAssetSettings.ParseFileFormat("{\"headerRowCount\":5}");

            Assert.IsTrue(parsed.IsEmpty);
        }

        [Test]
        public void Test_UserData_EmptyFormatAndNoHeaderLeavesMetaClean()
        {
            Assert.AreEqual(string.Empty, TableAssetSettings.FormatFileFormat(string.Empty, new TableFileFormatOverride()));
        }

        [Test]
        public void Test_Encodings_IndexOfDistinguishesBom()
        {
            int plain = TableEncodings.IndexOf(new UTF8Encoding(false));
            int withBom = TableEncodings.IndexOf(new UTF8Encoding(true));

            Assert.AreEqual("UTF-8", TableEncodings.Entries[plain].Label);
            Assert.AreEqual("UTF-8 with BOM", TableEncodings.Entries[withBom].Label);
        }
    }
}
