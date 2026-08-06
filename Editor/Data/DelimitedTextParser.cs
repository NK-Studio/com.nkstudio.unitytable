using System.Collections.Generic;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// RFC 4180 규격의 구분자 기반 텍스트를 파싱합니다. 구분자를 인자로 받으므로 CSV와 TSV가 같은 코드를 사용합니다.
    /// </summary>
    public static class DelimitedTextParser
    {
        /// <summary>
        /// 구분자 텍스트를 행과 셀 목록으로 파싱합니다.
        /// </summary>
        /// <param name="text">파싱할 원본 텍스트입니다.</param>
        /// <param name="delimiter">필드 구분자입니다.</param>
        /// <param name="options">개행 형태와 최종 개행 여부를 기록할 파일 옵션입니다. null이면 무시합니다.</param>
        /// <returns>행마다 셀 문자열을 담은 목록입니다. 행별 셀 개수는 다를 수 있습니다.</returns>
        public static List<List<string>> Parse(string text, char delimiter, TableFileOptions options)
        {
            List<List<string>> rows = new();

            if (string.IsNullOrEmpty(text))
            {
                if (options != null)
                    options.EndsWithNewLine = false;

                return rows;
            }

            StringBuilder field = new();
            List<string> row = new();
            bool inQuotes = false;
            bool rowStarted = false;
            bool newLineDetected = false;
            int index = 0;

            while (index < text.Length)
            {
                char current = text[index];

                if (inQuotes)
                {
                    if (current == '"')
                    {
                        // 인용 구간 안의 ""는 리터럴 따옴표 하나로 해석한다.
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index += 2;
                            continue;
                        }

                        inQuotes = false;
                        index++;
                        continue;
                    }

                    field.Append(current);
                    index++;
                    continue;
                }

                if (current == '"' && field.Length == 0)
                {
                    inQuotes = true;
                    rowStarted = true;
                    index++;
                    continue;
                }

                if (current == delimiter)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rowStarted = true;
                    index++;
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    string newLine = "\n";

                    if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        newLine = "\r\n";
                        index += 2;
                    }
                    else
                    {
                        if (current == '\r')
                            newLine = "\r";

                        index++;
                    }

                    if (!newLineDetected && options != null)
                    {
                        options.NewLine = newLine;
                        newLineDetected = true;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    rowStarted = false;
                    continue;
                }

                field.Append(current);
                rowStarted = true;
                index++;
            }

            bool hasTrailingRow = rowStarted || field.Length > 0 || row.Count > 0;

            if (hasTrailingRow)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            if (options != null)
                options.EndsWithNewLine = !hasTrailingRow && rows.Count > 0;

            return rows;
        }
    }
}
