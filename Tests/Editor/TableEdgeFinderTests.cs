using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// Ctrl/Cmd+방향키 도착 셀 규칙(Excel 방식)을 검증합니다.
    /// </summary>
    public sealed class TableEdgeFinderTests
    {
        // 한 행: [a, b, c, _, _, f]
        private const string Row = "a,b,c,,,f";

        [Test]
        public void Test_FindEdge_InsideRunGoesToRunEnd()
        {
            Assert.AreEqual(2, FindRight(Row, 0));
        }

        [Test]
        public void Test_FindEdge_AtRunEndJumpsToNextValue()
        {
            Assert.AreEqual(5, FindRight(Row, 2));
        }

        [Test]
        public void Test_FindEdge_AtTableEndStays()
        {
            Assert.AreEqual(5, FindRight(Row, 5));
        }

        [Test]
        public void Test_FindEdge_NoValueAheadGoesToTableEnd()
        {
            Assert.AreEqual(3, FindRight("a,,,", 0));
        }

        [Test]
        public void Test_FindEdge_LeftFromValueJumpsBackOverBlanks()
        {
            Assert.AreEqual(2, Find(Row, new CellCoord(0, 5), 0, -1).Column);
        }

        [Test]
        public void Test_FindEdge_QuotedEmptyCellCountsAsBlank()
        {
            Assert.AreEqual(2, FindRight("a,\"\",c", 0));
        }

        [Test]
        public void Test_FindEdge_VerticalDownAndUp()
        {
            const string column = "a\nb\n\nd";

            Assert.AreEqual(1, Find(column, new CellCoord(0, 0), 1, 0).Row);
            Assert.AreEqual(1, Find(column, new CellCoord(3, 0), -1, 0).Row);
        }

        private static int FindRight(string text, int fromColumn)
        {
            return Find(text, new CellCoord(0, fromColumn), 0, 1).Column;
        }

        private static CellCoord Find(string text, CellCoord from, int rowDelta, int columnDelta)
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse(text, ',', null));
            return TableEdgeFinder.FindEdge(document, from, rowDelta, columnDelta);
        }
    }
}
