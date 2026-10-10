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
        /// 상태 표시줄에 보일 구분 기호·따옴표 설명입니다. 따옴표는 구분자·따옴표·개행이 든 셀에만 붙이므로 "최소"입니다.
        /// </summary>
        public static string DescribeDelimiter(TableFormat format)
        {
            string delimiter = format == TableFormat.Tsv ? "Tab" : ",";
            return $"구분 기호=[{delimiter}], 따옴표=[\"](최소)";
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
