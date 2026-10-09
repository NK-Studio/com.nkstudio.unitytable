using System;
using System.IO;
using System.Text;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 백그라운드 불러오기가 쓰는 파일 읽기 경로를 검증합니다.
    /// </summary>
    public sealed class TableDocumentIOTests
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
        public void Load_ReturnsSameHashAsComputeFileHash()
        {
            File.WriteAllText(_tempPath, "a,b\nc,d\n", new UTF8Encoding(false));

            TableDocumentIO.Load(_tempPath, out string fileHash);

            // 저장 시 외부 변경 감지가 이 둘을 비교하므로 같아야 한다.
            Assert.AreEqual(TableDocumentIO.ComputeFileHash(_tempPath), fileHash);
        }

        [Test]
        public void Load_MissingFileReturnsEmptyHash()
        {
            TableDocument document = TableDocumentIO.Load(_tempPath, out string fileHash);

            Assert.AreEqual(string.Empty, fileHash);
            Assert.AreEqual(1, document.RowCount);
        }
    }
}
