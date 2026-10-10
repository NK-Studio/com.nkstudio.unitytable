using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 파일 형식 대화상자와 Preferences의 '새 CSV 파일'이 함께 쓰는 선택지입니다.
    /// </summary>
    internal static class TableFormatChoices
    {
        // 문구는 언어에 따라 바뀌므로 읽을 때마다 만든다. 값(문자)과 순서는 고정이다.
        public static string OtherLabel => Localization.Get("choice.otherCharacter");

        public static (string Label, char Value)[] Delimiters => new[]
        {
            (Localization.Get("choice.comma"), ','),
            (Localization.Get("choice.tab"), '\t'),
            (Localization.Get("choice.semicolon"), ';'),
            (Localization.Get("choice.colon"), ':'),
            (Localization.Get("choice.pipe"), '|'),
            (Localization.Get("choice.space"), ' '),
        };

        public static (string Label, char Value)[] Quotes => new[]
        {
            (Localization.Get("choice.none"), TableFileOptions.NoQuote),
            ("\"", '"'),
            ("'", '\''),
        };

        // TableQuoteMode 순서(Always, Minimal, Never)와 같다.
        public static string[] QuoteModes => new[]
        {
            Localization.Get("choice.always"),
            Localization.Get("choice.minimal"),
            Localization.Get("choice.never"),
        };

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
