using NKStudio.TabularEditor.Selection;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// Ctrl/Cmd+G 입력 해석을 검증합니다. 표는 100행 × 30열, 지금 열은 2(0부터)라고 둔다.
    /// </summary>
    public sealed class GoToTargetTests
    {
        [Test]
        public void Test_TryParse_RowOnlyKeepsCurrentColumn()
        {
            Assert.AreEqual(new CellCoord(11, 2), Parse("12"));
        }

        [Test]
        public void Test_TryParse_RowAndColumnNumber()
        {
            Assert.AreEqual(new CellCoord(11, 2), Parse("12:3"));
        }

        [Test]
        public void Test_TryParse_RowAndColumnLetters()
        {
            Assert.AreEqual(new CellCoord(11, 2), Parse("12:c"));
            Assert.AreEqual(new CellCoord(0, 26), Parse(" 1 : AA "));
        }

        [Test]
        public void Test_TryParse_CellAddress()
        {
            Assert.AreEqual(new CellCoord(11, 2), Parse("C12"));
            Assert.AreEqual(new CellCoord(0, 26), Parse("aa1"));
        }

        [Test]
        public void Test_TryParse_ClampsToLastRowAndColumn()
        {
            Assert.AreEqual(new CellCoord(99, 29), Parse("9999:999"));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("0")]
        [TestCase("-3")]
        [TestCase("5:")]
        [TestCase("5:0")]
        [TestCase("5:3:1")]
        [TestCase("5:A1")]
        [TestCase("C")]
        [TestCase("C0")]
        [TestCase("12C")]
        [TestCase("가12")]
        public void Test_TryParse_RejectsInvalidInput(string text)
        {
            Assert.IsFalse(GoToTarget.TryParse(text, 100, 30, 2, out _));
        }

        private static CellCoord Parse(string text)
        {
            Assert.IsTrue(GoToTarget.TryParse(text, 100, 30, 2, out CellCoord target), text);
            return target;
        }
    }
}
