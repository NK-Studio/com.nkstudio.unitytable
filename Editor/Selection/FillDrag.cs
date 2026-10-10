using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Selection
{
    public enum FillDirection
    {
        Down,
        Up,
        Right,
        Left,
    }

    /// <summary>
    /// 채우기 핸들로 늘린 범위입니다. 행·열 번호는 모두 문서 기준(0부터)이고 양 끝을 포함합니다.
    /// </summary>
    public readonly struct FillTarget
    {
        public FillTarget(FillDirection direction, int count, int firstRow, int firstColumn, int lastRow, int lastColumn)
        {
            Direction = direction;
            Count = count;
            FirstRow = firstRow;
            FirstColumn = firstColumn;
            LastRow = lastRow;
            LastColumn = lastColumn;
        }

        public FillDirection Direction { get; }

        /// <summary>
        /// 새로 채울 행(위·아래) 또는 열(왼쪽·오른쪽) 수입니다.
        /// </summary>
        public int Count { get; }

        // 원본과 새로 채울 칸을 합친 범위다. 채운 뒤 이 범위를 선택한다.
        public int FirstRow { get; }

        public int FirstColumn { get; }

        public int LastRow { get; }

        public int LastColumn { get; }

        public bool IsVertical => Direction == FillDirection.Down || Direction == FillDirection.Up;
    }

    /// <summary>
    /// 채우기 핸들 끌기의 규칙입니다.
    /// <list type="bullet">
    /// <item>포인터가 선택 범위 밖으로 더 많이 나간 축(세로/가로) 하나로만 늘린다.</item>
    /// <item>범위 안쪽으로 끌면 아무것도 하지 않는다.</item>
    /// <item>본문에서 시작했으면 위로 늘려도 고정 헤더 행까지는 들어가지 않는다.</item>
    /// </list>
    /// </summary>
    public static class FillDrag
    {
        public static bool TryGetTarget(
            int firstRow,
            int firstColumn,
            int lastRow,
            int lastColumn,
            CellCoord pointer,
            int headerRowCount,
            out FillTarget target)
        {
            target = default;

            int rowOverflow = pointer.Row > lastRow ? pointer.Row - lastRow : pointer.Row < firstRow ? firstRow - pointer.Row : 0;
            int columnOverflow = pointer.Column > lastColumn ? pointer.Column - lastColumn : pointer.Column < firstColumn ? firstColumn - pointer.Column : 0;

            if (rowOverflow == 0 && columnOverflow == 0)
                return false;

            if (rowOverflow >= columnOverflow)
            {
                if (pointer.Row > lastRow)
                {
                    target = new FillTarget(FillDirection.Down, rowOverflow, firstRow, firstColumn, pointer.Row, lastColumn);
                    return true;
                }

                int topLimit = firstRow >= headerRowCount ? headerRowCount : 0;
                int top = Math.Max(pointer.Row, topLimit);

                if (top >= firstRow)
                    return false;

                target = new FillTarget(FillDirection.Up, firstRow - top, top, firstColumn, lastRow, lastColumn);
                return true;
            }

            if (pointer.Column > lastColumn)
            {
                target = new FillTarget(FillDirection.Right, columnOverflow, firstRow, firstColumn, lastRow, pointer.Column);
                return true;
            }

            target = new FillTarget(FillDirection.Left, columnOverflow, firstRow, pointer.Column, lastRow, lastColumn);
            return true;
        }

        /// <summary>
        /// 새로 채울 칸의 값을 만듭니다. 돌려준 배열은 (originRow, originColumn)부터의 직사각형이다.
        /// 원본 범위의 열(위·아래로 채울 때) 또는 행(왼쪽·오른쪽으로 채울 때)마다 따로 <see cref="FillSeries"/>를 적용한다.
        /// </summary>
        public static string[][] BuildValues(
            TableDocument document,
            int firstRow,
            int firstColumn,
            int lastRow,
            int lastColumn,
            FillTarget target,
            bool toggle,
            out int originRow,
            out int originColumn)
        {
            bool backward = target.Direction == FillDirection.Up || target.Direction == FillDirection.Left;

            originRow = target.Direction == FillDirection.Down ? lastRow + 1 : target.FirstRow;
            originColumn = target.Direction == FillDirection.Right ? lastColumn + 1 : target.FirstColumn;

            int height = target.IsVertical ? target.Count : lastRow - firstRow + 1;
            int width = target.IsVertical ? lastColumn - firstColumn + 1 : target.Count;
            string[][] values = new string[height][];

            for (int row = 0; row < height; row++)
                values[row] = new string[width];

            int lineCount = target.IsVertical ? width : height;

            for (int line = 0; line < lineCount; line++)
            {
                // 채우는 방향 순서로 원본을 늘어놓는다. 위·왼쪽으로 채울 때는 맞닿은 칸(맨 위·맨 왼쪽)이 마지막에 오도록 거꾸로다.
                List<string> source = new();
                int sourceLength = target.IsVertical ? lastRow - firstRow + 1 : lastColumn - firstColumn + 1;

                for (int offset = 0; offset < sourceLength; offset++)
                {
                    int step = backward ? sourceLength - 1 - offset : offset;
                    source.Add(target.IsVertical
                        ? document.GetCell(firstRow + step, firstColumn + line)
                        : document.GetCell(firstRow + line, firstColumn + step));
                }

                string[] filled = FillSeries.Extend(source, target.Count, toggle, backward);

                for (int index = 0; index < filled.Length; index++)
                {
                    // filled[0]은 원본에 맞닿은 칸이다. 위·왼쪽으로 채울 때 배열에서는 맨 아래·맨 오른쪽이다.
                    int position = backward ? filled.Length - 1 - index : index;

                    if (target.IsVertical)
                        values[position][line] = filled[index];
                    else
                        values[line][position] = filled[index];
                }
            }

            return values;
        }
    }
}
