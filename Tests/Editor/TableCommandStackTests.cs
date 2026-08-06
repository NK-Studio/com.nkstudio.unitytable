using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// Undo 스택과 셀 교체 작업의 되돌리기 정확성을 검증합니다.
    /// </summary>
    public sealed class TableCommandStackTests
    {
        private static TableDocument CreateDocument()
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse("a,b\nc,d", ',', null));

            return document;
        }

        [Test]
        public void SetCells_UndoRestoresPreviousValue()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            stack.Execute(document, new SetCellsCommand("셀 편집", 1, 1, new[] { new[] { "x" } }));
            Assert.AreEqual("x", document.GetCell(1, 1));

            stack.Undo(document);
            Assert.AreEqual("d", document.GetCell(1, 1));

            stack.Redo(document);
            Assert.AreEqual("x", document.GetCell(1, 1));
        }

        [Test]
        public void SetCells_GrowsTableAndUndoShrinksBack()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            string[][] values = { new[] { "1", "2", "3" }, new[] { "4", "5", "6" } };
            stack.Execute(document, new SetCellsCommand("붙여넣기", 1, 1, values));

            Assert.AreEqual(3, document.RowCount);
            Assert.AreEqual(4, document.ColumnCount);
            Assert.AreEqual("6", document.GetCell(2, 3));

            stack.Undo(document);

            Assert.AreEqual(2, document.RowCount);
            Assert.AreEqual(2, document.ColumnCount);
            Assert.AreEqual("d", document.GetCell(1, 1));
        }

        [Test]
        public void RemoveRows_UndoRestoresValues()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            stack.Execute(document, new RemoveRowsCommand(0, 1));
            Assert.AreEqual(1, document.RowCount);
            Assert.AreEqual("c", document.GetCell(0, 0));

            stack.Undo(document);
            Assert.AreEqual(2, document.RowCount);
            Assert.AreEqual("a", document.GetCell(0, 0));
        }

        [Test]
        public void RemoveColumns_UndoRestoresValues()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            stack.Execute(document, new RemoveColumnsCommand(0, 1));
            Assert.AreEqual(1, document.ColumnCount);
            Assert.AreEqual("b", document.GetCell(0, 0));

            stack.Undo(document);
            Assert.AreEqual(2, document.ColumnCount);
            Assert.AreEqual("a", document.GetCell(0, 0));
        }

        [Test]
        public void MarkSaved_ClearsDirtyWhenReturningToSavePoint()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            Assert.IsFalse(stack.IsDirty);

            stack.Execute(document, new SetCellsCommand("셀 편집", 0, 0, new[] { new[] { "z" } }));
            Assert.IsTrue(stack.IsDirty);

            stack.MarkSaved();
            Assert.IsFalse(stack.IsDirty);

            stack.Execute(document, new SetCellsCommand("셀 편집", 0, 1, new[] { new[] { "y" } }));
            Assert.IsTrue(stack.IsDirty);

            stack.Undo(document);
            Assert.IsFalse(stack.IsDirty);
        }

        [Test]
        public void RemoveRows_KeepsAtLeastOneRow()
        {
            TableDocument document = CreateDocument();
            TableCommandStack stack = new();

            stack.Execute(document, new RemoveRowsCommand(0, 5));

            Assert.AreEqual(1, document.RowCount);
        }
    }
}
