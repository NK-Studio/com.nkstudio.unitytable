using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 열 제목의 ▼ 버튼으로 여는 열 메뉴입니다. 별도 창이 아니라 그리드 컨테이너 안에 뜨는 패널이다.
    /// 메뉴 바깥을 누르면 투명한 뒷판이 그 클릭을 받아 메뉴를 닫는다.
    /// </summary>
    public sealed class TableColumnMenu : IDisposable
    {
        private const string BackdropClassName = "table-editor__column-menu-backdrop";
        private const string PanelClassName = "table-editor__column-menu";
        private const string TitleClassName = "table-editor__column-menu-title";
        private const string ActionRowClassName = "table-editor__column-menu-actions";
        private const string ActionButtonClassName = "table-editor__column-menu-action";

        // 메뉴가 그리드 오른쪽 끝을 넘어 잘리지 않도록 이만큼은 안쪽에 둔다.
        private const float EdgeMargin = 4f;

        private readonly VisualElement _container;
        private readonly VisualElement _backdrop;
        private readonly VisualElement _panel;
        private readonly Button _ascendingButton;
        private readonly Button _descendingButton;

        /// <summary>
        /// 열 메뉴를 만들고 컨테이너에 숨긴 채로 붙입니다.
        /// </summary>
        /// <param name="container">메뉴를 띄울 그리드 컨테이너입니다. 위치 기준이 됩니다.</param>
        public TableColumnMenu(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));

            _backdrop = new VisualElement();
            _backdrop.AddToClassList(BackdropClassName);
            _backdrop.RegisterCallback<PointerDownEvent>(OnBackdropPointerDown);

            _panel = new VisualElement();
            _panel.AddToClassList(PanelClassName);

            Label title = new("정렬");
            title.AddToClassList(TitleClassName);
            _panel.Add(title);

            VisualElement actions = new();
            actions.AddToClassList(ActionRowClassName);

            _ascendingButton = new Button(() => RequestSort(false)) { text = "오름차순 정렬" };
            _ascendingButton.AddToClassList(ActionButtonClassName);
            actions.Add(_ascendingButton);

            _descendingButton = new Button(() => RequestSort(true)) { text = "내림차순 정렬" };
            _descendingButton.AddToClassList(ActionButtonClassName);
            actions.Add(_descendingButton);

            _panel.Add(actions);

            _container.Add(_backdrop);
            _container.Add(_panel);
            SetVisible(false);
        }

        /// <summary>
        /// 메뉴가 열려 있는 열 인덱스입니다. 닫혀 있으면 -1입니다.
        /// </summary>
        public int ColumnIndex { get; private set; } = -1;

        /// <summary>
        /// 메뉴가 열려 있는지 여부입니다.
        /// </summary>
        public bool IsOpen => ColumnIndex >= 0;

        /// <summary>
        /// 정렬 버튼을 눌렀을 때 열 인덱스와 내림차순 여부를 전달합니다.
        /// </summary>
        public event Action<int, bool> SortRequested;

        /// <summary>
        /// 메뉴가 닫혔을 때 호출됩니다.
        /// </summary>
        public event Action Closed;

        /// <summary>
        /// 지정한 열의 메뉴를 열 제목 바로 아래에 엽니다.
        /// </summary>
        /// <param name="columnIndex">메뉴를 열 열 인덱스입니다.</param>
        /// <param name="anchorWorldBound">메뉴를 붙일 열 제목 셀의 월드 좌표 영역입니다.</param>
        public void Open(int columnIndex, Rect anchorWorldBound)
        {
            ColumnIndex = columnIndex;

            Vector2 anchor = _container.WorldToLocal(new Vector2(anchorWorldBound.xMin, anchorWorldBound.yMax));

            _panel.style.left = Math.Max(EdgeMargin, anchor.x);
            _panel.style.top = anchor.y;
            SetVisible(true);

            // 패널 폭은 레이아웃이 끝나야 알 수 있으므로, 오른쪽으로 넘치면 다음 프레임에 안쪽으로 당긴다.
            _panel.schedule.Execute(KeepPanelInsideContainer);
        }

        /// <summary>
        /// 메뉴를 닫습니다. 이미 닫혀 있으면 아무 것도 하지 않습니다.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
                return;

            ColumnIndex = -1;
            SetVisible(false);
            Closed?.Invoke();
        }

        /// <summary>
        /// 등록한 콜백을 해제하고 메뉴 요소를 컨테이너에서 뗍니다.
        /// </summary>
        public void Dispose()
        {
            _backdrop.UnregisterCallback<PointerDownEvent>(OnBackdropPointerDown);
            _backdrop.RemoveFromHierarchy();
            _panel.RemoveFromHierarchy();
        }

        private void RequestSort(bool descending)
        {
            int columnIndex = ColumnIndex;

            Close();
            SortRequested?.Invoke(columnIndex, descending);
        }

        // 뒷판은 포커스를 받지 않는 요소라, 그냥 두면 포커스 컨트롤러가 편집 필드의 포커스를 거둬 간다.
        private void OnBackdropPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            _container.focusController?.IgnoreEvent(evt);
            Close();
        }

        private void KeepPanelInsideContainer()
        {
            float containerWidth = _container.resolvedStyle.width;
            float panelWidth = _panel.resolvedStyle.width;

            if (float.IsNaN(containerWidth) || float.IsNaN(panelWidth))
                return;

            float maxLeft = containerWidth - panelWidth - EdgeMargin;

            if (_panel.resolvedStyle.left > maxLeft)
                _panel.style.left = Math.Max(EdgeMargin, maxLeft);
        }

        private void SetVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _backdrop.style.display = display;
            _panel.style.display = display;
        }
    }
}
