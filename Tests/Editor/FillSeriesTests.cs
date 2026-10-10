using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 채우기 핸들이 만드는 값(이어 가기·복사·Alt 전환·위로 채우기)을 검증합니다.
    /// </summary>
    public sealed class FillSeriesTests
    {
        [Test]
        public void Test_Extend_SingleNumberCopies()
        {
            CollectionAssert.AreEqual(new[] { "1002", "1002" }, Extend(new[] { "1002" }, 2));
        }

        [Test]
        public void Test_Extend_SingleNumberWithToggleCounts()
        {
            CollectionAssert.AreEqual(new[] { "1003", "1004" }, Extend(new[] { "1002" }, 2, toggle: true));
        }

        [Test]
        public void Test_Extend_BackwardSingleNumberWithToggleCountsDown()
        {
            CollectionAssert.AreEqual(new[] { "999", "998" }, FillSeries.Extend(new[] { "1000" }, 2, true, true));
        }

        [Test]
        public void Test_Extend_TwoNumbersContinueStep()
        {
            CollectionAssert.AreEqual(new[] { "5", "7" }, Extend(new[] { "1", "3" }, 2));
        }

        [Test]
        public void Test_Extend_DecimalsKeepSourceScale()
        {
            CollectionAssert.AreEqual(new[] { "2.00", "2.50" }, Extend(new[] { "1.00", "1.50" }, 2));
            CollectionAssert.AreEqual(new[] { "0.7", "0.9" }, Extend(new[] { "0.3", "0.5" }, 2));
        }

        [Test]
        public void Test_Extend_NumbersWithToggleCopyPattern()
        {
            CollectionAssert.AreEqual(new[] { "1", "3", "1" }, Extend(new[] { "1", "3" }, 3, toggle: true));
        }

        [Test]
        public void Test_Extend_TextWithNumberCounts()
        {
            CollectionAssert.AreEqual(
                new[] { "Art/spr_buff_1001", "Art/spr_buff_1002" },
                Extend(new[] { "Art/spr_buff_1000" }, 2));
        }

        [Test]
        public void Test_Extend_TextWithNumberKeepsZeroPadding()
        {
            CollectionAssert.AreEqual(new[] { "item_008", "item_009", "item_010" }, Extend(new[] { "item_007" }, 3));
        }

        [Test]
        public void Test_Extend_LeadingZeroNumberKeepsWidth()
        {
            CollectionAssert.AreEqual(new[] { "008" }, Extend(new[] { "007" }, 1));
        }

        [Test]
        public void Test_Extend_TextWithNumberToggleCopies()
        {
            CollectionAssert.AreEqual(new[] { "a1", "a1" }, Extend(new[] { "a1" }, 2, toggle: true));
        }

        [Test]
        public void Test_Extend_TextWithNumberTwoCellsUseStep()
        {
            CollectionAssert.AreEqual(new[] { "a5", "a7" }, Extend(new[] { "a1", "a3" }, 2));
        }

        [Test]
        public void Test_Extend_DifferentPrefixesRepeat()
        {
            CollectionAssert.AreEqual(new[] { "a1", "b2", "a1" }, Extend(new[] { "a1", "b2" }, 3));
        }

        [Test]
        public void Test_Extend_PlainTextRepeats()
        {
            CollectionAssert.AreEqual(new[] { "SeasonPass", "SeasonPass" }, Extend(new[] { "SeasonPass" }, 2));
            CollectionAssert.AreEqual(new[] { "x", "y", "x" }, Extend(new[] { "x", "y" }, 3));
        }

        [Test]
        public void Test_Extend_BackwardSingleCellCountsDown()
        {
            CollectionAssert.AreEqual(new[] { "a4", "a3" }, FillSeries.Extend(new[] { "a5" }, 2, false, true));
        }

        [Test]
        public void Test_Extend_EmptyCellRepeats()
        {
            CollectionAssert.AreEqual(new[] { "1", "", "1" }, Extend(new[] { "1", "" }, 3));
        }

        private static string[] Extend(string[] source, int count, bool toggle = false)
        {
            return FillSeries.Extend(source, count, toggle, false);
        }
    }
}
