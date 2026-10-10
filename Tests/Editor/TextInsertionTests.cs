using NKStudio.TabularEditor.Window;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 편집 중 Alt/Option+Enter로 줄바꿈을 넣는 규칙을 검증합니다.
    /// </summary>
    public sealed class TextInsertionTests
    {
        [Test]
        public void Test_Insert_AtCaret()
        {
            Assert.AreEqual("ab\ncd", TextInsertion.Insert("abcd", 2, 2, "\n", out int caret));
            Assert.AreEqual(3, caret);
        }

        [Test]
        public void Test_Insert_ReplacesSelectionEitherDirection()
        {
            Assert.AreEqual("a\nd", TextInsertion.Insert("abcd", 1, 3, "\n", out int forward));
            Assert.AreEqual("a\nd", TextInsertion.Insert("abcd", 3, 1, "\n", out int backward));
            Assert.AreEqual(2, forward);
            Assert.AreEqual(2, backward);
        }

        [Test]
        public void Test_Insert_EndAndEmpty()
        {
            Assert.AreEqual("abc\n", TextInsertion.Insert("abc", 3, 3, "\n", out _));
            Assert.AreEqual("\n", TextInsertion.Insert(null, 0, 0, "\n", out int caret));
            Assert.AreEqual(1, caret);
        }
    }
}
