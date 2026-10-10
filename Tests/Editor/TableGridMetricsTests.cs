using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Window;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 글꼴 크기에 비례하는 표 크기, 자동 맞춤 최대 너비, 새 CSV 빈 표 내용을 검증합니다.
    /// </summary>
    public sealed class TableGridMetricsTests
    {
        [Test]
        public void Test_FromFontSize_BaseSizeMatchesOriginalLayout()
        {
            TableGridMetrics metrics = TableGridMetrics.FromFontSize(12);

            Assert.AreEqual(20f, metrics.RowHeight);
            Assert.AreEqual(46f, metrics.RowNumberColumnWidth);
            Assert.AreEqual(22f, metrics.HeaderHeight);
            Assert.AreEqual(160f, metrics.DefaultColumnWidth);
        }

        [Test]
        public void Test_FromFontSize_ScalesProportionally()
        {
            TableGridMetrics metrics = TableGridMetrics.FromFontSize(24);

            Assert.AreEqual(40f, metrics.RowHeight);
            Assert.AreEqual(92f, metrics.RowNumberColumnWidth);
        }

        [Test]
        public void Test_FromFontSize_RowNumberFontHasFloor()
        {
            Assert.AreEqual(8f, TableGridMetrics.FromFontSize(8).RowNumberFontSize);
        }

        [Test]
        public void Test_MaxAutoFitWidth_IsPercentOfDataArea()
        {
            Assert.AreEqual(700f, TableGridMetrics.MaxAutoFitWidth(1000f, 70, 40f));
        }

        [Test]
        public void Test_MaxAutoFitWidth_NeverBelowMinColumnWidth()
        {
            Assert.AreEqual(40f, TableGridMetrics.MaxAutoFitWidth(100f, 1, 40f));
        }

        [Test]
        public void Test_TemplateBuilder_DefaultIsEmptyFiveByFive()
        {
            string text = TableTemplateBuilder.CreateNormalizedText(5, 5, new TableFileOptions { EndsWithNewLine = true });

            Assert.AreEqual(",,,,\n,,,,\n,,,,\n,,,,\n,,,,\n", text);
        }

        [Test]
        public void Test_TemplateBuilder_UsesDelimiterAndAlwaysQuotes()
        {
            TableFileOptions options = new() { Delimiter = ';', QuoteMode = TableQuoteMode.Always, EndsWithNewLine = false };

            Assert.AreEqual("\"\";\"\"\n\"\";\"\"", TableTemplateBuilder.CreateNormalizedText(2, 2, options));
        }

        [Test]
        public void Test_TemplateBuilder_FileBytesKeepCrlfAndBom()
        {
            TableFileOptions options = new()
            {
                Encoding = new System.Text.UTF8Encoding(true),
                NewLine = "\r\n",
                EndsWithNewLine = true,
            };

            byte[] bytes = TableTemplateBuilder.CreateFileBytes(1, 2, options);

            CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF, (byte)',', (byte)'\r', (byte)'\n' }, bytes);
        }
    }
}
