using System;
using NKStudio.TabularEditor.Selection;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// Ctrl/Cmd+G로 여는 '행으로 이동' 입력칸입니다(VS Code의 Go to Line처럼 표 위쪽 가운데에 뜬다).
    /// 입력 해석은 <see cref="GoToTarget"/>이 맡는다. 바깥을 누르거나 Esc를 누르면 닫힌다.
    /// </summary>
    public sealed class TableGoToBar : IDisposable
    {
        private const string BackdropClassName = "table-editor__column-menu-backdrop";
        private const string PanelClassName = "table-editor__goto";
        private const string FieldClassName = "table-editor__goto-field";
        private const string InvalidClassName = "table-editor__goto-field--invalid";

        private readonly VisualElement _container;
        private readonly VisualElement _backdrop;
        private readonly VisualElement _panel;
        private readonly TextField _field;

        private int _rowCount;
        private int _columnCount;
        private int _currentColumn;

        /// <summary>
        /// 입력칸을 만들어 컨테이너 위에 숨겨 둡니다.
        /// </summary>
        public TableGoToBar(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));

            _backdrop = new VisualElement();
            _backdrop.AddToClassList(BackdropClassName);
            _backdrop.RegisterCallback<PointerDownEvent>(OnBackdropPointerDown);

            _panel = new VisualElement();
            _panel.AddToClassList(PanelClassName);

            _field = new TextField();
            _field.AddToClassList(FieldClassName);
            _field.textEdition.hidePlaceholderOnFocus = false;
            _field.RegisterValueChangedCallback(_ => _field.RemoveFromClassList(InvalidClassName));

            // 트리클로 받아야 TextField가 Enter·Esc를 먼저 처리(포커스 해제 등)하기 전에 가로챌 수 있다.
            _field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _panel.Add(_field);

            _container.Add(_backdrop);
            _container.Add(_panel);
            SetVisible(false);
        }

        /// <summary>
        /// 입력한 셀로 이동하라고 할 때 호출됩니다.
        /// </summary>
        public event Action<CellCoord> Submitted;

        /// <summary>
        /// 닫혔을 때 호출됩니다. 이동한 뒤에도 호출됩니다.
        /// </summary>
        public event Action Closed;

        public bool IsOpen => _panel.style.display == DisplayStyle.Flex;

        /// <summary>
        /// 표 크기와 지금 열을 받아 엽니다. 열을 적지 않으면 지금 열에 머문다.
        /// </summary>
        public void Open(int rowCount, int columnCount, int currentColumn)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _currentColumn = currentColumn;

            _field.SetValueWithoutNotify(string.Empty);
            _field.RemoveFromClassList(InvalidClassName);
            _field.textEdition.placeholder = Localization.Get("goto.placeholder");

            // TRAP: 표는 숨은 편집칸이 늘 포커스를 갖고, 거기 글자가 들어오면 바로 셀 편집이 시작된다.
            // 이 입력칸은 보이게 한 다음 프레임에야 포커스를 받을 수 있으므로, 그 사이 빠르게 친 글자가 셀로 들어가지 않게 먼저 포커스를 뺀다.
            (_container.focusController?.focusedElement as VisualElement)?.Blur();

            SetVisible(true);
            _field.schedule.Execute(() => _field.Focus()).ExecuteLater(0);
        }

        public void Close()
        {
            if (IsOpen == false)
                return;

            SetVisible(false);
            Closed?.Invoke();
        }

        public void Dispose()
        {
            _backdrop.UnregisterCallback<PointerDownEvent>(OnBackdropPointerDown);
            _field.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _backdrop.RemoveFromHierarchy();
            _panel.RemoveFromHierarchy();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Consume(evt);
                    Submit();
                    return;

                case KeyCode.Escape:
                    Consume(evt);
                    Close();
                    return;
            }
        }

        // 해석할 수 없는 입력이면 닫지 않고 입력칸을 빨갛게 표시한다. 다시 타이핑하면 표시가 사라진다.
        private void Submit()
        {
            if (GoToTarget.TryParse(_field.value, _rowCount, _columnCount, _currentColumn, out CellCoord target) == false)
            {
                _field.AddToClassList(InvalidClassName);
                return;
            }

            Close();
            Submitted?.Invoke(target);
        }

        private void OnBackdropPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            _container.focusController?.IgnoreEvent(evt);
            Close();
        }

        private void Consume(KeyDownEvent evt)
        {
            evt.StopPropagation();
            _container.focusController?.IgnoreEvent(evt);
        }

        private void SetVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _backdrop.style.display = display;
            _panel.style.display = display;
        }
    }
}
