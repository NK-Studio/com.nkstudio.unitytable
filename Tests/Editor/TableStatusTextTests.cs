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
        public void Test_DescribeDelimiter()
        {
            Assert.AreEqual("구분 기호=[,], 따옴표=[\"](최소)", TableFormatUtility.DescribeDelimiter(TableFormat.Csv));
            Assert.AreEqual("구분 기호=[Tab], 따옴표=[\"](최소)", TableFormatUtility.DescribeDelimiter(TableFormat.Tsv));
        }
    }
}
