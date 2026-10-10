using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 채우기 핸들로 늘린 칸에 넣을 값을 만듭니다. 한 줄(열 하나 또는 행 하나)씩 계산합니다.
    /// <list type="bullet">
    /// <item>숫자 두 칸 이상: 같은 간격으로 이어 간다. [1, 3] → 5, 7</item>
    /// <item>끝에 숫자가 붙은 글자: 그 숫자를 이어 간다. 한 칸이면 1씩. [spr_007] → spr_008 (자릿수 유지)</item>
    /// <item>숫자 한 칸·그 밖의 글자: 복사한다. 여러 칸이면 순서대로 반복한다. [a, b] → a, b, a</item>
    /// </list>
    /// <c>toggle</c>은 놓을 때 Alt/Option을 누른 경우로, 이어 가기와 복사를 서로 바꾼다(숫자 한 칸은 1씩 이어 간다).
    /// 숫자 한 칸을 기본으로 복사하는 것은 Excel과 같다. ID처럼 이어 가려면 Alt를 누르거나 두 칸을 골라 끈다.
    /// </summary>
    public static class FillSeries
    {
        // 소수점 계산이 끝없이 늘어지지 않게 자르는 자릿수다. 표 데이터에서 이보다 정밀한 값은 쓰지 않는다.
        private const int MaxDecimals = 10;

        private static readonly Regex NumberSuffix = new(@"^(.*?)(\d+)$", RegexOptions.CultureInvariant);

        /// <summary>
        /// source 다음에 올 count개의 값을 만듭니다.
        /// </summary>
        /// <param name="source">채우기 방향 순서로 늘어놓은 원본 값입니다. 위·왼쪽으로 채울 때는 아래·오른쪽부터 거꾸로 넘긴다.</param>
        /// <param name="count">만들 값 개수입니다.</param>
        /// <param name="toggle">이어 가기와 복사를 바꿀지 여부입니다.</param>
        /// <param name="backward">위·왼쪽으로 채우는지 여부입니다. 한 칸에서 이어 갈 때 1씩 줄어든다.</param>
        public static string[] Extend(IReadOnlyList<string> source, int count, bool toggle, bool backward)
        {
            string[] result = new string[Math.Max(0, count)];

            if (source == null || source.Count == 0 || result.Length == 0)
                return result;

            int singleStep = backward ? -1 : 1;

            if (TryParseNumbers(source, out decimal[] numbers, out int scale))
            {
                bool isSeries = source.Count == 1 ? toggle : toggle == false;

                if (isSeries)
                {
                    decimal step = source.Count == 1 ? singleStep : (numbers[^1] - numbers[0]) / (source.Count - 1);
                    FillNumbers(result, numbers[^1], step, scale);
                    return result;
                }
            }
            else if (toggle == false && TryParseSuffixed(source, singleStep, out string prefix, out long last, out long suffixStep, out int width))
            {
                for (int index = 0; index < result.Length; index++)
                {
                    long next = last + suffixStep * (index + 1);
                    result[index] = next < 0 ? $"{prefix}{next}" : prefix + next.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
                }

                return result;
            }

            for (int index = 0; index < result.Length; index++)
                result[index] = source[index % source.Count] ?? string.Empty;

            return result;
        }

        private static void FillNumbers(string[] result, decimal last, decimal step, int scale)
        {
            for (int index = 0; index < result.Length; index++)
            {
                decimal value = Math.Round(last + step * (index + 1), MaxDecimals);
                result[index] = FormatNumber(value, scale);
            }
        }

        // 원본에 소수 자릿수가 있으면(1.50) 그만큼은 맞춰 쓰고, 계산으로 생긴 자릿수는 뒤의 0을 떼어 짧게 쓴다.
        private static string FormatNumber(decimal value, int scale)
        {
            string text = value.ToString("0.##########", CultureInfo.InvariantCulture);
            int dot = text.IndexOf('.');
            int decimals = dot < 0 ? 0 : text.Length - dot - 1;

            if (decimals >= scale)
                return text;

            return value.ToString("F" + scale, CultureInfo.InvariantCulture);
        }

        // 모든 칸이 평범한 숫자일 때만 숫자로 본다. 앞자리 0(007)은 자릿수를 지켜야 하므로 글자+숫자 규칙으로 넘긴다.
        private static bool TryParseNumbers(IReadOnlyList<string> source, out decimal[] numbers, out int scale)
        {
            numbers = new decimal[source.Count];
            scale = 0;

            for (int index = 0; index < source.Count; index++)
            {
                string text = source[index]?.Trim();

                if (string.IsNullOrEmpty(text) || HasLeadingZero(text))
                    return false;

                if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number) == false)
                    return false;

                numbers[index] = number;

                int dot = text.IndexOf('.');

                if (dot >= 0)
                    scale = Math.Max(scale, text.Length - dot - 1);
            }

            return true;
        }

        private static bool HasLeadingZero(string text)
        {
            string digits = text.TrimStart('-', '+');
            return digits.Length > 1 && digits[0] == '0' && digits[1] != '.';
        }

        // 모든 칸이 같은 앞부분 + 끝 숫자일 때 이어 간다. 두 칸 이상이면 간격이 정수로 나눠떨어져야 한다.
        private static bool TryParseSuffixed(
            IReadOnlyList<string> source,
            int singleStep,
            out string prefix,
            out long last,
            out long step,
            out int width)
        {
            prefix = null;
            last = 0;
            step = singleStep;
            width = 0;

            long first = 0;

            for (int index = 0; index < source.Count; index++)
            {
                Match match = NumberSuffix.Match(source[index] ?? string.Empty);

                if (match.Success == false || long.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long number) == false)
                    return false;

                if (index == 0)
                {
                    prefix = match.Groups[1].Value;
                    first = number;
                }
                else if (match.Groups[1].Value != prefix)
                {
                    return false;
                }

                last = number;
                width = match.Groups[2].Length;
            }

            if (source.Count >= 2)
            {
                long span = last - first;

                if (span % (source.Count - 1) != 0)
                    return false;

                step = span / (source.Count - 1);
            }

            return true;
        }
    }
}
