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
            List<List<CellSlot>> slotRows = ParseSlots(text, delimiter, options);
            List<List<string>> rows = new(slotRows.Count);

            foreach (List<CellSlot> slotRow in slotRows)
            {
                List<string> row = new(slotRow.Count);

                foreach (CellSlot slot in slotRow)
                    row.Add(slot.ResolveFromText(text));

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// 셀 문자열을 만들지 않고 원본 텍스트 안의 셀 구간만 기록합니다.
        /// 셀마다 문자열 객체를 두지 않으므로 큰 파일도 메모리를 적게 쓴다.
        /// </summary>
        internal static List<List<CellSlot>> ParseSlots(string text, char delimiter, TableFileOptions options)
        {
            List<List<CellSlot>> rows = new();

            if (string.IsNullOrEmpty(text))
            {
                if (options != null)
                    options.EndsWithNewLine = false;

                return rows;
            }

            List<CellSlot> row = new();
            int fieldStart = 0;

            // 기존 StringBuilder 기반 파서의 field.Length > 0 에 해당한다. 따옴표가 필드 시작으로 해석되는 조건이다.
            bool fieldHasContent = false;
            bool fieldIsQuoted = false;
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
                            fieldHasContent = true;
                            index += 2;
                            continue;
                        }

                        inQuotes = false;
                        index++;
                        continue;
                    }

                    fieldHasContent = true;
                    index++;
                    continue;
                }

                if (current == '"' && !fieldHasContent)
                {
                    inQuotes = true;
                    fieldIsQuoted = true;
                    rowStarted = true;
                    index++;
                    continue;
                }

                if (current == delimiter)
                {
                    row.Add(CellSlot.FromText(fieldStart, index - fieldStart, fieldIsQuoted));
                    index++;
                    fieldStart = index;
                    fieldHasContent = false;
                    fieldIsQuoted = false;
                    rowStarted = true;
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    int fieldEnd = index;
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

                    row.Add(CellSlot.FromText(fieldStart, fieldEnd - fieldStart, fieldIsQuoted));
                    rows.Add(row);

                    // 행마다 셀 수가 거의 같으므로 직전 행 크기로 잡는다. 그냥 두면 List가 두 배씩 자라 행마다 최대 절반을 낭비한다.
                    row = new List<CellSlot>(row.Count);
                    fieldStart = index;
                    fieldHasContent = false;
                    fieldIsQuoted = false;
                    rowStarted = false;
                    continue;
                }

                fieldHasContent = true;
                rowStarted = true;
                index++;
            }

            bool hasTrailingRow = rowStarted || fieldHasContent || row.Count > 0;

            if (hasTrailingRow)
            {
                row.Add(CellSlot.FromText(fieldStart, text.Length - fieldStart, fieldIsQuoted));
                rows.Add(row);
            }

            if (options != null)
                options.EndsWithNewLine = !hasTrailingRow && rows.Count > 0;

            return rows;
        }

        /// <summary>
        /// 따옴표로 시작한 필드의 원본 구간을 셀 값으로 해석합니다. 파서와 같은 규칙을 구간 안에서 다시 적용한다.
        /// 예: <c>"say ""hi"""</c> → <c>say "hi"</c>, 닫는 따옴표 뒤 문자는 그대로 이어 붙는다(<c>"ab"cd</c> → <c>abcd</c>).
        /// </summary>
        internal static string DecodeQuotedField(string text, int start, int length)
        {
            StringBuilder field = new(length);
            bool inQuotes = false;
            int end = start + length;
            int index = start;

            while (index < end)
            {
                char current = text[index];

                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (index + 1 < end && text[index + 1] == '"')
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
                    index++;
                    continue;
                }

                field.Append(current);
                index++;
            }

            return field.ToString();
        }
    }
}
