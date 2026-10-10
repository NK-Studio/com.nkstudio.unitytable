using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 열 폭을 내용에 맞출 때 실제로 글자 폭을 잴 행을 고릅니다.
    /// 모든 셀을 재면 큰 파일에서 느리므로, 글자 수가 긴 행 몇 개만 후보로 남겨 그것만 잽니다.
    /// 비례 폰트라 글자 수가 가장 긴 값이 꼭 가장 넓지는 않아서, 하나가 아니라 여러 개를 남깁니다.
    /// </summary>
    public static class ColumnFitCandidates
    {
        /// <summary>
        /// 글자 수가 긴 순으로 최대 count개의 행 번호를 고릅니다. 빈 셀은 후보가 아닙니다.
        /// </summary>
        /// <param name="lengthOfRow">행 번호를 받아 그 셀의 글자 수를 돌려주는 함수입니다.</param>
        /// <param name="firstRow">첫 행(포함)입니다.</param>
        /// <param name="lastRow">마지막 행(포함)입니다.</param>
        /// <param name="count">남길 후보 수입니다.</param>
        /// <returns>후보 행 번호입니다. 순서는 정해져 있지 않습니다.</returns>
        public static List<int> SelectLongestRows(Func<int, int> lengthOfRow, int firstRow, int lastRow, int count)
        {
            List<int> rows = new(count);
            List<int> lengths = new(count);

            if (count <= 0)
                return rows;

            for (int row = firstRow; row <= lastRow; row++)
            {
                int length = lengthOfRow(row);

                if (length <= 0)
                    continue;

                if (rows.Count < count)
                {
                    rows.Add(row);
                    lengths.Add(length);
                    continue;
                }

                int shortestIndex = IndexOfShortest(lengths);

                if (length <= lengths[shortestIndex])
                    continue;

                rows[shortestIndex] = row;
                lengths[shortestIndex] = length;
            }

            return rows;
        }

        private static int IndexOfShortest(List<int> lengths)
        {
            int shortestIndex = 0;

            for (int index = 1; index < lengths.Count; index++)
            {
                if (lengths[index] < lengths[shortestIndex])
                    shortestIndex = index;
            }

            return shortestIndex;
        }
    }
}
