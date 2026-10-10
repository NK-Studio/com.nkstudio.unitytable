using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 파일 형식 대화상자와 Preferences의 '새 CSV 파일'이 함께 쓰는 선택지입니다.
    /// </summary>
    internal static class TableFormatChoices
    {
        public const string OtherLabel = "기타...";

        public static readonly (string Label, char Value)[] Delimiters =
        {
            ("쉼표 ( , )", ','),
            ("탭", '\t'),
            ("세미콜론 ( ; )", ';'),
            ("콜론 ( : )", ':'),
            ("파이프 ( | )", '|'),
            ("공백", ' '),
        };

        public static readonly (string Label, char Value)[] Quotes =
        {
            ("없음", TableFileOptions.NoQuote),
            ("\"", '"'),
            ("'", '\''),
        };

        // TableQuoteMode 순서(Always, Minimal, Never)와 같다.
        public static readonly string[] QuoteModes = { "항상", "최소", "사용 안 함" };

        public static readonly (string Label, string Value)[] NewLines =
        {
            ("LF", "\n"),
            ("CRLF", "\r\n"),
        };

        /// <summary>
        /// 값이 목록에 있으면 그 위치, 없으면 '기타'(목록 길이)입니다.
        /// </summary>
        public static int IndexOf((string Label, char Value)[] choices, char value)
        {
            for (int index = 0; index < choices.Length; index++)
            {
                if (choices[index].Value == value)
                    return index;
            }

            return choices.Length;
        }
    }
}
