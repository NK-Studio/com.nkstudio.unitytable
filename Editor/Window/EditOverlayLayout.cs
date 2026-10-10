using System;
using UnityEngine;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 셀 편집 입력칸의 위치와 크기입니다.
    /// </summary>
    public readonly struct EditOverlayPlacement
    {
        public EditOverlayPlacement(Rect rect, bool isWrapped)
        {
            Rect = rect;
            IsWrapped = isWrapped;
        }

        public Rect Rect { get; }

        /// <summary>
        /// 한 줄에 다 들어가지 않아 줄바꿈해서 보여 줘야 하는지 여부입니다.
        /// </summary>
        public bool IsWrapped { get; }
    }

    /// <summary>
    /// SmoothCSV처럼 셀 편집 입력칸이 내용에 따라 커지는 규칙입니다.
    /// 짧으면 셀 폭에 최소 높이(셀보다 넉넉한 높이), 길면 오른쪽으로 표 영역 끝까지 늘어나고,
    /// 그보다 길면 줄바꿈해서 아래로 늘어납니다.
    /// </summary>
    public static class EditOverlayLayout
    {
        /// <param name="cell">편집 중인 셀 영역입니다.</param>
        /// <param name="viewport">입력칸이 넘어가면 안 되는 표의 보이는 영역입니다.</param>
        /// <param name="textWidth">값을 한 줄로 그렸을 때의 글자 폭입니다.</param>
        /// <param name="hasLineBreak">값에 개행 문자가 있는지 여부입니다. 있으면 폭과 상관없이 여러 줄로 보여 준다.</param>
        /// <param name="wrappedHeight">글자 영역 폭을 받아 줄바꿈했을 때의 글자 높이를 돌려주는 함수입니다.</param>
        /// <param name="horizontalPadding">입력칸 좌우 테두리·여백 합입니다.</param>
        /// <param name="verticalPadding">입력칸 위아래 테두리·여백 합입니다.</param>
        /// <param name="minHeight">입력칸의 최소 높이입니다. 셀 높이보다 작으면 셀 높이를 씁니다.</param>
        public static EditOverlayPlacement Compute(
            Rect cell,
            Rect viewport,
            float textWidth,
            bool hasLineBreak,
            Func<float, float> wrappedHeight,
            float horizontalPadding,
            float verticalPadding,
            float minHeight)
        {
            // 셀이 화면 오른쪽 끝에 걸려 남은 폭이 셀보다 좁아도 셀보다 작아지지는 않는다.
            float maxWidth = Math.Max(cell.width, viewport.xMax - cell.xMin);
            float desiredWidth = textWidth + horizontalPadding;
            float width = Math.Clamp(desiredWidth, cell.width, maxWidth);

            bool isWrapped = hasLineBreak || desiredWidth > maxWidth;

            // 높이는 표 영역 전체 높이까지 쓸 수 있다. 셀 높이로 눌러 두면 글자가 테두리·여백에 밀려 위로 스크롤되어 잘린다.
            float maxHeight = Math.Max(cell.height, viewport.height);
            float lowestHeight = Math.Min(Math.Max(cell.height, minHeight), maxHeight);

            float desiredHeight = isWrapped
                ? wrappedHeight(width - horizontalPadding) + verticalPadding
                : lowestHeight;

            float height = Math.Clamp(desiredHeight, lowestHeight, maxHeight);

            // 아래 공간이 모자라면(화면 맨 아래 행 등) 셀 위쪽으로 넓혀 연다. 표 영역 위로는 넘지 않는다.
            float top = cell.yMin;

            if (top + height > viewport.yMax)
                top = Math.Max(viewport.yMin, viewport.yMax - height);

            return new EditOverlayPlacement(new Rect(cell.xMin, top, width, height), isWrapped);
        }
    }
}
