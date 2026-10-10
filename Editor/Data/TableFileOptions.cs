using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 파일을 읽고 쓰는 형식입니다. 인코딩·개행·구분 기호·따옴표 규칙을 담고, 저장할 때 그대로 재현한다.
    /// </summary>
    public sealed class TableFileOptions
    {
        /// <summary>
        /// 파일을 읽고 쓸 때 사용하는 인코딩입니다.
        /// </summary>
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);

        /// <summary>
        /// 저장 시 사용할 개행 문자열입니다.
        /// </summary>
        public string NewLine { get; set; } = "\n";

        /// <summary>
        /// 파일이 개행으로 끝나는지 여부입니다.
        /// </summary>
        public bool EndsWithNewLine { get; set; }

        /// <summary>
        /// 필드 구분 기호입니다. 읽을 때 확장자(.csv는 쉼표, .tsv는 탭)나 .meta에 기록한 값으로 정해진다.
        /// </summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// 셀을 감싸는 따옴표 문자입니다. <see cref="NoQuote"/>이면 따옴표를 쓰지 않는다.
        /// </summary>
        public char Quote { get; set; } = '"';

        /// <summary>
        /// 저장할 때 셀을 따옴표로 감싸는 규칙입니다.
        /// </summary>
        public TableQuoteMode QuoteMode { get; set; } = TableQuoteMode.Minimal;

        /// <summary>
        /// 따옴표를 쓰지 않는다는 뜻의 <see cref="Quote"/> 값입니다.
        /// </summary>
        public const char NoQuote = '\0';

        /// <summary>
        /// 현재 설정을 복사한 새 인스턴스를 반환합니다.
        /// </summary>
        /// <returns>복사된 파일 옵션입니다.</returns>
        public TableFileOptions Clone()
        {
            TableFileOptions clone = new();
            clone.Encoding = Encoding;
            clone.NewLine = NewLine;
            clone.EndsWithNewLine = EndsWithNewLine;
            clone.Delimiter = Delimiter;
            clone.Quote = Quote;
            clone.QuoteMode = QuoteMode;

            return clone;
        }
    }
}
