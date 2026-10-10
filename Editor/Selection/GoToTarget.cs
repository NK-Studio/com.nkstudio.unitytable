using System;

namespace NKStudio.TabularEditor.Selection
{
    /// <summary>
    /// Ctrl/Cmd+G 입력을 이동할 셀로 바꿉니다. 사람이 보는 번호(1부터)를 받아 문서 좌표(0부터)로 돌려줍니다.
    /// 행은 왼쪽 행 번호(숫자), 열은 위쪽 열 제목(글자)과 같은 표기를 쓴다.
    /// <list type="bullet">
    /// <item><c>120</c> → 120행, 열은 지금 열 그대로</item>
    /// <item><c>C120</c> → C열 120행 (스프레드시트 셀 주소)</item>
    /// <item><c>120:3</c>·<c>120:C</c> → 120행 3열 ('행:열' 표기도 받는다)</item>
    /// </list>
    /// 범위를 넘는 번호는 마지막 행·열로 맞춘다. 해석할 수 없거나 0 이하면 false다.
    /// </summary>
    public static class GoToTarget
    {
        public static bool TryParse(string text, int rowCount, int columnCount, int currentColumn, out CellCoord target)
        {
            target = default;

            if (string.IsNullOrWhiteSpace(text) || rowCount <= 0 || columnCount <= 0)
                return false;

            if (TrySplit(text.Trim(), out string rowText, out string columnText) == false)
                return false;

            if (int.TryParse(rowText, out int rowNumber) == false || rowNumber <= 0)
                return false;

            int column = currentColumn;

            if (columnText != null && TryParseColumn(columnText, out column) == false)
                return false;

            target = new CellCoord(
                Math.Min(rowNumber, rowCount) - 1,
                Math.Clamp(column, 0, columnCount - 1));

            return true;
        }

        // "120" → (120, 없음), "C120" → (120, C), "120:C" → (120, C). 열이 없으면 columnText는 null이다.
        private static bool TrySplit(string text, out string rowText, out string columnText)
        {
            rowText = text;
            columnText = null;

            string[] parts = text.Split(':');

            if (parts.Length == 2)
            {
                rowText = parts[0].Trim();
                columnText = parts[1].Trim();
                return true;
            }

            if (parts.Length > 2)
                return false;

            // 셀 주소: 앞의 글자가 열, 뒤의 숫자가 행이다.
            int letterCount = 0;

            while (letterCount < text.Length && char.IsLetter(text[letterCount]))
                letterCount++;

            if (letterCount == 0)
                return true;

            columnText = text.Substring(0, letterCount);
            rowText = text.Substring(letterCount).Trim();
            return true;
        }

        // 열은 번호(1부터) 또는 열 제목 글자(A, B, …, Z, AA)로 받는다. 돌려주는 값은 0부터다.
        private static bool TryParseColumn(string text, out int column)
        {
            column = 0;

            if (text.Length == 0)
                return false;

            if (int.TryParse(text, out int columnNumber))
            {
                column = columnNumber - 1;
                return columnNumber > 0;
            }

            int value = 0;

            foreach (char letter in text.ToUpperInvariant())
            {
                if (letter < 'A' || letter > 'Z')
                    return false;

                // 26진법이지만 0이 없는 표기다(A=1, Z=26, AA=27). 아주 긴 입력은 넘치기 전에 끊는다.
                if (value > int.MaxValue / 26 - 26)
                    return false;

                value = value * 26 + (letter - 'A' + 1);
            }

            column = value - 1;
            return true;
        }
    }
}
