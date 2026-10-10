using System;
using NKStudio.TabularEditor.Selection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 키보드 입력과 에디터 커맨드를 해석해 그리드 조작과 윈도우 동작으로 연결합니다.
    /// 이동과 저장은 KeyDownEvent로, 복사/붙여넣기 계열은 ValidateCommand/ExecuteCommand로 처리합니다.
    /// </summary>
    public sealed class TableInputRouter : IDisposable
    {
        private const double DuplicateCommandThreshold = 0.05;

        private readonly VisualElement _root;
        private readonly TableGridView _gridView;
        private readonly TableSearchController _searchController;

        private double _lastUndoTime;
        private double _lastRedoTime;
        private double _lastClipboardTime;
        private string _lastClipboardAction = string.Empty;

        /// <summary>
        /// 입력 라우터를 생성하고 콜백을 등록합니다.
        /// </summary>
        /// <param name="root">윈도우의 최상위 VisualElement입니다.</param>
        /// <param name="gridView">조작할 그리드 View입니다.</param>
        /// <param name="searchController">검색 컨트롤러입니다.</param>
        public TableInputRouter(
            VisualElement root,
            TableGridView gridView,
            TableSearchController searchController)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));
            _searchController = searchController;

            // ListView 내부 ScrollView가 방향키를 먼저 소비하므로 반드시 트리클 단계에서 가로챈다.
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<ValidateCommandEvent>(OnValidateCommand, TrickleDown.TrickleDown);
            _root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// 대화상자 같은 모달 오버레이가 열려 있는지 여부입니다. 열려 있으면 키·명령을 그리드로 보내지 않고 오버레이에 맡긴다.
        /// </summary>
        public bool IsModalOpen { get; set; }

        /// <summary>
        /// 저장이 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action SaveRequested;

        /// <summary>
        /// 되돌리기가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action UndoRequested;

        /// <summary>
        /// 다시 실행이 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action RedoRequested;

        /// <summary>
        /// 선택 범위 복사가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action CopyRequested;

        /// <summary>
        /// 선택 범위 잘라내기가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action CutRequested;

        /// <summary>
        /// 붙여넣기가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action PasteRequested;

        /// <summary>
        /// Delete 키 동작이 요청되었을 때 호출됩니다.
        /// 선택 종류에 따라 행/열 삭제 또는 내용 비우기로 해석됩니다.
        /// </summary>
        public event Action DeleteRequested;

        /// <summary>
        /// 검색 열기가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action SearchOpenRequested;

        /// <summary>
        /// 행으로 이동(Ctrl/Cmd+G) 입력칸 열기가 요청되었을 때 호출됩니다.
        /// </summary>
        public event Action GoToRequested;

        /// <summary>
        /// 등록한 콜백을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _root.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand, TrickleDown.TrickleDown);
            _root.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand, TrickleDown.TrickleDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (IsModalOpen)
                return;

            if (_gridView.IsEditing)
            {
                HandleEditingKey(evt);
                return;
            }

            if (_searchController != null && _searchController.ContainsFocus(GetFocusedElement()))
            {
                HandleSearchKey(evt);
                return;
            }

            HandleNavigationKey(evt);
        }

        private void HandleEditingKey(KeyDownEvent evt)
        {
            // 행·열 추가는 입력 중에도 바로 쓸 수 있게, 입력을 확정한 뒤 실행한다.
            if (evt.actionKey && evt.altKey && TryInsertByArrow(evt))
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (evt.actionKey)
                    {
                        _gridView.CommitEditToSelection();
                        Consume(evt);
                        return;
                    }

                    _gridView.CommitEdit();
                    _gridView.MoveActiveCell(evt.shiftKey ? -1 : 1, 0, false);
                    Consume(evt);
                    return;

                case KeyCode.Escape:
                    _gridView.CancelEdit();
                    Consume(evt);
                    return;

                case KeyCode.Tab:
                    _gridView.CommitEdit();
                    _gridView.MoveActiveCellWithWrap(evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.UpArrow:
                    CommitAndMove(evt, -1, 0);
                    return;

                case KeyCode.DownArrow:
                    CommitAndMove(evt, 1, 0);
                    return;

                case KeyCode.LeftArrow:
                    CommitAndMove(evt, 0, -1);
                    return;

                case KeyCode.RightArrow:
                    CommitAndMove(evt, 0, 1);
                    return;
            }

            // 그 밖의 키는 편집 중인 TextField가 그대로 처리한다.
        }

        // 타이핑으로 시작한 편집이면 방향키를 입력 완료로 보고 셀을 옮긴다.
        // F2나 더블클릭으로 연 편집에서는 방향키가 캐럿 이동이어야 하므로 그대로 넘긴다.
        private void CommitAndMove(KeyDownEvent evt, int rowDelta, int columnDelta)
        {
            if (!_gridView.IsTypingEntry)
                return;

            _gridView.CommitEdit();
            _gridView.MoveActiveCell(rowDelta, columnDelta, false);
            Consume(evt);
        }

        private void HandleSearchKey(KeyDownEvent evt)
        {
            if (_searchController == null)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    _searchController.Close();
                    Consume(evt);
                    return;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.F3:
                    bool isEnter = evt.keyCode != KeyCode.F3;

                    // 바꾸기 칸에서 Enter는 바꾸기, Ctrl/Cmd+Enter는 모두 바꾸기다.
                    if (isEnter && _searchController.IsReplaceFieldFocused(GetFocusedElement()))
                    {
                        if (evt.actionKey)
                            _searchController.ReplaceAll();
                        else
                            _searchController.ReplaceCurrent();

                        Consume(evt);
                        return;
                    }

                    if (evt.shiftKey)
                        _searchController.SelectPrevious();
                    else
                        _searchController.SelectNext();

                    Consume(evt);
                    return;
            }
        }

        private void HandleNavigationKey(KeyDownEvent evt)
        {
            if (evt.actionKey && HandleActionKey(evt))
                return;

            if (evt.altKey && evt.actionKey == false && HandleMoveKey(evt))
                return;

            if (evt.actionKey || evt.altKey)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    _gridView.MoveActiveCell(-1, 0, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.DownArrow:
                    _gridView.MoveActiveCell(1, 0, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.LeftArrow:
                    _gridView.MoveActiveCell(0, -1, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.RightArrow:
                    _gridView.MoveActiveCell(0, 1, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.Tab:
                    _gridView.MoveActiveCellWithWrap(evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    _gridView.MoveActiveCell(evt.shiftKey ? -1 : 1, 0, false);
                    Consume(evt);
                    return;

                case KeyCode.Home:
                    _gridView.SetActiveCell(
                        new CellCoord(GetHomeEndRow(evt.shiftKey), 0),
                        evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.End:
                    _gridView.SetActiveCell(
                        new CellCoord(GetHomeEndRow(evt.shiftKey), _gridView.MaxColumn),
                        evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.PageUp:
                    _gridView.MoveActiveCell(-_gridView.VisibleRowCount, 0, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.PageDown:
                    _gridView.MoveActiveCell(_gridView.VisibleRowCount, 0, evt.shiftKey);
                    Consume(evt);
                    return;

                case KeyCode.F2:
                    Consume(evt);
                    _gridView.BeginEdit(null);
                    return;

                case KeyCode.F3:
                    if (_searchController != null)
                    {
                        if (evt.shiftKey)
                            _searchController.SelectPrevious();
                        else
                            _searchController.SelectNext();

                        Consume(evt);
                    }

                    return;

                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteRequested?.Invoke();
                    Consume(evt);
                    return;

                case KeyCode.Escape:
                    // 열 메뉴가 떠 있으면 그것부터 닫는다. 한 번 더 누르면 검색을 닫는다.
                    if (_gridView.IsColumnMenuOpen)
                    {
                        _gridView.CloseColumnMenu();
                        Consume(evt);
                        return;
                    }

                    if (_searchController != null && _searchController.IsOpen)
                    {
                        _searchController.Close();
                        Consume(evt);
                    }

                    return;
            }

            // 문자 입력은 가로채지 않는다. 편집 필드가 항상 활성 셀 위에서 포커스를 유지하므로
            // 타건이 그대로 필드에 도달하고, 한글 IME 조합도 첫 자모부터 정상 동작한다.
            // 편집 진입은 필드의 ChangeEvent에서 감지한다.
        }

        // Ctrl/Cmd+Alt/Option+방향키: 그 방향에 행·열을 추가한다.
        private bool TryInsertByArrow(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    _gridView.InsertRowsBelow();
                    break;

                case KeyCode.UpArrow:
                    _gridView.InsertRowsAbove();
                    break;

                case KeyCode.RightArrow:
                    _gridView.InsertColumnsRight();
                    break;

                case KeyCode.LeftArrow:
                    _gridView.InsertColumnsLeft();
                    break;

                default:
                    return false;
            }

            Consume(evt);
            return true;
        }

        // Alt/Option+방향키: 선택한 행·열을 한 칸 옮긴다.
        private bool HandleMoveKey(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    _gridView.MoveSelectedRows(-1);
                    break;

                case KeyCode.DownArrow:
                    _gridView.MoveSelectedRows(1);
                    break;

                case KeyCode.LeftArrow:
                    _gridView.MoveSelectedColumns(-1);
                    break;

                case KeyCode.RightArrow:
                    _gridView.MoveSelectedColumns(1);
                    break;

                default:
                    return false;
            }

            Consume(evt);
            return true;
        }

        private bool HandleActionKey(KeyDownEvent evt)
        {
            if (evt.altKey)
                return TryInsertByArrow(evt);

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    _gridView.MoveToDataEdge(-1, 0, evt.shiftKey);
                    Consume(evt);
                    return true;

                case KeyCode.DownArrow:
                    _gridView.MoveToDataEdge(1, 0, evt.shiftKey);
                    Consume(evt);
                    return true;

                case KeyCode.LeftArrow:
                    _gridView.MoveToDataEdge(0, -1, evt.shiftKey);
                    Consume(evt);
                    return true;

                case KeyCode.RightArrow:
                    _gridView.MoveToDataEdge(0, 1, evt.shiftKey);
                    Consume(evt);
                    return true;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    _gridView.DeleteSelectedRowsOrColumns();
                    Consume(evt);
                    return true;

                case KeyCode.D:
                    // Unity 전역 Duplicate가 씬 오브젝트를 복제하지 않도록 여기서 반드시 소비한다.
                    // 같은 입력이 "Duplicate" 명령으로도 올 수 있어 복사·붙여넣기와 같은 중복 방지를 거친다.
                    if (ShouldRunClipboard("Duplicate"))
                        _gridView.DuplicateSelectedRows();
                    Consume(evt);
                    return true;

                case KeyCode.S:
                    SaveRequested?.Invoke();
                    Consume(evt);
                    return true;

                case KeyCode.F:
                    SearchOpenRequested?.Invoke();
                    Consume(evt);
                    return true;

                case KeyCode.G:
                    GoToRequested?.Invoke();
                    Consume(evt);
                    return true;

                case KeyCode.Z:
                    // Unity 전역 Undo가 씬을 되돌리지 못하도록 여기서 반드시 소비한다.
                    if (evt.shiftKey)
                        RequestRedo();
                    else
                        RequestUndo();

                    Consume(evt);
                    return true;

                case KeyCode.Y:
                    RequestRedo();
                    Consume(evt);
                    return true;

                // 편집 필드가 항상 포커스를 갖고 있어 ValidateCommand/ExecuteCommand가
                // 우리에게 오지 않을 수 있다. 키 조합으로도 받아 두 경로 모두 커버한다.
                case KeyCode.C:
                    if (ShouldRunClipboard("Copy"))
                        CopyRequested?.Invoke();

                    Consume(evt);
                    return true;

                case KeyCode.X:
                    if (ShouldRunClipboard("Cut"))
                        CutRequested?.Invoke();

                    Consume(evt);
                    return true;

                case KeyCode.V:
                    if (ShouldRunClipboard("Paste"))
                        PasteRequested?.Invoke();

                    Consume(evt);
                    return true;

                case KeyCode.A:
                    if (ShouldRunClipboard("SelectAll"))
                        _gridView.SelectAll();

                    Consume(evt);
                    return true;

                case KeyCode.Home:
                    _gridView.SetActiveCell(new CellCoord(0, 0), evt.shiftKey);
                    Consume(evt);
                    return true;

                case KeyCode.End:
                    _gridView.SetActiveCell(
                        new CellCoord(_gridView.MaxRow, _gridView.MaxColumn),
                        evt.shiftKey);
                    Consume(evt);
                    return true;
            }

            return false;
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (IsModalOpen || _gridView.IsEditing || !IsHandledCommand(evt.commandName))
                return;

            evt.StopPropagation();
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (IsModalOpen || _gridView.IsEditing || !IsHandledCommand(evt.commandName))
                return;

            switch (evt.commandName)
            {
                case "Copy":
                    if (ShouldRunClipboard("Copy"))
                        CopyRequested?.Invoke();

                    break;

                case "Cut":
                    if (ShouldRunClipboard("Cut"))
                        CutRequested?.Invoke();

                    break;

                case "Paste":
                    if (ShouldRunClipboard("Paste"))
                        PasteRequested?.Invoke();

                    break;

                case "SelectAll":
                    if (ShouldRunClipboard("SelectAll"))
                        _gridView.SelectAll();

                    break;

                case "Duplicate":
                    if (ShouldRunClipboard("Duplicate"))
                        _gridView.DuplicateSelectedRows();

                    break;

                case "Delete":
                case "SoftDelete":
                    DeleteRequested?.Invoke();
                    break;

                case "Undo":
                    RequestUndo();
                    break;

                case "Redo":
                    RequestRedo();
                    break;

                case "Find":
                    SearchOpenRequested?.Invoke();
                    break;
            }

            evt.StopPropagation();
        }

        private static bool IsHandledCommand(string commandName)
        {
            return commandName is "Copy"
                or "Cut"
                or "Paste"
                or "SelectAll"
                or "Duplicate"
                or "Delete"
                or "SoftDelete"
                or "Undo"
                or "Redo"
                or "Find";
        }

        // 같은 입력이 KeyDownEvent와 ExecuteCommandEvent 양쪽으로 도착할 수 있어 중복 실행을 막는다.
        private bool ShouldRunClipboard(string action)
        {
            double now = EditorApplication.timeSinceStartup;

            if (_lastClipboardAction == action && now - _lastClipboardTime < DuplicateCommandThreshold)
                return false;

            _lastClipboardAction = action;
            _lastClipboardTime = now;

            return true;
        }

        // KeyDownEvent와 ExecuteCommandEvent가 같은 입력에 대해 둘 다 도착할 수 있어 중복 실행을 막는다.
        private void RequestUndo()
        {
            double now = EditorApplication.timeSinceStartup;

            if (now - _lastUndoTime < DuplicateCommandThreshold)
                return;

            _lastUndoTime = now;
            UndoRequested?.Invoke();
        }

        private void RequestRedo()
        {
            double now = EditorApplication.timeSinceStartup;

            if (now - _lastRedoTime < DuplicateCommandThreshold)
                return;

            _lastRedoTime = now;
            RedoRequested?.Invoke();
        }

        private VisualElement GetFocusedElement()
        {
            return _root.focusController?.focusedElement as VisualElement;
        }

        // Shift로 넓힐 때는 범위의 움직이는 끝 행을, 그냥 이동할 때는 활성 셀(범위 시작점) 행을 따른다.
        private int GetHomeEndRow(bool extendSelection)
        {
            CellSelection selection = _gridView.Selection;
            return extendSelection ? selection.Focus.Row : selection.Anchor.Row;
        }

        // Tab처럼 포커스를 옮기는 키를 가로챌 때는 StopPropagation만으로는 부족해 IgnoreEvent를 함께 호출한다.
        private void Consume(KeyDownEvent evt)
        {
            evt.StopPropagation();
            _root.focusController?.IgnoreEvent(evt);
        }
    }
}
