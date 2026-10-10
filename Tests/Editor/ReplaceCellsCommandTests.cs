using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 흩어진 셀을 한 번에 바꾸는 작업의 실행·되돌리기·다시 실행을 검증합니다.
    /// </summary>
    public sealed class ReplaceCellsCommandTests
    {
        private static TableDocument CreateDocument()
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse("a,b\nc,d", ',', null));

            return document;
        }

        [Test]
        public void Test_ReplaceCells_UndoRestoresAllCellsAtOnce()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            stack.Execute(document, new ReplaceCellsCommand("모두 바꾸기", new[] { (0, 0, "x"), (1, 1, "y") }));
            Assert.AreEqual("x", document.GetCell(0, 0));
            Assert.AreEqual("y", document.GetCell(1, 1));
            Assert.AreEqual("b", document.GetCell(0, 1));

            stack.Undo(document);
            Assert.AreEqual("a", document.GetCell(0, 0));
            Assert.AreEqual("d", document.GetCell(1, 1));

            stack.Redo(document);
            Assert.AreEqual("x", document.GetCell(0, 0));
            Assert.AreEqual("y", document.GetCell(1, 1));
        }
    }
}
