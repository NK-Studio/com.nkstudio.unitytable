using System;
using System.Collections.Generic;
using System.Text;
using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// MultiColumnListView로 테이블을 표시하고 셀 선택과 셀 편집을 담당하는 View입니다.
    /// 문서 변경은 직접 수행하지 않고 CommandRequested 이벤트로 위임합니다.
    /// </summary>
    public sealed class TableGridView : IDisposable
    {
        private const string CellClassName = "table-editor__cell";
        private const string CellLabelClassName = "table-editor__cell-label";
        private const string SelectedClassName = "table-editor__cell--selected";
        private const string ActiveClassName = "table-editor__cell--active";
        private const string MatchClassName = "table-editor__cell--match";
        private const string EditingClassName = "table-editor__cell--editing";
        private const string RowNumberClassName = "table-editor__row-number";
        private const string RowNumberSelectedClassName = "table-editor__row-number--selected";
        private const string ColumnHeaderSelectedClassName = "table-editor__column-header--selected";
        private const string UnityColumnHeaderContainerClassName = "unity-multi-column-header";
        private const string UnityColumnHeaderClassName = "unity-multi-column-header__column";
        private const string RowNumberLabelClassName = "table-editor__row-number-label";
        private const string EditFieldEditingClassName = "table-editor__edit-field--editing";

        private const float DefaultColumnWidth = 160f;
        private const float RowNumberColumnWidth = 46f;
        private const float RowHeight = 20f;
        private const float ColumnResizeEdgeWidth = 4f;

        private readonly VisualElement _container;
        private readonly MultiColumnListView _listView;
        private readonly TextField _editField;
        private readonly List<int> _itemIndices = new();
        private readonly List<float> _columnWidths = new();

        private TableDocument _document;
        private ScrollView _scrollView;
        private VisualElement _columnHeaderContainer;
        private ContextualMenuManipulator _columnHeaderManipulator;
        private int _pressedColumnIndex = -1;
        private HashSet<CellCoord> _matches;
        private bool _isDragSelecting;
        private bool _isEditing;
        private bool _isTypingEntry;
        private bool _suppressEditCommit;
        private CellCoord _editingCoord;
        private string _editOriginalValue = string.Empty;

        /// <summary>
        /// 그리드 View를 생성하고 컨테이너에 배치합니다.
        /// </summary>
        /// <param name="container">그리드를 배치할 컨테이너입니다.</param>
        public TableGridView(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));

            Selection = new CellSelection();
            Selection.Changed += OnSelectionChanged;

            _listView = new MultiColumnListView();
            _listView.AddToClassList("table-editor__grid");
            _listView.selectionType = SelectionType.None;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _listView.fixedItemHeight = RowHeight;
            _listView.showBorder = false;
            _listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            _listView.horizontalScrollingEnabled = true;
            _listView.focusable = true;
            _listView.itemsSource = _itemIndices;
            _listView.columns.reorderable = false;
            _container.Add(_listView);

            _editField = new TextField();
            _editField.AddToClassList("table-editor__edit-field");
            _editField.isDelayed = false;

            // 포커스 시 자동 전체 선택을 끈다. 켜져 있으면 Focus() 뒤에 적용되어
            // 우리가 잡아 둔 커서 위치를 덮어쓰고, 다음 타이핑이 내용을 통째로 갈아치운다.
            // 전체 선택이 필요한 F2/더블클릭 경로에서는 SelectAll()을 직접 호출한다.
            _editField.selectAllOnFocus = false;
            _editField.selectAllOnMouseUp = false;
            _editField.RegisterCallback<FocusOutEvent>(OnEditFieldFocusOut);
            _editField.RegisterCallback<ChangeEvent<string>>(OnEditFieldValueChanged);
            _container.Add(_editField);
            SetEditFieldEditing(false);

            _listView.RegisterCallback<GeometryChangedEvent>(OnListGeometryChanged);

            // 헤더 요소가 포인터를 캡처해 전파를 끊더라도 놓치지 않도록 ListView에서 트리클로 먼저 받는다.
            _listView.RegisterCallback<PointerDownEvent>(OnGridPointerDown, TrickleDown.TrickleDown);
            _listView.RegisterCallback<PointerMoveEvent>(OnGridPointerMove, TrickleDown.TrickleDown);
            _listView.RegisterCallback<PointerUpEvent>(OnGridPointerUp, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// 활성 셀과 선택 범위입니다. 좌표는 항상 문서 기준입니다.
        /// </summary>
        public CellSelection Selection { get; }

        /// <summary>
        /// 현재 표시 중인 문서입니다.
        /// </summary>
        public TableDocument Document => _document;

        /// <summary>
        /// 첫 행을 열 제목으로 사용할지 여부입니다.
        /// </summary>
        public bool UseFirstRowAsHeader { get; private set; } = true;

        /// <summary>
        /// 편집 가능한 첫 행 인덱스입니다. 헤더 사용 시 1입니다.
        /// </summary>
        public int MinRow => UseFirstRowAsHeader && _document != null && _document.RowCount > 1 ? 1 : 0;

        /// <summary>
        /// 편집 가능한 마지막 행 인덱스입니다.
        /// </summary>
        public int MaxRow => _document == null ? 0 : Math.Max(MinRow, _document.RowCount - 1);

        /// <summary>
        /// 마지막 열 인덱스입니다.
        /// </summary>
        public int MaxColumn => _document == null ? 0 : Math.Max(0, _document.ColumnCount - 1);

        /// <summary>
        /// 현재 셀 편집 중인지 여부입니다.
        /// </summary>
        public bool IsEditing => _isEditing;

        /// <summary>
        /// 문자를 입력해 편집을 시작했는지 여부입니다.
        /// F2나 더블클릭으로 연 편집(false)에서는 방향키가 캐럿을 움직입니다.
        /// </summary>
        public bool IsTypingEntry => _isTypingEntry;

        /// <summary>
        /// 한 화면에 보이는 행 개수입니다. PageUp/PageDown 이동에 사용합니다.
        /// </summary>
        public int VisibleRowCount
        {
            get
            {
                float height = _listView.resolvedStyle.height;

                if (float.IsNaN(height) || height <= 0f)
                    return 10;

                return Math.Max(1, Mathf.FloorToInt(height / RowHeight) - 1);
            }
        }

        /// <summary>
        /// 문서를 변경해야 할 때 실행할 작업을 전달합니다.
        /// </summary>
        public event Action<ITableCommand> CommandRequested;

        /// <summary>
        /// 컨텍스트 메뉴에서 복사가 선택되었을 때 호출됩니다.
        /// </summary>
        public event Action CopyRequested;

        /// <summary>
        /// 컨텍스트 메뉴에서 잘라내기가 선택되었을 때 호출됩니다.
        /// </summary>
        public event Action CutRequested;

        /// <summary>
        /// 컨텍스트 메뉴에서 붙여넣기가 선택되었을 때 호출됩니다.
        /// </summary>
        public event Action PasteRequested;

        /// <summary>
        /// 컨텍스트 메뉴에서 내용 지우기가 선택되었을 때 호출됩니다.
        /// </summary>
        public event Action ClearRequested;

        /// <summary>
        /// 표시할 문서를 설정하고 그리드를 다시 만듭니다.
        /// </summary>
        /// <param name="document">표시할 문서입니다.</param>
        public void SetDocument(TableDocument document)
        {
            if (_document != null)
            {
                _document.CellChanged -= OnDocumentCellChanged;
                _document.StructureChanged -= OnDocumentStructureChanged;
            }

            _document = document;
            _columnWidths.Clear();

            if (_document != null)
            {
                _document.CellChanged += OnDocumentCellChanged;
                _document.StructureChanged += OnDocumentStructureChanged;
            }

            CancelEdit();
            RebuildColumns();
            RebuildItems();
            Selection.SetActive(new CellCoord(MinRow, 0));
        }

        /// <summary>
        /// 첫 행을 헤더로 사용할지 설정합니다.
        /// </summary>
        /// <param name="useHeader">헤더로 사용하면 true입니다.</param>
        public void SetUseFirstRowAsHeader(bool useHeader)
        {
            if (UseFirstRowAsHeader == useHeader)
                return;

            UseFirstRowAsHeader = useHeader;
            CancelEdit();
            RebuildColumns();
            RebuildItems();
            Selection.Clamp(MinRow, _document?.RowCount ?? 1, _document?.ColumnCount ?? 1);
        }

        /// <summary>
        /// 검색 일치 셀 목록을 설정해 강조 표시를 갱신합니다.
        /// </summary>
        /// <param name="matches">강조할 셀 좌표 집합입니다. null이면 강조를 지웁니다.</param>
        public void SetSearchMatches(HashSet<CellCoord> matches)
        {
            _matches = matches;
            RefreshCellStates();
        }

        /// <summary>
        /// 활성 셀을 지정한 좌표로 옮깁니다.
        /// </summary>
        /// <param name="coord">이동할 좌표입니다.</param>
        /// <param name="extendSelection">범위를 확장하면 true, 선택을 축소하면 false입니다.</param>
        public void SetActiveCell(CellCoord coord, bool extendSelection)
        {
            CellCoord clamped = new(
                Math.Clamp(coord.Row, MinRow, MaxRow),
                Math.Clamp(coord.Column, 0, MaxColumn));

            CommitEdit();

            if (extendSelection)
                Selection.ExtendTo(clamped);
            else
                Selection.SetActive(clamped);

            ScrollToActiveCell();

            // 선택이 그대로여서 Changed가 발생하지 않는 경우에도 배치는 맞춰 둔다.
            UpdateEditFieldPlacement();
        }

        /// <summary>
        /// 활성 셀을 상대 좌표만큼 이동합니다.
        /// </summary>
        /// <param name="rowDelta">행 이동량입니다.</param>
        /// <param name="columnDelta">열 이동량입니다.</param>
        /// <param name="extendSelection">범위를 확장하면 true입니다.</param>
        public void MoveActiveCell(int rowDelta, int columnDelta, bool extendSelection)
        {
            CellCoord focus = Selection.Focus;
            SetActiveCell(new CellCoord(focus.Row + rowDelta, focus.Column + columnDelta), extendSelection);
        }

        /// <summary>
        /// 활성 셀을 오른쪽으로 옮기되 행 끝에서는 다음 행 처음으로 넘어갑니다.
        /// </summary>
        /// <param name="backward">왼쪽으로 이동하면 true입니다.</param>
        public void MoveActiveCellWithWrap(bool backward)
        {
            CellCoord focus = Selection.Focus;
            int row = focus.Row;
            int column = focus.Column + (backward ? -1 : 1);

            if (column > MaxColumn)
            {
                column = 0;
                row = Math.Min(MaxRow, row + 1);
            }
            else if (column < 0)
            {
                column = MaxColumn;
                row = Math.Max(MinRow, row - 1);
            }

            SetActiveCell(new CellCoord(row, column), false);
        }

        /// <summary>
        /// 표 맨 아래에 빈 행을 추가하고 그 행으로 활성 셀을 옮깁니다.
        /// </summary>
        public void AppendRow()
        {
            if (_document == null)
                return;

            RequestInsertRows(_document.RowCount, 1);
        }

        /// <summary>
        /// 표 맨 오른쪽에 빈 열을 추가하고 그 열로 활성 셀을 옮깁니다.
        /// </summary>
        public void AppendColumn()
        {
            if (_document == null)
                return;

            RequestInsertColumns(_document.ColumnCount, 1);
        }

        /// <summary>
        /// Delete 키 동작을 수행합니다.
        /// 행 번호나 열 제목으로 선택한 상태면 그 행/열을 삭제하고, 그 외에는 셀 내용만 지웁니다.
        /// </summary>
        public void DeleteSelection()
        {
            if (_document == null)
                return;

            if (Selection.Kind == CellSelectionKind.Rows)
            {
                int count = Selection.MaxRow - Selection.MinRow + 1;

                if (CanRemoveRows(count))
                {
                    RequestRemoveRows(Selection.MinRow, count);
                    return;
                }
            }
            else if (Selection.Kind == CellSelectionKind.Columns)
            {
                int count = Selection.MaxColumn - Selection.MinColumn + 1;

                if (CanRemoveColumns(count))
                {
                    RequestRemoveColumns(Selection.MinColumn, count);
                    return;
                }
            }

            // 마지막 남은 한 행/열이라 삭제할 수 없으면 내용만 지운다.
            ClearRequested?.Invoke();
        }

        /// <summary>
        /// 표 전체를 선택합니다.
        /// </summary>
        public void SelectAll()
        {
            CommitEdit();
            Selection.SetRange(MinRow, 0, MaxRow, MaxColumn);
        }

        /// <summary>
        /// 활성 셀 편집을 시작합니다.
        /// </summary>
        /// <param name="initialText">편집을 시작할 초기 문자열입니다. null이면 기존 값을 그대로 사용합니다.</param>
        public void BeginEdit(string initialText)
        {
            if (_document == null || _isEditing)
                return;

            _isDragSelecting = false;

            ScrollToActiveCell();
            UpdateEditFieldPlacement();

            _editingCoord = Selection.Focus;
            _editOriginalValue = _document.GetCell(_editingCoord.Row, _editingCoord.Column);
            _isEditing = true;
            _isTypingEntry = initialText != null;

            string startText = initialText ?? _editOriginalValue;

            _suppressEditCommit = true;
            _editField.SetValueWithoutNotify(startText);
            _suppressEditCommit = false;

            SetEditFieldEditing(true);
            FocusEditField();

            if (initialText == null)
                _editField.SelectAll();
            else
                _editField.SelectRange(startText.Length, startText.Length);

            RefreshCellStates();
        }

        /// <summary>
        /// 편집 중인 값을 문서에 반영하고 편집을 종료합니다.
        /// </summary>
        public void CommitEdit()
        {
            if (!_isEditing)
                return;

            string value = _editField.value ?? string.Empty;
            CellCoord coord = _editingCoord;

            EndEdit();

            if (!string.Equals(value, _editOriginalValue, StringComparison.Ordinal))
            {
                string[][] values = { new[] { value } };
                CommandRequested?.Invoke(new SetCellsCommand("셀 편집", coord.Row, coord.Column, values));
            }

            RefreshCellStates();
        }

        /// <summary>
        /// 편집을 취소하고 원래 값을 유지합니다.
        /// </summary>
        public void CancelEdit()
        {
            if (!_isEditing)
                return;

            EndEdit();
            RefreshCellStates();
        }

        /// <summary>
        /// 활성 셀이 화면에 보이도록 스크롤합니다.
        /// </summary>
        public void ScrollToActiveCell()
        {
            if (_document == null || _itemIndices.Count == 0)
                return;

            int itemIndex = Math.Clamp(Selection.Focus.Row - HeaderOffset, 0, _itemIndices.Count - 1);
            _listView.ScrollToItem(itemIndex);

            ScrollToColumn(Selection.Focus.Column);
        }

        /// <summary>
        /// 그리드에 키보드 포커스를 부여합니다.
        /// </summary>
        public void FocusGrid()
        {
            // 편집 필드가 항상 포커스를 갖는다. 그래야 첫 타건이 IME를 거쳐 바로 필드로 들어간다.
            FocusEditField();
        }

        /// <summary>
        /// 등록한 콜백과 이벤트 구독을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            Selection.Changed -= OnSelectionChanged;
            _editField.UnregisterCallback<FocusOutEvent>(OnEditFieldFocusOut);
            _editField.UnregisterCallback<ChangeEvent<string>>(OnEditFieldValueChanged);
            _listView.UnregisterCallback<GeometryChangedEvent>(OnListGeometryChanged);
            _listView.UnregisterCallback<PointerDownEvent>(OnGridPointerDown, TrickleDown.TrickleDown);
            _listView.UnregisterCallback<PointerMoveEvent>(OnGridPointerMove, TrickleDown.TrickleDown);
            _listView.UnregisterCallback<PointerUpEvent>(OnGridPointerUp, TrickleDown.TrickleDown);

            if (_columnHeaderContainer != null)
            {
                if (_columnHeaderManipulator != null)
                {
                    _columnHeaderContainer.RemoveManipulator(_columnHeaderManipulator);
                    _columnHeaderManipulator = null;
                }

                _columnHeaderContainer = null;
            }

            if (_document != null)
            {
                _document.CellChanged -= OnDocumentCellChanged;
                _document.StructureChanged -= OnDocumentStructureChanged;
                _document = null;
            }
        }

        private int HeaderOffset => UseFirstRowAsHeader && _document != null && _document.RowCount > 1 ? 1 : 0;

        private void OnListGeometryChanged(GeometryChangedEvent evt)
        {
            _scrollView ??= _listView.Q<ScrollView>();
            TryAttachColumnHeaderMenu();

            // 열이 다시 만들어지면 헤더 요소도 새로 생기므로 강조를 다시 입힌다.
            RefreshColumnHeaderStates();
            UpdateEditFieldPlacement();
        }

        // 열 헤더는 MultiColumnListView 내부에서 나중에 만들어지므로 지연 조회한다.
        private void TryAttachColumnHeaderMenu()
        {
            if (_columnHeaderContainer != null)
                return;

            _columnHeaderContainer = _listView.Q<VisualElement>(
                className: UnityColumnHeaderContainerClassName);

            if (_columnHeaderContainer == null)
                return;

            _columnHeaderManipulator = new ContextualMenuManipulator(BuildColumnHeaderContextMenu);
            _columnHeaderContainer.AddManipulator(_columnHeaderManipulator);

        }

        // PointerUp은 헤더가 포인터를 캡처해 삼키므로 여기까지 오지 않는다. PointerDown에서 바로 선택한다.
        private void OnGridPointerDown(PointerDownEvent evt)
        {
            _pressedColumnIndex = TryGetHeaderColumnAt(evt.position, out int columnIndex)
                ? columnIndex
                : -1;

            // 우클릭은 선택을 바꾸지 않고 컨텍스트 메뉴가 쓸 열만 기억한다.
            if (evt.button != 0)
                return;

            // 셀 위에서 눌렀으면 드래그로 범위를 넓힐 준비를 한다.
            // 더블클릭은 편집 진입이므로 드래그를 걸지 않는다. 걸어두면 손떨림 한 번에 편집이 닫힌다.
            if (_pressedColumnIndex < 0)
            {
                _isDragSelecting = evt.clickCount < 2 && TryGetCellAt(evt.position, out _);
                return;
            }

            // 열 경계 근처는 폭 조절 영역이라 선택에서 제외한다.
            if (IsNearColumnEdge(evt.position, _pressedColumnIndex))
                return;

            CommitEdit();

            int firstColumn = evt.shiftKey && Selection.Kind == CellSelectionKind.Columns
                ? Selection.Anchor.Column
                : _pressedColumnIndex;

            Selection.SetRange(MinRow, firstColumn, MaxRow, _pressedColumnIndex, CellSelectionKind.Columns);
            FocusGrid();
        }

        private bool IsNearColumnEdge(Vector2 position, int columnIndex)
        {
            List<VisualElement> headers = GetColumnHeaders();
            int headerIndex = columnIndex + 1;

            if (headerIndex < 0 || headerIndex >= headers.Count)
                return false;

            Rect bound = headers[headerIndex].worldBound;

            return position.x - bound.xMin <= ColumnResizeEdgeWidth
                || bound.xMax - position.x <= ColumnResizeEdgeWidth;
        }

        // 드래그로 셀 범위를 넓힌다. 누른 셀이 anchor로 남고 지나가는 셀이 focus가 된다.
        private void OnGridPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragSelecting)
                return;

            // 편집 중이면 드래그 선택을 하지 않는다. 편집을 커밋해 버리면 안 된다.
            if (_isEditing)
            {
                _isDragSelecting = false;
                return;
            }

            // 버튼을 놓으면 끝낸다. PointerUp이 캡처에 먹혀 오지 않을 수 있어 여기서도 확인한다.
            if ((evt.pressedButtons & 1) == 0)
            {
                _isDragSelecting = false;
                return;
            }

            if (!TryGetCellAt(evt.position, out CellCoord coord))
                return;

            if (coord.Equals(Selection.Focus))
                return;

            CommitEdit();
            Selection.ExtendTo(coord);
        }

        private void OnGridPointerUp(PointerUpEvent evt)
        {
            _isDragSelecting = false;
        }

        // 좌표로 어떤 셀 위인지 판별한다. 화면에 보이는 셀만 존재하므로 순회 비용은 작다.
        private bool TryGetCellAt(Vector2 position, out CellCoord coord)
        {
            CellCoord found = default;
            bool hit = false;

            _listView.Query<VisualElement>(className: CellClassName).ForEach(cell =>
            {
                if (hit)
                    return;

                if (cell.userData is not TableCellBinding binding || binding.Row < 0)
                    return;

                if (!cell.worldBound.Contains(position))
                    return;

                found = new CellCoord(binding.Row, binding.Column);
                hit = true;
            });

            coord = found;
            return hit;
        }

        // evt.target이나 포인터 캡처에 기대지 않고 좌표만으로 어떤 열 제목을 눌렀는지 판별한다.
        // MultiColumnListView 내부 이벤트 처리 방식이 바뀌어도 영향을 받지 않는다.
        private bool TryGetHeaderColumnAt(Vector2 position, out int columnIndex)
        {
            columnIndex = -1;

            if (_columnHeaderContainer == null)
                return false;

            if (!_columnHeaderContainer.worldBound.Contains(position))
                return false;

            List<VisualElement> headers = GetColumnHeaders();

            for (int index = 0; index < headers.Count; index++)
            {
                if (!headers[index].worldBound.Contains(position))
                    continue;

                // 0번은 행 번호 거터 헤더라 데이터 열이 아니다.
                columnIndex = index - 1;
                return columnIndex >= 0;
            }

            return false;
        }

        private void OnDocumentCellChanged(int row, int column)
        {
            int itemIndex = row - HeaderOffset;

            if (itemIndex < 0)
            {
                // 헤더 행이 바뀌면 열 제목을 다시 만든다.
                RebuildColumns();
                return;
            }

            if (itemIndex < _itemIndices.Count)
                _listView.RefreshItem(itemIndex);
        }

        private void OnDocumentStructureChanged()
        {
            RebuildColumns();
            RebuildItems();
            Selection.Clamp(MinRow, _document?.RowCount ?? 1, _document?.ColumnCount ?? 1);
        }

        private void OnSelectionChanged()
        {
            RefreshCellStates();
            UpdateEditFieldPlacement();
        }

        private void RebuildItems()
        {
            _itemIndices.Clear();

            int rowCount = _document?.RowCount ?? 0;

            for (int index = HeaderOffset; index < rowCount; index++)
                _itemIndices.Add(index);

            _listView.RefreshItems();
        }

        private void RebuildColumns()
        {
            SaveColumnWidths();
            _listView.columns.Clear();

            if (_document == null)
                return;

            Column rowNumberColumn = new();
            rowNumberColumn.name = "table-editor-row-number";
            rowNumberColumn.title = "#";
            rowNumberColumn.width = RowNumberColumnWidth;
            rowNumberColumn.minWidth = 32f;
            rowNumberColumn.resizable = false;
            rowNumberColumn.sortable = false;
            rowNumberColumn.optional = false;
            rowNumberColumn.makeCell = MakeRowNumberCell;
            rowNumberColumn.bindCell = BindRowNumberCell;
            _listView.columns.Add(rowNumberColumn);

            for (int columnIndex = 0; columnIndex < _document.ColumnCount; columnIndex++)
            {
                int captured = columnIndex;

                Column column = new();
                column.name = $"table-editor-column-{columnIndex}";
                column.title = GetColumnTitle(columnIndex);
                column.width = GetStoredColumnWidth(columnIndex);
                column.minWidth = 40f;
                column.resizable = true;
                column.sortable = false;
                column.optional = false;
                column.makeCell = MakeCell;
                column.bindCell = (element, itemIndex) => BindCell(element, itemIndex, captured);
                _listView.columns.Add(column);
            }
        }

        private void SaveColumnWidths()
        {
            if (_listView.columns.Count <= 1)
                return;

            _columnWidths.Clear();

            for (int index = 1; index < _listView.columns.Count; index++)
            {
                float width = _listView.columns[index].width.value;
                _columnWidths.Add(width > 0f ? width : DefaultColumnWidth);
            }
        }

        private float GetStoredColumnWidth(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < _columnWidths.Count)
                return _columnWidths[columnIndex];

            return DefaultColumnWidth;
        }

        private string GetColumnTitle(int columnIndex)
        {
            if (UseFirstRowAsHeader && _document != null && _document.RowCount > 1)
            {
                string header = _document.GetCell(0, columnIndex);

                if (!string.IsNullOrEmpty(header))
                    return header;
            }

            return GetSpreadsheetColumnName(columnIndex);
        }

        /// <summary>
        /// 열 인덱스를 스프레드시트식 알파벳 이름으로 변환합니다.
        /// </summary>
        /// <param name="columnIndex">0부터 시작하는 열 인덱스입니다.</param>
        /// <returns>A, B, ... Z, AA 형태의 열 이름입니다.</returns>
        public static string GetSpreadsheetColumnName(int columnIndex)
        {
            StringBuilder builder = new();
            int value = columnIndex;

            do
            {
                builder.Insert(0, (char)('A' + value % 26));
                value = value / 26 - 1;
            }
            while (value >= 0);

            return builder.ToString();
        }

        private VisualElement MakeRowNumberCell()
        {
            VisualElement cell = new();
            cell.AddToClassList(RowNumberClassName);
            cell.userData = new TableCellBinding();

            Label label = new();
            label.AddToClassList(RowNumberLabelClassName);
            label.pickingMode = PickingMode.Ignore;
            cell.Add(label);

            cell.RegisterCallback<PointerDownEvent>(OnRowNumberPointerDown);
            cell.AddManipulator(new ContextualMenuManipulator(BuildRowNumberContextMenu));

            return cell;
        }

        private void BindRowNumberCell(VisualElement element, int itemIndex)
        {
            int row = itemIndex + HeaderOffset;

            if (element.userData is TableCellBinding binding)
            {
                binding.Row = row;

                // 셀 요소는 재활용되므로 선택 강조를 매번 다시 적용한다.
                UpdateRowNumberState(element);
            }

            Label label = element.Q<Label>(className: RowNumberLabelClassName);

            if (label == null)
                return;

            label.text = (row + 1).ToString();
        }

        // 행 번호를 클릭하면 스프레드시트처럼 그 행 전체를 선택한다.
        private void OnRowNumberPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (evt.currentTarget is not VisualElement cell)
                return;

            if (cell.userData is not TableCellBinding binding)
                return;

            evt.StopPropagation();
            _listView.focusController?.IgnoreEvent(evt);

            CommitEdit();

            int firstRow = evt.shiftKey && Selection.Kind == CellSelectionKind.Rows
                ? Selection.Anchor.Row
                : binding.Row;

            Selection.SetRange(firstRow, 0, binding.Row, MaxColumn, CellSelectionKind.Rows);
            FocusGrid();
        }

        private VisualElement MakeCell()
        {
            VisualElement cell = new();
            cell.AddToClassList(CellClassName);
            cell.userData = new TableCellBinding();

            Label label = new();
            label.AddToClassList(CellLabelClassName);
            label.pickingMode = PickingMode.Ignore;
            cell.Add(label);

            cell.RegisterCallback<PointerDownEvent>(OnCellPointerDown);
            cell.AddManipulator(new ContextualMenuManipulator(BuildCellContextMenu));

            return cell;
        }

        private void BindCell(VisualElement element, int itemIndex, int columnIndex)
        {
            if (element.userData is not TableCellBinding binding)
                return;

            int row = itemIndex + HeaderOffset;
            binding.Row = row;
            binding.Column = columnIndex;

            Label label = element.Q<Label>(className: CellLabelClassName);

            if (label != null)
                label.text = _document != null ? _document.GetCell(row, columnIndex) : string.Empty;

            // 셀 요소는 재활용되므로 선택 상태를 매번 모델에서 다시 적용해야 한다.
            UpdateCellState(element);
        }

        private void OnCellPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (evt.currentTarget is not VisualElement cell)
                return;

            if (cell.userData is not TableCellBinding binding)
                return;

            CellCoord coord = new(binding.Row, binding.Column);

            if (evt.clickCount >= 2)
            {
                // StopPropagation만으로는 포커스 컨트롤러가 포인터 다운으로 포커스를 되가져가는 것을 막지 못한다.
                // IgnoreEvent를 함께 호출하지 않으면 편집 필드가 열리자마자 포커스를 잃고 닫힌다.
                evt.StopPropagation();
                _listView.focusController?.IgnoreEvent(evt);

                SetActiveCell(coord, false);
                BeginEdit(null);
                return;
            }

            // 편집 필드가 포커스를 유지해야 첫 타건이 IME를 거쳐 바로 들어온다.
            // StopPropagation만으로는 포커스 컨트롤러가 ListView로 포커스를 되가져간다.
            evt.StopPropagation();
            _listView.focusController?.IgnoreEvent(evt);

            SetActiveCell(coord, evt.shiftKey);
            FocusGrid();
        }

        private void BuildCellContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_document == null)
                return;

            if (evt.currentTarget is VisualElement cell && cell.userData is TableCellBinding binding)
            {
                // 선택 범위 밖을 우클릭하면 그 셀로 옮기고, 범위 안이면 선택을 유지한다.
                if (!Selection.Contains(binding.Row, binding.Column))
                    SetActiveCell(new CellCoord(binding.Row, binding.Column), false);
            }

            evt.menu.ClearItems();

            AppendRowActions(evt.menu);
            evt.menu.AppendSeparator();
            AppendColumnActions(evt.menu);
            evt.menu.AppendSeparator();
            AppendClipboardActions(evt.menu);
        }

        private void BuildRowNumberContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_document == null)
                return;

            if (evt.currentTarget is VisualElement cell && cell.userData is TableCellBinding binding)
            {
                if (binding.Row < Selection.MinRow || binding.Row > Selection.MaxRow)
                    Selection.SetRange(binding.Row, 0, binding.Row, MaxColumn, CellSelectionKind.Rows);
            }

            evt.menu.ClearItems();

            AppendRowActions(evt.menu);
            evt.menu.AppendSeparator();
            AppendClipboardActions(evt.menu);
        }

        private void BuildColumnHeaderContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Unity 기본 열 표시/숨김 메뉴를 걷어내고 우리 항목만 남긴다.
            evt.menu.ClearItems();

            if (_document == null)
                return;

            // 우클릭 직전의 PointerDown에서 좌표로 판별해 둔 열을 쓴다.
            int columnIndex = _pressedColumnIndex;

            if (columnIndex < 0)
                return;

            if (columnIndex < Selection.MinColumn || columnIndex > Selection.MaxColumn)
                Selection.SetRange(MinRow, columnIndex, MaxRow, columnIndex, CellSelectionKind.Columns);

            AppendColumnActions(evt.menu);
            evt.menu.AppendSeparator();
            AppendClipboardActions(evt.menu);
        }

        private List<VisualElement> GetColumnHeaders()
        {
            if (_columnHeaderContainer == null)
                return new List<VisualElement>();

            return _columnHeaderContainer
                .Query<VisualElement>(className: UnityColumnHeaderClassName)
                .ToList();
        }

        private void AppendRowActions(DropdownMenu menu)
        {
            int firstRow = Selection.MinRow;
            int lastRow = Selection.MaxRow;
            int count = lastRow - firstRow + 1;

            menu.AppendAction("위에 행 삽입", _ => RequestInsertRows(firstRow, count));
            menu.AppendAction("아래에 행 삽입", _ => RequestInsertRows(lastRow + 1, count));

            string label = count > 1 ? $"행 {count}개 삭제" : "행 삭제";
            menu.AppendAction(
                label,
                _ => RequestRemoveRows(firstRow, count),
                _ => CanRemoveRows(count) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void AppendColumnActions(DropdownMenu menu)
        {
            int firstColumn = Selection.MinColumn;
            int lastColumn = Selection.MaxColumn;
            int count = lastColumn - firstColumn + 1;

            menu.AppendAction("왼쪽에 열 삽입", _ => RequestInsertColumns(firstColumn, count));
            menu.AppendAction("오른쪽에 열 삽입", _ => RequestInsertColumns(lastColumn + 1, count));

            string label = count > 1 ? $"열 {count}개 삭제" : "열 삭제";
            menu.AppendAction(
                label,
                _ => RequestRemoveColumns(firstColumn, count),
                _ => CanRemoveColumns(count) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void AppendClipboardActions(DropdownMenu menu)
        {
            menu.AppendAction("복사", _ => CopyRequested?.Invoke());
            menu.AppendAction("잘라내기", _ => CutRequested?.Invoke());
            menu.AppendAction(
                "붙여넣기",
                _ => PasteRequested?.Invoke(),
                _ => TableClipboard.HasContent()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("내용 지우기", _ => ClearRequested?.Invoke());
        }

        private bool CanRemoveRows(int count)
        {
            return _document != null && _document.RowCount - count >= 1;
        }

        private bool CanRemoveColumns(int count)
        {
            return _document != null && _document.ColumnCount - count >= 1;
        }

        private void RequestInsertRows(int index, int count)
        {
            CommandRequested?.Invoke(new InsertRowsCommand(index, count));
            SetActiveCell(new CellCoord(index, Selection.Focus.Column), false);
        }

        private void RequestRemoveRows(int index, int count)
        {
            CommandRequested?.Invoke(new RemoveRowsCommand(index, count));
        }

        private void RequestInsertColumns(int index, int count)
        {
            CommandRequested?.Invoke(new InsertColumnsCommand(index, count));
            SetActiveCell(new CellCoord(Selection.Focus.Row, index), false);
        }

        private void RequestRemoveColumns(int index, int count)
        {
            CommandRequested?.Invoke(new RemoveColumnsCommand(index, count));
        }

        private void RefreshCellStates()
        {
            _listView.Query<VisualElement>(className: CellClassName).ForEach(UpdateCellState);
            _listView.Query<VisualElement>(className: RowNumberClassName).ForEach(UpdateRowNumberState);
            RefreshColumnHeaderStates();
        }

        private void UpdateRowNumberState(VisualElement cell)
        {
            if (cell.userData is not TableCellBinding binding || binding.Row < 0)
                return;

            // 셀을 고른 것만으로는 강조하지 않는다. 행 번호를 눌러 행 자체를 선택했을 때만 켠다.
            bool isSelected = Selection.Kind == CellSelectionKind.Rows
                && binding.Row >= Selection.MinRow
                && binding.Row <= Selection.MaxRow;

            cell.EnableInClassList(RowNumberSelectedClassName, isSelected);
        }

        // 열을 선택했다는 사실이 보이도록 헤더에 강조 클래스를 토글한다.
        private void RefreshColumnHeaderStates()
        {
            if (_columnHeaderContainer == null)
                return;

            List<VisualElement> headers = GetColumnHeaders();

            for (int index = 0; index < headers.Count; index++)
            {
                // 0번은 행 번호 거터 헤더라 데이터 열이 아니다.
                int columnIndex = index - 1;

                bool isSelected = columnIndex >= 0
                    && Selection.Kind == CellSelectionKind.Columns
                    && columnIndex >= Selection.MinColumn
                    && columnIndex <= Selection.MaxColumn;

                headers[index].EnableInClassList(ColumnHeaderSelectedClassName, isSelected);
            }
        }

        private void UpdateCellState(VisualElement cell)
        {
            if (cell.userData is not TableCellBinding binding)
                return;

            if (binding.Row < 0 || binding.Column < 0)
                return;

            bool isActive = Selection.Focus.Row == binding.Row && Selection.Focus.Column == binding.Column;
            bool isSelected = Selection.Contains(binding.Row, binding.Column);
            bool isMatch = _matches != null && _matches.Contains(new CellCoord(binding.Row, binding.Column));

            cell.EnableInClassList(SelectedClassName, isSelected && !isActive);
            cell.EnableInClassList(ActiveClassName, isActive);
            cell.EnableInClassList(MatchClassName, isMatch && !isSelected);
            cell.EnableInClassList(EditingClassName, _isEditing && isActive);
        }

        // 편집 필드는 항상 활성 셀 위에 놓여 있고 항상 포커스를 유지한다.
        // 그래야 한글 IME 조합이 첫 자모부터 필드에서 직접 시작되어 끊기지 않는다.
        private void UpdateEditFieldPlacement()
        {
            VisualElement cell = FindCellElement(Selection.Focus);

            if (cell == null)
                return;

            Rect worldBound = cell.worldBound;

            if (float.IsNaN(worldBound.x) || worldBound.width <= 0f)
                return;

            Vector2 topLeft = _container.WorldToLocal(new Vector2(worldBound.xMin, worldBound.yMin));

            _editField.style.left = topLeft.x;
            _editField.style.top = topLeft.y;
            _editField.style.width = worldBound.width;
            _editField.style.height = worldBound.height;
        }

        // 편집 중이 아닐 때는 투명하게 두고 클릭이 셀로 지나가게 한다. display는 끄지 않는다.
        private void SetEditFieldEditing(bool editing)
        {
            _editField.EnableInClassList(EditFieldEditingClassName, editing);

            // pickingMode는 자신에게만 적용된다. 내부 입력 요소까지 꺼야 유휴 상태에서
            // 클릭이 활성 셀로 통과한다. 끄지 않으면 더블클릭이 셀에 도달하지 못한다.
            PickingMode mode = editing ? PickingMode.Position : PickingMode.Ignore;
            _editField.pickingMode = mode;

            foreach (VisualElement child in _editField.Query<VisualElement>().ToList())
                child.pickingMode = mode;
        }

        // TextField.Focus()는 래퍼에 포커스를 주는 경우가 있어 내부 입력 요소를 직접 지정한다.
        private void FocusEditField()
        {
            VisualElement input = _editField.Q(TextField.textInputUssName);

            if (input != null)
                input.Focus();
            else
                _editField.Focus();
        }

        private void EndEdit()
        {
            _isEditing = false;
            _isTypingEntry = false;

            _suppressEditCommit = true;
            _editField.SetValueWithoutNotify(string.Empty);
            _suppressEditCommit = false;

            SetEditFieldEditing(false);
            UpdateEditFieldPlacement();
        }

        // 항상 포커스된 빈 필드에 사용자가 직접 입력했다는 뜻이다. IME 조합도 이 경로로 들어온다.
        private void OnEditFieldValueChanged(ChangeEvent<string> evt)
        {
            if (_suppressEditCommit || _isEditing || _document == null)
                return;

            if (string.IsNullOrEmpty(evt.newValue))
                return;

            _isDragSelecting = false;

            // 유휴 상태에서는 위치가 어긋나 있을 수 있으므로 보이기 직전에 활성 셀 위로 확정한다.
            UpdateEditFieldPlacement();

            _editingCoord = Selection.Focus;
            _editOriginalValue = _document.GetCell(_editingCoord.Row, _editingCoord.Column);
            _isEditing = true;
            _isTypingEntry = true;

            SetEditFieldEditing(true);
            RefreshCellStates();
        }

        private void OnEditFieldFocusOut(FocusOutEvent evt)
        {
            if (_suppressEditCommit || !_isEditing)
                return;

            CommitEdit();
        }

        private VisualElement FindCellElement(CellCoord coord)
        {
            VisualElement found = null;

            _listView.Query<VisualElement>(className: CellClassName).ForEach(cell =>
            {
                if (found != null)
                    return;

                if (cell.userData is TableCellBinding binding
                    && binding.Row == coord.Row
                    && binding.Column == coord.Column)
                {
                    found = cell;
                }
            });

            return found;
        }

        // MultiColumnListView는 가로 스크롤 API를 제공하지 않으므로 누적 폭으로 직접 계산한다.
        private void ScrollToColumn(int columnIndex)
        {
            _scrollView ??= _listView.Q<ScrollView>();

            if (_scrollView == null || _listView.columns.Count <= columnIndex + 1)
                return;

            float left = 0f;

            for (int index = 0; index <= columnIndex; index++)
                left += GetResolvedColumnWidth(index);

            float width = GetResolvedColumnWidth(columnIndex + 1);
            float right = left + width;
            float viewportWidth = _scrollView.contentViewport.resolvedStyle.width;

            if (float.IsNaN(viewportWidth) || viewportWidth <= 0f)
                return;

            Vector2 offset = _scrollView.scrollOffset;

            if (left < offset.x)
                offset.x = left;
            else if (right > offset.x + viewportWidth)
                offset.x = right - viewportWidth;
            else
                return;

            _scrollView.scrollOffset = offset;
        }

        private float GetResolvedColumnWidth(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _listView.columns.Count)
                return 0f;

            float width = _listView.columns[columnIndex].width.value;

            return width > 0f ? width : DefaultColumnWidth;
        }
    }
}
