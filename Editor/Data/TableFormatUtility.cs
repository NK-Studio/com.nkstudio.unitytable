using System;
using System.IO;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 테이블 형식과 구분자, 파일 확장자를 변환하는 유틸리티입니다.
    /// </summary>
    public static class TableFormatUtility
    {
        /// <summary>
        /// 클립보드 교환에 사용하는 구분자입니다. Excel과 구글 시트가 탭을 사용합니다.
        /// </summary>
        public const char ClipboardDelimiter = '\t';

        /// <summary>
        /// 형식에 대응하는 구분자 문자를 반환합니다.
        /// </summary>
        /// <param name="format">테이블 형식입니다.</param>
        /// <returns>구분자 문자입니다.</returns>
        public static char GetDelimiter(TableFormat format)
        {
            return format == TableFormat.Tsv ? '\t' : ',';
        }

        /// <summary>
        /// 상태 표시줄에 보일 인코딩 이름입니다. BOM이 있으면 따로 표시합니다. 예: "UTF-8", "UTF-8 BOM", "UTF-16 LE".
        /// </summary>
        public static string DescribeEncoding(Encoding encoding)
        {
            if (encoding == null)
                return "UTF-8";

            bool hasBom = encoding.GetPreamble().Length > 0;

            if (encoding is UTF8Encoding)
                return hasBom ? "UTF-8 BOM" : "UTF-8";

            if (encoding is UnicodeEncoding)
                return encoding.CodePage == 1201 ? "UTF-16 BE" : "UTF-16 LE";

            return encoding.WebName.ToUpperInvariant();
        }

        /// <summary>
        /// 상태 표시줄에 보일 개행 이름입니다. "LF" 또는 "CRLF"입니다.
        /// </summary>
        public static string DescribeNewLine(string newLine)
        {
            return newLine == "\r\n" ? "CRLF" : "LF";
        }

        /// <summary>
        /// 상태 표시줄에 보일 구분 기호·따옴표 설명입니다. 예: <c>Delimiter=[,], Quote=["](Minimal)</c>
        /// </summary>
        public static string DescribeDelimiter(TableFileOptions options)
        {
            options ??= new TableFileOptions();

            string quote = options.Quote == TableFileOptions.NoQuote ? Localization.Get("choice.none") : options.Quote.ToString();
            return Localization.Format("status.delimiter", DescribeDelimiterChar(options.Delimiter), quote, DescribeQuoteMode(options.QuoteMode));
        }

        /// <summary>
        /// 구분 기호를 사람이 읽을 이름으로 바꿉니다. 보이지 않는 문자는 이름으로 적는다(탭 → "Tab", 공백 → "Space").
        /// </summary>
        public static string DescribeDelimiterChar(char delimiter)
        {
            return delimiter switch
            {
                '\t' => "Tab",
                ' ' => Localization.Get("choice.space"),
                _ => delimiter.ToString(),
            };
        }

        /// <summary>
        /// 따옴표 감싸는 규칙의 표시 이름입니다.
        /// </summary>
        public static string DescribeQuoteMode(TableQuoteMode quoteMode)
        {
            return quoteMode switch
            {
                TableQuoteMode.Always => Localization.Get("choice.always"),
                TableQuoteMode.Never => Localization.Get("choice.never"),
                _ => Localization.Get("choice.minimal"),
            };
        }

        /// <summary>
        /// 파일 경로의 확장자로 테이블 형식을 판별합니다.
        /// </summary>
        /// <param name="path">판별할 파일 경로입니다.</param>
        /// <param name="format">판별된 테이블 형식입니다.</param>
        /// <returns>지원하는 확장자면 true입니다.</returns>
        public static bool TryGetFormat(string path, out TableFormat format)
        {
            format = TableFormat.Csv;

            if (string.IsNullOrEmpty(path))
                return false;

            string extension = Path.GetExtension(path);

            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                format = TableFormat.Csv;
                return true;
            }

            if (string.Equals(extension, ".tsv", StringComparison.OrdinalIgnoreCase))
            {
                format = TableFormat.Tsv;
                return true;
            }

            return false;
        }
    }
}
