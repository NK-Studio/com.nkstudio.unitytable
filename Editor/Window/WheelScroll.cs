using UnityEngine;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 휠 입력을 표의 스크롤 위치로 바꿉니다. 가로 입력이 있을 때만 처리하고, 세로만 오는 휠은 ListView에 맡깁니다.
    /// <list type="bullet">
    /// <item>트랙패드 좌우 쓸기·가로(틸트) 휠·macOS의 Shift+휠은 <c>delta.x</c>로 온다.</item>
    /// <item>Windows의 Shift+휠은 <c>delta.y</c>에 Shift가 붙어 온다. 이때는 세로량을 가로로 쓴다.</item>
    /// </list>
    /// </summary>
    public static class WheelScroll
    {
        /// <summary>
        /// 새 스크롤 위치를 계산합니다. 가로 입력이 없으면 false를 돌려주고 위치를 바꾸지 않습니다.
        /// </summary>
        /// <param name="delta">휠 이벤트의 delta입니다.</param>
        /// <param name="isShiftHeld">Shift를 누르고 있으면 true입니다.</param>
        /// <param name="current">지금 스크롤 위치입니다.</param>
        /// <param name="max">스크롤할 수 있는 최대 위치입니다.</param>
        /// <param name="step">휠 한 칸당 이동량(px)입니다. 세로 스크롤과 같은 값을 넘긴다.</param>
        /// <param name="next">새 스크롤 위치입니다. 0..max로 제한됩니다.</param>
        public static bool TryCompute(Vector2 delta, bool isShiftHeld, Vector2 current, Vector2 max, float step, out Vector2 next)
        {
            next = current;

            bool isShiftedVertical = delta.x == 0f && isShiftHeld && delta.y != 0f;

            if (delta.x == 0f && isShiftedVertical == false)
                return false;

            // Shift로 가로로 바꾼 세로 휠은 세로로는 움직이지 않는다. 트랙패드 대각선 쓸기는 두 축을 함께 움직인다.
            float horizontal = isShiftedVertical ? delta.y : delta.x;
            float vertical = isShiftedVertical ? 0f : delta.y;

            next = new Vector2(
                Mathf.Clamp(current.x + horizontal * step, 0f, Mathf.Max(0f, max.x)),
                Mathf.Clamp(current.y + vertical * step, 0f, Mathf.Max(0f, max.y)));

            return true;
        }
    }
}
