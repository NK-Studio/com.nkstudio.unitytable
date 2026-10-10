using System;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 셀에 그릴 글자를 칸에 보일 만큼으로 자릅니다.
    /// <para>
    /// TRAP: UI Toolkit은 칸을 넘는 부분을 …로 자르더라도 Label에 넣은 글자 전체를 배치한다. 2천 자짜리 셀은 한 칸에 약 10ms가 들어,
    /// 긴 셀이 많은 파일(data_level_override.csv)에서 스크롤 프레임이 0.3초씩 멈췄다(2026-10-10 실측, 200자로 자르면 10배 빠름).
    /// 칸 너비에 들어갈 수 있는 글자 수보다 길면 그 뒤는 어차피 보이지 않으므로 넘기지 않는다. 화면은 자르기 전과 같다(여전히 …가 붙는다).
    /// </para>
    /// 편집·복사·저장은 문서의 원래 값을 쓴다. 이 함수는 그리기와 폭 재기에만 쓴다.
    /// </summary>
    public static class CellDisplayText
    {
        // 가장 좁은 글자(i, l, |, . 등)의 폭을 글꼴 크기에 대한 비율로 낮게 잡은 값이다.
        // 실제보다 좁게 잡아야 칸을 채울 만큼의 글자가 항상 남는다(넓게 잡으면 칸 끝이 비어 보인다).
        private const float NarrowestGlyphRatio = 0.2f;

        // 계산 오차와 …를 붙일 여유로 몇 글자 더 남긴다.
        private const int SpareCharacters = 8;

        /// <summary>
        /// width(px) 칸에 fontSize(px) 글꼴로 보일 수 있는 만큼만 남깁니다. 그보다 짧으면 그대로 돌려줍니다.
        /// 예) 400px, 12px → 최대 175자(400 / 2.4 + 8)
        /// </summary>
        public static string Clip(string value, float width, float fontSize)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            int maxLength = MaxVisibleLength(width, fontSize);

            if (value.Length <= maxLength)
                return value;

            // 서로게이트 쌍(이모지 등)의 앞 반쪽에서 자르면 깨진 글자가 그려진다. 한 글자 덜 남긴다.
            if (char.IsHighSurrogate(value[maxLength - 1]))
                maxLength--;

            return value.Substring(0, maxLength);
        }

        public static int MaxVisibleLength(float width, float fontSize)
        {
            if (float.IsNaN(width) || width <= 0f || fontSize <= 0f)
                return SpareCharacters;

            return (int)Math.Ceiling(width / (fontSize * NarrowestGlyphRatio)) + SpareCharacters;
        }
    }
}
