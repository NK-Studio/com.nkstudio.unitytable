using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Selection;

namespace NKStudio.TabularEditor.AssetLinks
{
    /// <summary>
    /// 에셋 경로 열에서 에셋을 찾을 수 없는 칸을 가리고, 그 칸들 사이를 오가는 규칙입니다.
    /// </summary>
    public static class MissingAssets
    {
        /// <summary>
        /// 찾지 못했을 때 '깨진 경로'로 볼 값인지 확인합니다. 경로 구분자(/)가 있는 값만 그렇게 본다.
        /// 헤더 행을 지정하지 않은 파일의 열 제목(IconPath)이나 None·- 같은 자리표시 값이 경고로 뜨지 않게 하는 기준이다.
        /// </summary>
        public static bool LooksLikePath(string value)
        {
            return string.IsNullOrWhiteSpace(value) == false && value.IndexOf('/') >= 0;
        }

        /// <summary>
        /// from 다음(행 우선: 같은 행의 오른쪽, 그다음 아래 행)에 오는 문제 칸을 돌려줍니다. 마지막을 지나면 처음으로 돌아간다.
        /// </summary>
        /// <param name="missing">행 우선 순서로 정렬된 문제 칸 목록입니다.</param>
        public static bool TryFindNext(IReadOnlyList<CellCoord> missing, CellCoord from, out CellCoord next)
        {
            next = default;

            if (missing == null || missing.Count == 0)
                return false;

            foreach (CellCoord candidate in missing)
            {
                bool isAfter = candidate.Row > from.Row || (candidate.Row == from.Row && candidate.Column > from.Column);

                if (isAfter == false)
                    continue;

                next = candidate;
                return true;
            }

            next = missing[0];
            return true;
        }
    }
}
