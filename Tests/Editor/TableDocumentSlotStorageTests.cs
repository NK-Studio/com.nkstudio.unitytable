using System;
using System.IO;
using System.Text;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 파일에서 읽은 문서가 셀 문자열 대신 원본 텍스트 구간으로 값을 들고 있어도
    /// 읽기·편집·검색·저장 결과가 같은지 검증합니다.
    /// </summary>
    public sealed class TableDocumentSlotStorageTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), $"tabular-editor-{Guid.NewGuid():N}.csv");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        [Test]
        public void Load_DecodesQuotedCells()
        {
            TableDocument document = LoadCsv("\"a,b\",\"say \"\"hi\"\"\",\"x\ny\"");

            Assert.AreEqual("a,b", document.GetCell(0, 0));
            Assert.AreEqual("say \"hi\"", document.GetCell(0, 1));
            Assert.AreEqual("x\ny", document.GetCell(0, 2));
        }

        [Test]
        public void SetCell_ReplacesLoadedValue()
        {
            TableDocument document = LoadCsv("a,b\nc,d");

            document.SetCell(1, 0, "edited");
            document.SetCell(1, 0, "again");

            Assert.AreEqual("again", document.GetCell(1, 0));
            Assert.AreEqual("d", document.GetCell(1, 1));
        }

        [Test]
        public void SetCell_ToEmptyClearsValue()
        {
            TableDocument document = LoadCsv("a,b");

            document.SetCell(0, 0, "edited");
            document.SetCell(0, 0, string.Empty);

            Assert.AreEqual(string.Empty, document.GetCell(0, 0));
        }

        [Test]
        public void InsertColumns_KeepsLoadedValuesInPlace()
        {
            TableDocument document = LoadCsv("a,b\nc,d");

            document.InsertColumns(1, null, 1);

            Assert.AreEqual("a", document.GetCell(0, 0));
            Assert.AreEqual(string.Empty, document.GetCell(0, 1));
            Assert.AreEqual("b", document.GetCell(0, 2));
            Assert.AreEqual("d", document.GetCell(1, 2));
        }

        [Test]
        public void RemoveRows_ReturnsLoadedValues()
        {
            TableDocument document = LoadCsv("a,b\nc,d\ne,f");

            var removed = document.RemoveRows(1, 1);

            CollectionAssert.AreEqual(new[] { "c", "d" }, removed[0]);
            Assert.AreEqual("e", document.GetCell(1, 0));
        }

        [Test]
        public void CellContains_MatchesLoadedQuotedAndEditedCells()
        {
            TableDocument document = LoadCsv("plain cat,\"quoted, cat\",dog");
            document.SetCell(0, 2, "edited CAT");

            Assert.IsTrue(document.CellContains(0, 0, "cat", StringComparison.Ordinal));
            Assert.IsTrue(document.CellContains(0, 1, "d, c", StringComparison.Ordinal));
            Assert.IsTrue(document.CellContains(0, 2, "cat", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(document.CellContains(0, 2, "cat", StringComparison.Ordinal));
        }

        [Test]
        public void CellContains_DoesNotMatchAcrossCellBoundary()
        {
            TableDocument document = LoadCsv("ab,cd");

            Assert.IsFalse(document.CellContains(0, 0, "b,c", StringComparison.Ordinal));
            Assert.IsFalse(document.CellContains(0, 0, "abc", StringComparison.Ordinal));
        }

        [Test]
        public void Serialize_RoundTripsLoadedFileExactly()
        {
            const string original = "이름,설명\n박지훈,\"쉼표, 따옴표\"\" 그리고\n줄바꿈\"\n";
            TableDocument document = LoadCsv(original);

            string text = Encoding.UTF8.GetString(TableDocumentIO.Serialize(document));

            Assert.AreEqual(original, text);
        }

        private TableDocument LoadCsv(string content)
        {
            File.WriteAllText(_tempPath, content, new UTF8Encoding(false));
            return TableDocumentIO.Load(_tempPath);
        }
    }
}
