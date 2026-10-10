using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NKStudio.TabularEditor.Selection;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 찾기·바꾸기 조건 하나입니다. 대소문자 구분, 정규식, 단어 단위 옵션을 해석해 셀 일치 판정과 치환을 맡습니다.
    /// 정규식·단어 단위가 모두 꺼진 단순 모드에서는 문서가 원본 텍스트에서 직접 비교하는 빠른 경로를 씁니다.
    /// </summary>
    public sealed class TableFindQuery
    {
        // 사용자가 입력한 정규식이 파국적 역추적에 빠져도 에디터가 멈추지 않도록 셀 하나당 시간을 제한한다.
        // 50ms는 일반 셀 길이에서는 닿을 일이 없고, 수천 셀을 훑어도 체감 지연이 크지 않은 값이다.
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(50);

        private readonly Regex _regex;
        private readonly StringComparison _comparison;

        /// <summary>
        /// 찾기 조건을 만듭니다. 잘못된 정규식이면 <see cref="IsValid"/>가 false입니다.
        /// </summary>
        public TableFindQuery(string keyword, bool matchCase, bool useRegex, bool wholeWord)
        {
            Keyword = keyword ?? string.Empty;
            MatchCase = matchCase;
            UseRegex = useRegex;
            WholeWord = wholeWord;
            _comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (Keyword.Length == 0 || IsPlain)
                return;

            string pattern = useRegex ? Keyword : Regex.Escape(Keyword);

            if (wholeWord)
                pattern = $@"\b(?:{pattern})\b";

            RegexOptions options = RegexOptions.CultureInvariant;

            if (matchCase == false)
                options |= RegexOptions.IgnoreCase;

            try
            {
                _regex = new Regex(pattern, options, MatchTimeout);
            }
            catch (ArgumentException)
            {
                _hasInvalidRegex = true;
            }
        }

        public string Keyword { get; }

        public bool MatchCase { get; }

        public bool UseRegex { get; }

        public bool WholeWord { get; }

        /// <summary>
        /// 검색어가 비어 있는지 여부입니다. 비어 있으면 아무 셀과도 일치하지 않습니다.
        /// </summary>
        public bool IsEmpty => Keyword.Length == 0;

        /// <summary>
        /// 조건을 해석할 수 없을 때(잘못된 정규식) 표시할 메시지입니다. 정상이면 null입니다.
        /// </summary>
        // 문구는 읽을 때 현재 언어로 만든다. 언어를 바꿔도 검색 결과를 다시 만들 필요가 없다.
        public string ErrorMessage => _hasInvalidRegex ? Localization.Get("search.invalidRegex") : null;

        private readonly bool _hasInvalidRegex;

        public bool IsValid => _hasInvalidRegex == false;

        /// <summary>
        /// 문서의 빠른 부분 문자열 비교를 쓸 수 있는 단순 모드인지 여부입니다.
        /// 단순 모드끼리만 이전 결과를 좁혀 쓸 수 있습니다.
        /// </summary>
        public bool IsPlain => UseRegex == false && WholeWord == false;

        /// <summary>
        /// 문서의 셀이 조건과 일치하는지 확인합니다.
        /// </summary>
        public bool IsMatch(TableDocument document, int row, int column)
        {
            if (IsEmpty || IsValid == false)
                return false;

            if (IsPlain)
                return document.CellContains(row, column, Keyword, _comparison);

            return IsMatch(document.GetCell(row, column));
        }

        /// <summary>
        /// 값이 조건과 일치하는지 확인합니다.
        /// </summary>
        public bool IsMatch(string value)
        {
            if (IsEmpty || IsValid == false || value == null)
                return false;

            if (IsPlain)
                return value.IndexOf(Keyword, _comparison) >= 0;

            try
            {
                return _regex.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// 값 안의 일치 부분을 모두 바꿉니다. 정규식 모드에서는 $1 같은 그룹 참조를 쓸 수 있고,
        /// 그 밖의 모드에서는 바꿀 문자열을 글자 그대로 넣습니다.
        /// </summary>
        /// <param name="value">원래 셀 값입니다.</param>
        /// <param name="replacement">바꿀 문자열입니다.</param>
        /// <param name="preserveCase">일치한 원문의 대소문자 모양(전부 대문자/전부 소문자/첫 글자만 대문자)을 따를지 여부입니다.</param>
        /// <returns>바뀐 값입니다. 일치하지 않으면 원래 값을 그대로 반환합니다.</returns>
        public string Replace(string value, string replacement, bool preserveCase)
        {
            if (IsMatch(value) == false)
                return value;

            replacement ??= string.Empty;

            if (IsPlain)
                return ReplacePlain(value, replacement, preserveCase);

            try
            {
                return _regex.Replace(value, match =>
                {
                    string replaced = UseRegex ? match.Result(replacement) : replacement;
                    return preserveCase ? ApplyCase(match.Value, replaced) : replaced;
                });
            }
            catch (RegexMatchTimeoutException)
            {
                return value;
            }
        }

        private string ReplacePlain(string value, string replacement, bool preserveCase)
        {
            StringBuilder builder = new(value.Length);
            int start = 0;
            int index;

            while ((index = value.IndexOf(Keyword, start, _comparison)) >= 0)
            {
                builder.Append(value, start, index - start);

                string matched = value.Substring(index, Keyword.Length);
                builder.Append(preserveCase ? ApplyCase(matched, replacement) : replacement);

                start = index + Keyword.Length;
            }

            builder.Append(value, start, value.Length - start);
            return builder.ToString();
        }

        /// <summary>
        /// 원문의 대소문자 모양을 바꿀 문자열에 입힙니다.
        /// 예: ("CAT", "dog") → "DOG", ("cat", "Dog") → "dog", ("Cat", "dog") → "Dog". 섞인 모양이면 그대로 둡니다.
        /// </summary>
        public static string ApplyCase(string matched, string replacement)
        {
            if (string.IsNullOrEmpty(matched) || string.IsNullOrEmpty(replacement))
                return replacement;

            string upper = matched.ToUpperInvariant();
            string lower = matched.ToLowerInvariant();

            // 글자가 아닌 문자만 있으면 대소문자 모양이 없다.
            if (upper == lower)
                return replacement;

            if (matched == upper)
                return replacement.ToUpperInvariant();

            if (matched == lower)
                return replacement.ToLowerInvariant();

            string rest = matched.Substring(1);
            bool isCapitalized = char.IsUpper(matched[0]) && rest == rest.ToLowerInvariant();

            if (isCapitalized)
                return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1).ToLowerInvariant();

            return replacement;
        }

        /// <summary>
        /// 범위 안의 셀 좌표를 검색 순서대로 나열합니다. 기본은 행 우선(왼→오, 위→아래)이고,
        /// vertical이면 열 우선(위→아래, 왼쪽 열→오른쪽 열)입니다.
        /// </summary>
        public static IEnumerable<CellCoord> EnumerateCells(int firstRow, int firstColumn, int lastRow, int lastColumn, bool vertical)
        {
            if (vertical)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    for (int row = firstRow; row <= lastRow; row++)
                        yield return new CellCoord(row, column);
                }

                yield break;
            }

            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                    yield return new CellCoord(row, column);
            }
        }

        /// <summary>
        /// 검색 순서상 a가 b보다 앞이면 음수, 같으면 0, 뒤면 양수입니다.
        /// </summary>
        public static int CompareOrder(CellCoord a, CellCoord b, bool vertical)
        {
            if (vertical)
                return a.Column != b.Column ? a.Column.CompareTo(b.Column) : a.Row.CompareTo(b.Row);

            return a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column);
        }
    }
}
