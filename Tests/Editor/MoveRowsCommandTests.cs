using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 행·열 묶음 이동과 되돌리기를 검증합니다.
    /// </summary>
    public sealed class MoveRowsCommandTests
    {
        [Test]
        public void Test_MoveRows_UpOneRow()
        {
            TableDocument document = CreateDocument("a\nb\nc\nd");

            new MoveRowsCommand(2, 1, -1).Execute(document);

            CollectionAssert.AreEqual(new[] { "a", "c", "b", "d" }, document.GetColumnValues(0));
        }

        [Test]
        public void Test_MoveRows_BlockDownOneRow()
        {
            TableDocument document = CreateDocument("a\nb\nc\nd");

            new MoveRowsCommand(0, 2, 1).Execute(document);

            CollectionAssert.AreEqual(new[] { "c", "a", "b", "d" }, document.GetColumnValues(0));
        }

        [Test]
        public void Test_MoveRows_UndoRestoresOrder()
        {
            TableDocument document = CreateDocument("a\nb\nc\nd");
            MoveRowsCommand command = new(1, 2, 1);

            command.Execute(document);
            command.Undo(document);

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, document.GetColumnValues(0));
        }

        [Test]
        public void Test_MoveColumns_RightOneColumn()
        {
            TableDocument document = CreateDocument("a,b,c\n1,2,3");

            new MoveColumnsCommand(0, 1, 1).Execute(document);

            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, document.GetRowValues(0));
            CollectionAssert.AreEqual(new[] { "2", "1", "3" }, document.GetRowValues(1));
        }

        [Test]
        public void Test_MoveColumns_BlockLeftAndUndo()
        {
            TableDocument document = CreateDocument("a,b,c,d");
            MoveColumnsCommand command = new(2, 2, -1);

            command.Execute(document);
            CollectionAssert.AreEqual(new[] { "a", "c", "d", "b" }, document.GetRowValues(0));

            command.Undo(document);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, document.GetRowValues(0));
        }

        private static TableDocument CreateDocument(string text)
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse(text, ',', null));
            return document;
        }
    }
}
