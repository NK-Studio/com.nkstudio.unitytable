using System.Collections.Generic;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 행과 셀 목록을 RFC 4180 규격의 구분자 텍스트로 직렬화합니다. 따옴표 문자와 감싸는 규칙은 파일 형식에 따릅니다.
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
        public static string Write(IReadOnlyList<IReadOnlyList<string>> rows, char delimiter, string newLine, bool endsWithNewLine)
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
        /// 문서를 구분자 텍스트로 직렬화합니다. 셀을 하나씩 읽어 쓰므로 문서 전체를 문자열 목록으로 복사하지 않는다.
        /// </summary>
        /// <param name="document">직렬화할 문서입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        /// <param name="newLine">행 사이에 넣을 개행 문자열입니다.</param>
        /// <param name="endsWithNewLine">마지막 행 뒤에 개행을 붙일지 여부입니다.</param>
        /// <returns>직렬화된 텍스트입니다.</returns>
        public static string Write(TableDocument document, char delimiter, string newLine, bool endsWithNewLine)
        {
            TableFileOptions options = new()
            {
                Delimiter = delimiter,
                NewLine = newLine,
                EndsWithNewLine = endsWithNewLine,
            };

            return Write(document, options);
        }

        /// <summary>
        /// 문서를 파일 형식(구분 기호·따옴표·감싸는 규칙·개행)에 맞춰 직렬화합니다.
        /// </summary>
        /// <param name="document">직렬화할 문서입니다.</param>
        /// <param name="options">파일 형식입니다.</param>
        /// <returns>직렬화된 텍스트입니다.</returns>
        public static string Write(TableDocument document, TableFileOptions options)
        {
            if (document == null || document.RowCount == 0)
                return string.Empty;

            options ??= new TableFileOptions();

            char delimiter = options.Delimiter;
            string newLine = string.IsNullOrEmpty(options.NewLine) ? "\n" : options.NewLine;

            StringBuilder builder = new();

            for (int rowIndex = 0; rowIndex < document.RowCount; rowIndex++)
            {
                if (rowIndex > 0)
                    builder.Append(newLine);

                for (int columnIndex = 0; columnIndex < document.ColumnCount; columnIndex++)
                {
                    if (columnIndex > 0)
                        builder.Append(delimiter);

                    AppendField(builder, document.GetCell(rowIndex, columnIndex), delimiter, options.Quote, options.QuoteMode);
                }
            }

            if (options.EndsWithNewLine)
                builder.Append(newLine);

            return builder.ToString();
        }

        /// <summary>
        /// 셀 값을 필요한 경우에만 큰따옴표로 인용해 추가합니다.
        /// </summary>
        /// <param name="builder">대상 문자열 버퍼입니다.</param>
        /// <param name="value">셀 값입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        public static void AppendField(StringBuilder builder, string value, char delimiter)
        {
            AppendField(builder, value, delimiter, '"', TableQuoteMode.Minimal);
        }

        /// <summary>
        /// 셀 값을 감싸는 규칙에 따라 추가합니다.
        /// </summary>
        /// <param name="builder">대상 문자열 버퍼입니다.</param>
        /// <param name="value">셀 값입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        /// <param name="quote">따옴표 문자입니다. <see cref="TableFileOptions.NoQuote"/>이면 감싸지 않는다.</param>
        /// <param name="quoteMode">감싸는 규칙입니다.</param>
        public static void AppendField(StringBuilder builder, string value, char delimiter, char quote, TableQuoteMode quoteMode)
        {
            value ??= string.Empty;

            bool usesQuote = quote != TableFileOptions.NoQuote && quoteMode != TableQuoteMode.Never;
            bool quoted = usesQuote && (quoteMode == TableQuoteMode.Always || NeedsQuotes(value, delimiter, quote));

            if (quoted == false)
            {
                builder.Append(value);
                return;
            }

            builder.Append(quote);

            foreach (char current in value)
            {
                if (current == quote)
                    builder.Append(quote);

                builder.Append(current);
            }

            builder.Append(quote);
        }

        // 불필요한 인용은 파일 전체를 diff로 뒤집으므로 '최소' 규칙에서는 반드시 필요한 경우에만 인용한다.
        private static bool NeedsQuotes(string value, char delimiter, char quote)
        {
            foreach (char current in value)
            {
                if (current == delimiter || current == quote || current == '\r' || current == '\n')
                    return true;
            }

            return false;
        }
    }
}
