using System;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 셀 하나가 값을 어디서 가져오는지 기록합니다. 셀마다 문자열 객체를 두지 않으려고 8바이트로 압축한다.
    /// <list type="bullet">
    /// <item><c>Start &gt;= 0</c>: 원본 텍스트의 구간. <c>Length &gt; 0</c>이면 그대로 잘라 쓰고,
    /// <c>Length &lt; 0</c>이면 따옴표로 시작한 필드라 <c>-Length</c> 길이의 원본 구간을 해석해야 한다.</item>
    /// <item><c>Start &lt; 0</c>: 편집으로 바뀐 값. 편집 값 목록의 <c>-(Start + 1)</c>번 항목이다.</item>
    /// <item><c>Start == 0, Length == 0</c>: 빈 셀.</item>
    /// </list>
    /// </summary>
    internal readonly struct CellSlot
    {
        public static readonly CellSlot Empty = default;

        private readonly int _start;
        private readonly int _length;

        private CellSlot(int start, int length)
        {
            _start = start;
            _length = length;
        }

        public bool IsEditedValue => _start < 0;

        public int EditedValueIndex => -(_start + 1);

        public bool IsQuoted => _start >= 0 && _length < 0;

        public int TextStart => _start;

        public int RawLength => Math.Abs(_length);

        public static CellSlot FromText(int start, int length, bool isQuoted)
        {
            if (length == 0)
                return Empty;

            return new CellSlot(start, isQuoted ? -length : length);
        }

        public static CellSlot FromEditedValue(int valueIndex)
        {
            return new CellSlot(-(valueIndex + 1), 0);
        }

        /// <summary>
        /// 원본 텍스트에서 셀 값을 만듭니다. 편집 값 슬롯에는 쓸 수 없습니다.
        /// </summary>
        public string ResolveFromText(string text)
        {
            if (_length == 0)
                return string.Empty;

            return IsQuoted
                ? DelimitedTextParser.DecodeQuotedField(text, _start, RawLength)
                : text.Substring(_start, _length);
        }
    }
}
