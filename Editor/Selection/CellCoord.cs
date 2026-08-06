using System;

namespace NKStudio.TabularEditor.Selection
{
    /// <summary>
    /// 테이블 안의 셀 위치를 나타냅니다. 문서 기준 좌표를 사용합니다.
    /// </summary>
    public readonly struct CellCoord : IEquatable<CellCoord>
    {
        /// <summary>
        /// 행 인덱스입니다.
        /// </summary>
        public readonly int Row;

        /// <summary>
        /// 열 인덱스입니다.
        /// </summary>
        public readonly int Column;

        /// <summary>
        /// 셀 좌표를 생성합니다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        public CellCoord(int row, int column)
        {
            Row = row;
            Column = column;
        }

        /// <summary>
        /// 두 좌표가 같은지 비교합니다.
        /// </summary>
        /// <param name="other">비교할 좌표입니다.</param>
        /// <returns>같으면 true입니다.</returns>
        public bool Equals(CellCoord other)
        {
            return Row == other.Row && Column == other.Column;
        }

        /// <summary>
        /// 두 좌표가 같은지 비교합니다.
        /// </summary>
        /// <param name="obj">비교할 객체입니다.</param>
        /// <returns>같으면 true입니다.</returns>
        public override bool Equals(object obj)
        {
            return obj is CellCoord other && Equals(other);
        }

        /// <summary>
        /// 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>해시 코드입니다.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        /// <summary>
        /// 좌표를 문자열로 표현합니다.
        /// </summary>
        /// <returns>좌표 문자열입니다.</returns>
        public override string ToString()
        {
            return $"({Row}, {Column})";
        }
    }
}
