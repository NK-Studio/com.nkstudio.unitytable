using NKStudio.TabularEditor.AssetLinks;
using NKStudio.TabularEditor.Selection;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 깨진 에셋 경로로 볼 값과, 문제 칸 사이를 이동하는 순서를 검증합니다.
    /// </summary>
    public sealed class MissingAssetsTests
    {
        private static readonly CellCoord[] Missing = { new(2, 1), new(2, 3), new(7, 0) };

        [TestCase("Art/Sprite/spr_buf_1002", true)]
        [TestCase("IconPath", false)]
        [TestCase("None", false)]
        [TestCase("-", false)]
        [TestCase("", false)]
        public void Test_LooksLikePath(string value, bool expected)
        {
            Assert.AreEqual(expected, MissingAssets.LooksLikePath(value));
        }

        [Test]
        public void Test_TryFindNext_SameRowToTheRight()
        {
            Assert.IsTrue(MissingAssets.TryFindNext(Missing, new CellCoord(2, 1), out CellCoord next));
            Assert.AreEqual(new CellCoord(2, 3), next);
        }

        [Test]
        public void Test_TryFindNext_NextRow()
        {
            MissingAssets.TryFindNext(Missing, new CellCoord(3, 5), out CellCoord next);
            Assert.AreEqual(new CellCoord(7, 0), next);
        }

        [Test]
        public void Test_TryFindNext_WrapsToFirst()
        {
            MissingAssets.TryFindNext(Missing, new CellCoord(9, 0), out CellCoord next);
            Assert.AreEqual(new CellCoord(2, 1), next);
        }

        [Test]
        public void Test_TryFindNext_EmptyIsFalse()
        {
            Assert.IsFalse(MissingAssets.TryFindNext(new CellCoord[0], new CellCoord(0, 0), out _));
        }
    }
}
