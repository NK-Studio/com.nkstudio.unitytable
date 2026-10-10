using NKStudio.TabularEditor.Window;
using NUnit.Framework;
using UnityEngine;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 휠 입력을 가로·세로 스크롤 위치로 바꾸는 규칙을 검증합니다.
    /// </summary>
    public sealed class WheelScrollTests
    {
        private const float Step = 10f;
        private static readonly Vector2 Max = new(1000f, 1000f);
        private static readonly Vector2 Start = new(100f, 100f);

        [Test]
        public void Test_TryCompute_HorizontalDeltaMovesHorizontally()
        {
            Assert.IsTrue(WheelScroll.TryCompute(new Vector2(2f, 0f), false, Start, Max, Step, out Vector2 next));
            Assert.AreEqual(new Vector2(120f, 100f), next);
        }

        [Test]
        public void Test_TryCompute_ShiftVerticalMovesHorizontallyOnly()
        {
            Assert.IsTrue(WheelScroll.TryCompute(new Vector2(0f, 3f), true, Start, Max, Step, out Vector2 next));
            Assert.AreEqual(new Vector2(130f, 100f), next);
        }

        [Test]
        public void Test_TryCompute_DiagonalMovesBothAxes()
        {
            Assert.IsTrue(WheelScroll.TryCompute(new Vector2(1f, -1f), false, Start, Max, Step, out Vector2 next));
            Assert.AreEqual(new Vector2(110f, 90f), next);
        }

        [Test]
        public void Test_TryCompute_ClampsToRange()
        {
            WheelScroll.TryCompute(new Vector2(-50f, 0f), false, Start, Max, Step, out Vector2 left);
            WheelScroll.TryCompute(new Vector2(500f, 0f), false, Start, Max, Step, out Vector2 right);

            Assert.AreEqual(0f, left.x);
            Assert.AreEqual(1000f, right.x);
        }

        [Test]
        public void Test_TryCompute_VerticalOnlyIsNotHandled()
        {
            Assert.IsFalse(WheelScroll.TryCompute(new Vector2(0f, 3f), false, Start, Max, Step, out Vector2 next));
            Assert.AreEqual(Start, next);
        }
    }
}
