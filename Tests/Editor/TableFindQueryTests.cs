using System.Linq;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 찾기 옵션(대소문자·정규식·단어 단위)의 일치 판정, 치환, 검색 순서를 검증합니다.
    /// </summary>
    public sealed class TableFindQueryTests
    {
        [Test]
        public void Test_IsMatch_IgnoresCaseByDefault()
        {
            Assert.IsTrue(new TableFindQuery("cat", false, false, false).IsMatch("Black Cat"));
            Assert.IsFalse(new TableFindQuery("cat", true, false, false).IsMatch("Black Cat"));
        }

        [Test]
        public void Test_IsMatch_WholeWordSkipsPartialWords()
        {
            TableFindQuery query = new("cat", false, false, true);

            Assert.IsTrue(query.IsMatch("a cat here"));
            Assert.IsFalse(query.IsMatch("category"));
        }

        [Test]
        public void Test_IsMatch_Regex()
        {
            TableFindQuery query = new(@"^10\d\d$", false, true, false);

            Assert.IsTrue(query.IsMatch("1005"));
            Assert.IsFalse(query.IsMatch("10050"));
        }

        [Test]
        public void Test_InvalidRegex_IsReportedAndMatchesNothing()
        {
            TableFindQuery query = new("(", false, true, false);

            Assert.IsFalse(query.IsValid);
            Assert.IsFalse(query.IsMatch("("));
        }

        [Test]
        public void Test_Replace_RegexGroupReference()
        {
            TableFindQuery query = new(@"(\w+)@(\w+)", false, true, false);

            Assert.AreEqual("b at a", query.Replace("a@b", "$2 at $1", false));
        }

        [Test]
        public void Test_Replace_PlainTreatsDollarLiterally()
        {
            TableFindQuery query = new("price", false, false, false);

            Assert.AreEqual("$1 total", query.Replace("price total", "$1", false));
        }

        [Test]
        public void Test_Replace_WholeWordTreatsDollarLiterally()
        {
            TableFindQuery query = new("price", false, false, true);

            Assert.AreEqual("$1 prices", query.Replace("price prices", "$1", false));
        }

        [Test]
        public void Test_Replace_ReplacesEveryOccurrenceInCell()
        {
            TableFindQuery query = new("a", false, false, false);

            Assert.AreEqual("xbx", query.Replace("aba", "x", false));
        }

        [Test]
        public void Test_Replace_PreserveCase()
        {
            TableFindQuery query = new("cat", false, false, false);

            Assert.AreEqual("DOG", query.Replace("CAT", "dog", true));
            Assert.AreEqual("dog", query.Replace("cat", "Dog", true));
            Assert.AreEqual("Dog", query.Replace("Cat", "dog", true));
        }

        [Test]
        public void Test_EnumerateCells_RowMajorByDefault()
        {
            CellCoord[] order = TableFindQuery.EnumerateCells(0, 0, 1, 1, false).ToArray();

            CollectionAssert.AreEqual(
                new[] { new CellCoord(0, 0), new CellCoord(0, 1), new CellCoord(1, 0), new CellCoord(1, 1) },
                order);
        }

        [Test]
        public void Test_EnumerateCells_VerticalIsColumnMajor()
        {
            CellCoord[] order = TableFindQuery.EnumerateCells(0, 0, 1, 1, true).ToArray();

            CollectionAssert.AreEqual(
                new[] { new CellCoord(0, 0), new CellCoord(1, 0), new CellCoord(0, 1), new CellCoord(1, 1) },
                order);
        }

        [Test]
        public void Test_CompareOrder_FollowsDirection()
        {
            CellCoord upperRight = new(0, 1);
            CellCoord lowerLeft = new(1, 0);

            Assert.Less(TableFindQuery.CompareOrder(upperRight, lowerLeft, false), 0);
            Assert.Greater(TableFindQuery.CompareOrder(upperRight, lowerLeft, true), 0);
        }
    }
}
