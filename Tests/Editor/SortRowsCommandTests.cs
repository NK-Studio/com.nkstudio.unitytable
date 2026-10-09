using System.Text;
using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 한 열 기준 행 정렬의 비교 규칙과 되돌리기를 검증합니다.
    /// </summary>
    public sealed class SortRowsCommandTests
    {
        [Test]
        public void Execute_SortsNumbersByValueNotText()
        {
            TableDocument document = CreateDocument("1\n10\n2");

            Sort(document, 0, descending: false);

            CollectionAssert.AreEqual(new[] { "1", "2", "10" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_SortsNegativeAndDecimalNumbers()
        {
            TableDocument document = CreateDocument("-500\n2\n-12.5\n980.5");

            Sort(document, 0, descending: false);

            CollectionAssert.AreEqual(new[] { "-500", "-12.5", "2", "980.5" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_SortsDates()
        {
            TableDocument document = CreateDocument("2025-03-15\n2018-02-14\n2024-02-29");

            Sort(document, 0, descending: true);

            CollectionAssert.AreEqual(new[] { "2025-03-15", "2024-02-29", "2018-02-14" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_PutsLowercaseBeforeUppercaseOfSameText()
        {
            TableDocument document = CreateDocument("banana\nApple\napple");

            Sort(document, 0, descending: false);

            CollectionAssert.AreEqual(new[] { "apple", "Apple", "banana" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_SortsHangulInDictionaryOrder()
        {
            TableDocument document = CreateDocument("한지민\n가나다\n김철수\n강호동");

            Sort(document, 0, descending: false);

            CollectionAssert.AreEqual(new[] { "가나다", "강호동", "김철수", "한지민" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_TreatsMixedColumnAsText()
        {
            TableDocument document = CreateDocument("10\nA\n9");

            Sort(document, 0, descending: false);

            // 숫자로 비교했다면 9가 10보다 앞이다. 텍스트 비교라 "10" < "9" < "A".
            CollectionAssert.AreEqual(new[] { "10", "9", "A" }, GetColumn(document, 0));
        }

        [Test]
        public void Execute_KeepsEmptyCellsLastInBothDirections()
        {
            TableDocument ascending = CreateDocument("2\n\n1");
            TableDocument descending = CreateDocument("2\n\n1");

            Sort(ascending, 0, descending: false);
            Sort(descending, 0, descending: true);

            CollectionAssert.AreEqual(new[] { "1", "2", "" }, GetColumn(ascending, 0));
            CollectionAssert.AreEqual(new[] { "2", "1", "" }, GetColumn(descending, 0));
        }

        [Test]
        public void Execute_KeepsOriginalOrderOfEqualValues()
        {
            TableDocument document = CreateDocument("1,a\n2,b\n1,c\n2,d");

            Sort(document, 0, descending: true);

            CollectionAssert.AreEqual(new[] { "b", "d", "a", "c" }, GetColumn(document, 1));
        }

        [Test]
        public void Execute_LeavesRowsBeforeFirstRowInPlace()
        {
            TableDocument document = CreateDocument("ID\n3\n1\n2");

            new SortRowsCommand(1, 0, false).Execute(document);

            CollectionAssert.AreEqual(new[] { "ID", "1", "2", "3" }, GetColumn(document, 0));
        }

        [Test]
        public void Undo_RestoresOriginalOrderAndRedoSortsAgain()
        {
            TableDocument document = CreateDocument("c,3\na,1\nb,2");
            TableCommandStack stack = new();

            stack.Execute(document, new SortRowsCommand(0, 0, false));
            stack.Undo(document);

            CollectionAssert.AreEqual(new[] { "c", "a", "b" }, GetColumn(document, 0));
            CollectionAssert.AreEqual(new[] { "3", "1", "2" }, GetColumn(document, 1));

            stack.Redo(document);

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, GetColumn(document, 0));
        }

        [Test]
        public void Serialize_WritesSortedRowsAsIs()
        {
            TableDocument document = CreateDocument("이름,설명\n나,\"b, 2\"\n가,a\n");
            document.FileOptions = new TableFileOptions { NewLine = "\n", EndsWithNewLine = true, Encoding = new UTF8Encoding(false) };

            new SortRowsCommand(1, 0, false).Execute(document);

            string text = Encoding.UTF8.GetString(TableDocumentIO.Serialize(document));
            Assert.AreEqual("이름,설명\n가,a\n나,\"b, 2\"\n", text);
        }

        private static TableDocument CreateDocument(string text)
        {
            TableDocument document = new();
            document.SetContent(DelimitedTextParser.Parse(text, ',', null));
            return document;
        }

        private static void Sort(TableDocument document, int column, bool descending)
        {
            new SortRowsCommand(0, column, descending).Execute(document);
        }

        private static string[] GetColumn(TableDocument document, int column)
        {
            return document.GetColumnValues(column);
        }
    }
}
