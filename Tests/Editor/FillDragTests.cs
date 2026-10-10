using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 채우기 핸들 끌기의 범위 판정과 채울 값 배치를 검증합니다. 원본 선택은 B2:C3(행 1~2, 열 1~2)이라고 둔다.
    /// </summary>
    public sealed class FillDragTests
    {
        [Test]
        public void Test_TryGetTarget_InsideSelectionDoesNothing()
        {
            Assert.IsFalse(Target(new CellCoord(2, 2), 0, out _));
        }

        [Test]
        public void Test_TryGetTarget_DownExtendsRows()
        {
            Assert.IsTrue(Target(new CellCoord(5, 2), 0, out FillTarget target));
            Assert.AreEqual(FillDirection.Down, target.Direction);
            Assert.AreEqual(3, target.Count);
            Assert.AreEqual((1, 1, 5, 2), (target.FirstRow, target.FirstColumn, target.LastRow, target.LastColumn));
        }

        [Test]
        public void Test_TryGetTarget_PicksAxisWithLargerOverflow()
        {
            Assert.IsTrue(Target(new CellCoord(4, 6), 0, out FillTarget target));
            Assert.AreEqual(FillDirection.Right, target.Direction);
            Assert.AreEqual(4, target.Count);
        }

        [Test]
        public void Test_TryGetTarget_UpExtendsRows()
        {
            Assert.IsTrue(Target(new CellCoord(0, 1), 0, out FillTarget target));
            Assert.AreEqual(FillDirection.Up, target.Direction);
            Assert.AreEqual(1, target.Count);
            Assert.AreEqual(0, target.FirstRow);
        }

        [Test]
        public void Test_TryGetTarget_UpDoesNotEnterHeaderRows()
        {
            // 헤더 행이 0번 하나뿐이고 원본은 1번 행부터라, 위로 끌어도 채울 본문 행이 없다.
            Assert.IsFalse(Target(new CellCoord(0, 1), 1, out _));
        }

        [Test]
        public void Test_BuildValues_DownCountsEachColumn()
        {
            TableDocument document = CreateDocument("a1,1\na2,3");
            FillTarget target = new(FillDirection.Down, 2, 0, 0, 3, 1);

            string[][] values = FillDrag.BuildValues(document, 0, 0, 1, 1, target, false, out int originRow, out int originColumn);

            Assert.AreEqual((2, 0), (originRow, originColumn));
            CollectionAssert.AreEqual(new[] { "a3", "5" }, values[0]);
            CollectionAssert.AreEqual(new[] { "a4", "7" }, values[1]);
        }

        [Test]
        public void Test_BuildValues_UpPlacesNearestCellLast()
        {
            TableDocument document = CreateDocument("\n\nitem_5");
            FillTarget target = new(FillDirection.Up, 2, 0, 0, 2, 0);

            string[][] values = FillDrag.BuildValues(document, 2, 0, 2, 0, target, false, out int originRow, out _);

            Assert.AreEqual(0, originRow);
            Assert.AreEqual("item_3", values[0][0]);
            Assert.AreEqual("item_4", values[1][0]);
        }

        [Test]
        public void Test_BuildValues_RightFillsEachRow()
        {
            TableDocument document = CreateDocument("x,\ny,");
            FillTarget target = new(FillDirection.Right, 1, 0, 0, 1, 1);

            string[][] values = FillDrag.BuildValues(document, 0, 0, 1, 0, target, false, out _, out int originColumn);

            Assert.AreEqual(1, originColumn);
            Assert.AreEqual("x", values[0][0]);
            Assert.AreEqual("y", values[1][0]);
        }

        private static bool Target(CellCoord pointer, int headerRowCount, out FillTarget target)
        {
            return FillDrag.TryGetTarget(1, 1, 2, 2, pointer, headerRowCount, out target);
        }

        private static TableDocument CreateDocument(string text)
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse(text, ',', null));
            return document;
        }
    }
}
