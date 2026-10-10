using System.Text;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 상태 표시줄에 보이는 인코딩·개행·구분 기호 설명을 검증합니다.
    /// </summary>
    public sealed class TableStatusTextTests
    {
        private System.IDisposable _language;

        // 기대값은 영어 문구다. 사용자가 고른 언어(EditorPrefs)와 상관없이 영어로 고정한다.
        [SetUp]
        public void SetUp()
        {
            _language = Localization.Override(Language.English);
        }

        [TearDown]
        public void TearDown()
        {
            _language.Dispose();
        }

        [Test]
        public void Test_DescribeEncoding_DistinguishesBom()
        {
            Assert.AreEqual("UTF-8", TableFormatUtility.DescribeEncoding(new UTF8Encoding(false)));
            Assert.AreEqual("UTF-8 BOM", TableFormatUtility.DescribeEncoding(new UTF8Encoding(true)));
        }

        [Test]
        public void Test_DescribeEncoding_Utf16ByteOrder()
        {
            Assert.AreEqual("UTF-16 LE", TableFormatUtility.DescribeEncoding(new UnicodeEncoding(false, true)));
            Assert.AreEqual("UTF-16 BE", TableFormatUtility.DescribeEncoding(new UnicodeEncoding(true, true)));
        }

        [Test]
        public void Test_DescribeNewLine()
        {
            Assert.AreEqual("LF", TableFormatUtility.DescribeNewLine("\n"));
            Assert.AreEqual("CRLF", TableFormatUtility.DescribeNewLine("\r\n"));
        }

        [Test]
        public void Test_DescribeDelimiter_Defaults()
        {
            Assert.AreEqual("Delimiter=[,], Quote=[\"](Minimal)", TableFormatUtility.DescribeDelimiter(new TableFileOptions()));
            Assert.AreEqual(
                "Delimiter=[Tab], Quote=[\"](Minimal)",
                TableFormatUtility.DescribeDelimiter(new TableFileOptions { Delimiter = '\t' }));
        }

        [Test]
        public void Test_DescribeDelimiter_CustomQuoteAndMode()
        {
            TableFileOptions options = new()
            {
                Delimiter = ';',
                Quote = TableFileOptions.NoQuote,
                QuoteMode = TableQuoteMode.Always,
            };

            Assert.AreEqual("Delimiter=[;], Quote=[None](Always)", TableFormatUtility.DescribeDelimiter(options));
        }

        [Test]
        public void Test_DescribeDelimiter_Korean()
        {
            using (Localization.Override(Language.Korean))
            {
                Assert.AreEqual("구분 기호=[,], 따옴표=[\"](최소)", TableFormatUtility.DescribeDelimiter(new TableFileOptions()));
            }
        }
    }
}
