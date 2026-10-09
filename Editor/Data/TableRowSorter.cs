using System;
using System.Globalization;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 한 열의 값을 기준으로 행 순서를 계산합니다. 문서는 바꾸지 않습니다.
    /// <list type="bullet">
    /// <item>열의 값이 모두 숫자면 숫자로, 모두 날짜면 날짜로, 그 밖에는 텍스트로 비교합니다. ("1, 10, 2" → "1, 2, 10")</item>
    /// <item>빈 칸은 정렬 방향과 관계없이 맨 뒤에 둡니다.</item>
    /// <item>같은 값끼리는 원래 순서를 유지합니다. 내림차순에서도 마찬가지입니다.</item>
    /// </list>
    /// </summary>
    public static class TableRowSorter
    {
        // 게임 데이터에서 흔한 ISO 계열만 받는다. 지역 형식(10/09/2026 등)은 월/일 해석이 모호해 텍스트로 둔다.
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy/MM/dd HH:mm",
            "yyyy/MM/dd HH:mm:ss",
        };

        private static readonly CompareInfo TextCompareInfo = CultureInfo.InvariantCulture.CompareInfo;

        private enum ValueKind
        {
            Number,
            Date,
            Text,
        }

        /// <summary>
        /// firstRow부터 마지막 행까지를 지정한 열 기준으로 정렬했을 때의 순서를 계산합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        /// <param name="firstRow">정렬할 첫 행입니다. 그 앞의 행(헤더)은 정렬하지 않습니다.</param>
        /// <param name="column">기준 열입니다.</param>
        /// <param name="descending">내림차순이면 true입니다.</param>
        /// <returns>order[i]는 firstRow 기준으로 몇 번째 행이 i번째 자리로 오는지입니다.
        /// <see cref="TableDocument.ReorderRows"/>에 그대로 넘길 수 있습니다.</returns>
        public static int[] ComputeOrder(TableDocument document, int firstRow, int column, bool descending)
        {
            int count = Math.Max(0, document.RowCount - firstRow);
            string[] values = new string[count];
            bool[] isEmpty = new bool[count];

            for (int index = 0; index < count; index++)
            {
                values[index] = document.GetCell(firstRow + index, column);
                isEmpty[index] = string.IsNullOrWhiteSpace(values[index]);
            }

            // 비교할 때마다 파싱하지 않도록 키를 미리 만든다. 판별과 같은 패스에서 채운다.
            double[] numbers = new double[count];
            long[] dateTicks = new long[count];
            ValueKind kind = DetectKind(values, isEmpty, numbers, dateTicks);

            int[] order = new int[count];

            for (int index = 0; index < count; index++)
                order[index] = index;

            Array.Sort(order, (left, right) =>
            {
                if (isEmpty[left] != isEmpty[right])
                    return isEmpty[left] ? 1 : -1;

                int result = 0;

                if (!isEmpty[left])
                {
                    result = kind switch
                    {
                        ValueKind.Number => numbers[left].CompareTo(numbers[right]),
                        ValueKind.Date => dateTicks[left].CompareTo(dateTicks[right]),
                        _ => CompareText(values[left], values[right]),
                    };

                    if (descending)
                        result = -result;
                }

                // Array.Sort는 안정 정렬이 아니므로, 값이 같으면 원래 위치로 순서를 고정한다.
                return result != 0 ? result : left.CompareTo(right);
            });

            return order;
        }

        private static ValueKind DetectKind(string[] values, bool[] isEmpty, double[] numbers, long[] dateTicks)
        {
            bool hasValue = false;
            bool allNumbers = true;
            bool allDates = true;

            for (int index = 0; index < values.Length; index++)
            {
                if (isEmpty[index])
                    continue;

                hasValue = true;

                if (allNumbers)
                    allNumbers = TryParseNumber(values[index], out numbers[index]);

                if (allDates)
                    allDates = TryParseDate(values[index], out dateTicks[index]);

                if (!allNumbers && !allDates)
                    return ValueKind.Text;
            }

            if (!hasValue)
                return ValueKind.Text;

            if (allNumbers)
                return ValueKind.Number;

            if (allDates)
                return ValueKind.Date;

            return ValueKind.Text;
        }

        // "007"은 7로 본다. NaN·Infinity 같은 단어는 숫자로 치지 않는다.
        private static bool TryParseNumber(string value, out double number)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                && double.IsFinite(number);
        }

        private static bool TryParseDate(string value, out long ticks)
        {
            bool parsed = DateTime.TryParseExact(
                value.Trim(),
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date);

            ticks = parsed ? date.Ticks : 0;
            return parsed;
        }

        // 대소문자를 무시하고 비교하되, 그것만 다르면 소문자를 앞에 둔다. ("apple" < "Apple" < "banana")
        // 두 번째 기준을 런타임의 문화권 정렬에 맡기지 않고 직접 정해 Mono와 .NET에서 결과가 같게 한다.
        private static int CompareText(string left, string right)
        {
            int result = TextCompareInfo.Compare(left, right, CompareOptions.IgnoreCase);

            if (result != 0)
                return result;

            int length = Math.Min(left.Length, right.Length);

            for (int index = 0; index < length; index++)
            {
                char leftChar = left[index];
                char rightChar = right[index];

                if (leftChar == rightChar)
                    continue;

                if (char.IsLower(leftChar) && char.IsUpper(rightChar))
                    return -1;

                if (char.IsUpper(leftChar) && char.IsLower(rightChar))
                    return 1;

                return leftChar.CompareTo(rightChar);
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
