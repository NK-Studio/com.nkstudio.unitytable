using NKStudio.TabularEditor.Window;
using NUnit.Framework;
using UnityEngine;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 셀 편집 입력칸이 내용에 따라 오른쪽, 그다음 아래로 커지는 규칙을 검증합니다.
    /// </summary>
    public sealed class EditOverlayLayoutTests
    {
        // 셀 100x20이 (50, 40)에 있고, 표 영역은 오른쪽 450·아래 300까지다.
        private static readonly Rect Cell = new(50f, 40f, 100f, 20f);
        private static readonly Rect Viewport = new(0f, 0f, 450f, 300f);

        // 줄바꿈 높이: 글자 영역 폭 100마다 한 줄(16px)씩 쓰는 가짜 측정이다.
        private const float LineHeight = 16f;
        private const float TextLength = 1000f;

        private static float FakeWrappedHeight(float width)
        {
            return Mathf.Ceil(TextLength / width) * LineHeight;
        }

        // 편집 입력칸 최소 높이: 셀 두 줄만큼이다.
        private const float MinHeight = 40f;

        private static EditOverlayPlacement Compute(float textWidth, bool hasLineBreak = false)
        {
            return EditOverlayLayout.Compute(Cell, Viewport, textWidth, hasLineBreak, FakeWrappedHeight, 10f, 4f, MinHeight);
        }

        [Test]
        public void Test_Compute_ShortTextKeepsCellWidthWithMinHeight()
        {
            EditOverlayPlacement placement = Compute(40f);

            Assert.AreEqual(new Rect(Cell.x, Cell.y, Cell.width, MinHeight), placement.Rect);
            Assert.IsFalse(placement.IsWrapped);
        }

        [Test]
        public void Test_Compute_BottomRowOpensUpward()
        {
            Rect lastRowCell = new(50f, 280f, 100f, 20f);

            EditOverlayPlacement placement = EditOverlayLayout.Compute(
                lastRowCell, Viewport, 40f, false, FakeWrappedHeight, 10f, 4f, MinHeight);

            Assert.AreEqual(MinHeight, placement.Rect.height);
            Assert.AreEqual(Viewport.yMax - MinHeight, placement.Rect.y);
        }

        [Test]
        public void Test_Compute_LongTextGrowsRight()
        {
            EditOverlayPlacement placement = Compute(200f);

            Assert.AreEqual(210f, placement.Rect.width);
            Assert.AreEqual(MinHeight, placement.Rect.height);
            Assert.IsFalse(placement.IsWrapped);
        }

        [Test]
        public void Test_Compute_StopsAtViewportRightAndWraps()
        {
            EditOverlayPlacement placement = Compute(900f);

            Assert.AreEqual(400f, placement.Rect.width);
            Assert.IsTrue(placement.IsWrapped);
            Assert.Greater(placement.Rect.height, Cell.height);
        }

        [Test]
        public void Test_Compute_HeightStopsAtViewportHeight()
        {
            EditOverlayPlacement placement = EditOverlayLayout.Compute(
                Cell, Viewport, 5000f, false, _ => 10000f, 10f, 4f, MinHeight);

            Assert.AreEqual(Viewport.height, placement.Rect.height);
            Assert.AreEqual(Viewport.yMin, placement.Rect.y);
        }

        [Test]
        public void Test_Compute_CellBeyondViewportRightKeepsCellWidth()
        {
            Rect cellAtEdge = new(400f, 40f, 100f, 20f);

            EditOverlayPlacement placement = EditOverlayLayout.Compute(
                cellAtEdge, Viewport, 40f, false, FakeWrappedHeight, 10f, 4f, MinHeight);

            Assert.AreEqual(cellAtEdge.width, placement.Rect.width);
        }

        [Test]
        public void Test_Compute_LineBreakAlwaysWraps()
        {
            EditOverlayPlacement placement = Compute(40f, hasLineBreak: true);

            Assert.IsTrue(placement.IsWrapped);
        }
    }
}
