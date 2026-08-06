using System.Collections.Generic;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 행과 셀 목록을 RFC 4180 규격의 구분자 텍스트로 직렬화합니다.
    /// </summary>
    public static class DelimitedTextWriter
    {
        /// <summary>
        /// 행 목록을 구분자 텍스트로 직렬화합니다.
        /// </summary>
        /// <param name="rows">직렬화할 행 목록입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        /// <param name="newLine">행 사이에 넣을 개행 문자열입니다.</param>
        /// <param name="endsWithNewLine">마지막 행 뒤에 개행을 붙일지 여부입니다.</param>
        /// <returns>직렬화된 텍스트입니다.</returns>
        public static string Write(
            IReadOnlyList<IReadOnlyList<string>> rows,
            char delimiter,
            string newLine,
            bool endsWithNewLine)
        {
            if (rows == null || rows.Count == 0)
                return string.Empty;

            if (string.IsNullOrEmpty(newLine))
                newLine = "\n";

            StringBuilder builder = new();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rowIndex > 0)
                    builder.Append(newLine);

                IReadOnlyList<string> row = rows[rowIndex];

                for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                        builder.Append(delimiter);

                    AppendField(builder, row[columnIndex], delimiter);
                }
            }

            if (endsWithNewLine)
                builder.Append(newLine);

            return builder.ToString();
        }

        /// <summary>
        /// 셀 값을 필요한 경우에만 인용해 추가합니다.
        /// </summary>
        /// <param name="builder">대상 문자열 버퍼입니다.</param>
        /// <param name="value">셀 값입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        public static void AppendField(StringBuilder builder, string value, char delimiter)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (!NeedsQuotes(value, delimiter))
            {
                builder.Append(value);
                return;
            }

            builder.Append('"');

            foreach (char current in value)
            {
                if (current == '"')
                    builder.Append('"');

                builder.Append(current);
            }

            builder.Append('"');
        }

        // 불필요한 인용은 파일 전체를 diff로 뒤집으므로 반드시 필요한 경우에만 인용한다.
        private static bool NeedsQuotes(string value, char delimiter)
        {
            foreach (char current in value)
            {
                if (current == delimiter || current == '"' || current == '\r' || current == '\n')
                    return true;
            }

            return false;
        }
    }
}
