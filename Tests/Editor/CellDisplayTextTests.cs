using NKStudio.TabularEditor.Window;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 셀에 그릴 글자를 칸 너비만큼 자르는 규칙을 검증합니다. 400px·12px 칸은 최대 175자다.
    /// </summary>
    public sealed class CellDisplayTextTests
    {
        [Test]
        public void Test_Clip_ShortValueUnchanged()
        {
            Assert.AreEqual("Art/Sprite/icon", CellDisplayText.Clip("Art/Sprite/icon", 400f, 12f));
        }

        [Test]
        public void Test_Clip_LongValueCutToVisibleLength()
        {
            string clipped = CellDisplayText.Clip(new string('a', 2000), 400f, 12f);

            Assert.AreEqual(175, clipped.Length);
        }

        [Test]
        public void Test_Clip_WiderColumnKeepsMore()
        {
            Assert.Greater(
                CellDisplayText.Clip(new string('a', 2000), 800f, 12f).Length,
                CellDisplayText.Clip(new string('a', 2000), 400f, 12f).Length);
        }

        [Test]
        public void Test_Clip_DoesNotSplitSurrogatePair()
        {
            // 174번째 글자 자리에서 이모지(서로게이트 쌍)가 시작된다.
            string value = new string('a', 174) + "😺" + new string('b', 100);
            string clipped = CellDisplayText.Clip(value, 400f, 12f);

            Assert.IsFalse(char.IsHighSurrogate(clipped[clipped.Length - 1]));
        }

        [Test]
        public void Test_Clip_NullAndEmpty()
        {
            Assert.AreEqual(string.Empty, CellDisplayText.Clip(null, 400f, 12f));
            Assert.AreEqual(string.Empty, CellDisplayText.Clip(string.Empty, 400f, 12f));
        }
    }
}
