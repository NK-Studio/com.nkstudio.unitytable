using System;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 편집 입력칸의 캐럿 위치에 글자를 넣습니다. 범위가 선택돼 있으면 그 범위를 바꿔 넣는다(일반 입력과 같다).
    /// </summary>
    public static class TextInsertion
    {
        /// <param name="value">지금 입력칸 값입니다.</param>
        /// <param name="cursorIndex">캐럿 위치입니다.</param>
        /// <param name="selectIndex">선택 범위의 다른 끝입니다. 선택이 없으면 cursorIndex와 같다.</param>
        /// <param name="text">넣을 글자입니다.</param>
        /// <param name="caret">넣은 뒤 캐럿이 갈 위치(넣은 글자 바로 뒤)입니다.</param>
        public static string Insert(string value, int cursorIndex, int selectIndex, string text, out int caret)
        {
            value ??= string.Empty;
            text ??= string.Empty;

            int start = Math.Clamp(Math.Min(cursorIndex, selectIndex), 0, value.Length);
            int end = Math.Clamp(Math.Max(cursorIndex, selectIndex), 0, value.Length);

            caret = start + text.Length;
            return value.Substring(0, start) + text + value.Substring(end);
        }
    }
}
