using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 찾기·바꾸기 바를 담당합니다. 대소문자·정규식·단어 단위 옵션, 선택 영역에서 찾기, 수직 방향 순서,
    /// 현재 항목 바꾸기와 모두 바꾸기를 처리합니다. 문서 변경은 직접 하지 않고 CommandRequested로 위임합니다.
    /// </summary>
    public sealed class TableSearchController : IDisposable
    {
        private const string HiddenClassName = "table-editor__search-bar--hidden";
        private const string ReplaceRowHiddenClassName = "table-editor__replace-row--hidden";
        private const string ExpandedClassName = "table-editor__search-expand-button--expanded";
        private const string ToggleCheckedClassName = "table-editor__option-toggle--checked";

        // 연속 타이핑 중에는 키마다 전체 셀을 훑지 않도록 입력이 멈춘 뒤에 검색한다.
        // 150ms는 보통 타자 속도의 키 간격보다 길고, 결과가 늦게 뜬다고 느껴지지 않는 정도의 값이다.
        private const long SearchDebounceMs = 150;

        private readonly TableGridView _gridView;
        private readonly VisualElement _searchBar;
        private readonly VisualElement _replaceRow;
        private readonly TextField _searchField;
        private readonly TextField _replaceField;
        private readonly Label _countLabel;
        private readonly Button _expandButton;
        private readonly Button _previousButton;
        private readonly Button _nextButton;
        private readonly Button _closeButton;
        private readonly Button _replaceButton;
        private readonly Button _replaceAllButton;
        private readonly OptionToggle _caseToggle;
        private readonly OptionToggle _regexToggle;
        private readonly OptionToggle _wordToggle;
        private readonly OptionToggle _selectionToggle;
        private readonly OptionToggle _verticalToggle;
        private readonly OptionToggle _preserveCaseToggle;

        private readonly List<CellCoord> _matches = new();
        private readonly HashSet<CellCoord> _matchSet = new();

        private int _currentIndex = -1;
        private IVisualElementScheduledItem _pendingRebuild;
        private TableFindQuery _query = new(string.Empty, false, false, false);

        // 선택 영역에서 찾기를 켠 순간의 범위다. 켠 뒤에 선택을 옮겨도 찾는 범위는 바뀌지 않는다(VS Code와 같다).
        private int _scopeFirstRow;
        private int _scopeFirstColumn;
        private int _scopeLastRow;
        private int _scopeLastColumn;

        // _matches를 만든 조건이다. 단순 모드에서 검색어만 길어졌으면 이전 결과를 좁혀 쓴다.
        private TableFindQuery _matchesQuery;
        private bool _matchesInSelection;
        private bool _matchesVertical;

        /// <summary>
        /// 검색 컨트롤러를 생성하고 검색 바 요소를 캐싱합니다.
        /// </summary>
        /// <param name="root">윈도우의 최상위 VisualElement입니다.</param>
        /// <param name="gridView">검색 결과를 표시할 그리드 View입니다.</param>
        public TableSearchController(VisualElement root, TableGridView gridView)
        {
            _gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));

            _searchBar = root.Q<VisualElement>("table-editor__search-bar");
            _replaceRow = root.Q<VisualElement>("table-editor__replace-row");
            _searchField = root.Q<TextField>("table-editor__search-field");
            _replaceField = root.Q<TextField>("table-editor__replace-field");
            _countLabel = root.Q<Label>("table-editor__search-count");
            _expandButton = root.Q<Button>("table-editor__search-expand-button");
            _previousButton = root.Q<Button>("table-editor__search-previous-button");
            _nextButton = root.Q<Button>("table-editor__search-next-button");
            _closeButton = root.Q<Button>("table-editor__search-close-button");
            _replaceButton = root.Q<Button>("table-editor__replace-button");
            _replaceAllButton = root.Q<Button>("table-editor__replace-all-button");

            _caseToggle = new OptionToggle(root.Q<Button>("table-editor__search-case-toggle"), RebuildMatches);
            _regexToggle = new OptionToggle(root.Q<Button>("table-editor__search-regex-toggle"), RebuildMatches);
            _wordToggle = new OptionToggle(root.Q<Button>("table-editor__search-word-toggle"), RebuildMatches);
            _selectionToggle = new OptionToggle(root.Q<Button>("table-editor__search-selection-toggle"), OnSelectionToggleChanged);
            _verticalToggle = new OptionToggle(root.Q<Button>("table-editor__search-vertical-toggle"), RebuildMatches);
            _preserveCaseToggle = new OptionToggle(root.Q<Button>("table-editor__replace-preserve-case-toggle"), null);

            SetPlaceholder(_searchField, "Find");
            SetPlaceholder(_replaceField, "Replace");

            _searchField?.RegisterValueChangedCallback(OnSearchValueChanged);

            // 버튼을 눌러도 입력칸의 포커스를 빼앗지 않아야 Enter·Esc가 계속 검색 바로 들어온다.
            foreach (Button button in new[] { _expandButton, _previousButton, _nextButton, _closeButton, _replaceButton, _replaceAllButton })
            {
                if (button != null)
                    button.focusable = false;
            }

            if (_expandButton != null)
                _expandButton.clicked += ToggleReplace;

            if (_previousButton != null)
                _previousButton.clicked += SelectPrevious;

            if (_nextButton != null)
                _nextButton.clicked += SelectNext;

            if (_closeButton != null)
                _closeButton.clicked += Close;

            if (_replaceButton != null)
                _replaceButton.clicked += ReplaceCurrent;

            if (_replaceAllButton != null)
                _replaceAllButton.clicked += ReplaceAll;
        }

        /// <summary>
        /// 바꾸기·모두 바꾸기가 문서를 바꿀 때 실행할 작업을 전달합니다.
        /// </summary>
        public event Action<ITableCommand> CommandRequested;

        /// <summary>
        /// 검색 바가 열려 있는지 여부입니다.
        /// </summary>
        public bool IsOpen => _searchBar != null && !_searchBar.ClassListContains(HiddenClassName);

        /// <summary>
        /// 검색 바 안의 요소가 키보드 포커스를 가지고 있는지 확인합니다.
        /// </summary>
        /// <param name="focused">현재 포커스된 요소입니다.</param>
        /// <returns>검색 바 내부 요소면 true입니다.</returns>
        public bool ContainsFocus(VisualElement focused)
        {
            return focused != null && _searchBar != null && _searchBar.Contains(focused);
        }

        /// <summary>
        /// 바꾸기 입력칸이 키보드 포커스를 가지고 있는지 확인합니다.
        /// </summary>
        public bool IsReplaceFieldFocused(VisualElement focused)
        {
            return focused != null && _replaceField != null && _replaceField.Contains(focused);
        }

        /// <summary>
        /// 검색 바를 열고 입력 필드에 포커스를 줍니다.
        /// </summary>
        public void Open()
        {
            if (_searchBar == null)
                return;

            _searchBar.RemoveFromClassList(HiddenClassName);
            RebuildMatches();

            // display가 켜진 것이 resolvedStyle에 반영되기 전에는 포커스를 받지 못하므로 다음 프레임에 준다.
            _searchBar.schedule.Execute(() => FocusField(_searchField)).ExecuteLater(0);
        }

        /// <summary>
        /// 검색 바를 닫고 강조 표시를 지웁니다.
        /// </summary>
        public void Close()
        {
            if (_searchBar == null)
                return;

            // 닫은 뒤에 대기 중이던 검색이 실행되면 강조 표시가 되살아난다.
            CancelPendingRebuild();
            _searchBar.AddToClassList(HiddenClassName);
            _matches.Clear();
            _matchSet.Clear();
            _matchesQuery = null;
            _currentIndex = -1;

            _gridView.SetSearchMatches(null);
            _gridView.FocusGrid();
        }

        /// <summary>
        /// 문서가 바뀌었을 때 검색 결과를 다시 계산합니다.
        /// </summary>
        public void Refresh()
        {
            // 셀 값이 바뀌었으니 이전 결과 밖에서 새로 일치하는 셀이 생겼을 수 있다.
            _matchesQuery = null;

            if (!IsOpen)
                return;

            RebuildMatches();
        }

        /// <summary>
        /// 다음 일치 항목으로 활성 셀을 옮깁니다.
        /// </summary>
        public void SelectNext()
        {
            MoveToMatch(1);
        }

        /// <summary>
        /// 이전 일치 항목으로 활성 셀을 옮깁니다.
        /// </summary>
        public void SelectPrevious()
        {
            MoveToMatch(-1);
        }

        /// <summary>
        /// 현재 일치 항목 셀 안의 일치 부분을 바꾸고 다음 항목으로 넘어갑니다.
        /// 아직 고른 항목이 없으면 첫 항목으로 이동만 합니다(무엇이 바뀔지 먼저 보여 준다).
        /// </summary>
        public void ReplaceCurrent()
        {
            FlushPendingRebuild();

            TableDocument document = _gridView.Document;

            if (document == null || _matches.Count == 0)
                return;

            if (_currentIndex < 0)
            {
                MoveToMatch(1);
                return;
            }

            CellCoord coord = _matches[_currentIndex];
            string value = document.GetCell(coord.Row, coord.Column);
            string replaced = _query.Replace(value, _replaceField?.value, _preserveCaseToggle.Value);

            if (replaced != value)
            {
                // 실행 직후 창이 Refresh()를 불러 결과가 다시 만들어진다.
                CommandRequested?.Invoke(new SetCellsCommand("Replace", coord.Row, coord.Column, new[] { new[] { replaced } }));
            }

            SelectFirstMatchAfter(coord);
        }

        /// <summary>
        /// 일치하는 모든 셀을 한 번에 바꿉니다. Undo 한 번으로 되돌아갑니다.
        /// </summary>
        public void ReplaceAll()
        {
            FlushPendingRebuild();

            TableDocument document = _gridView.Document;

            if (document == null || _matches.Count == 0)
                return;

            List<(int Row, int Column, string Value)> edits = new(_matches.Count);

            foreach (CellCoord coord in _matches)
            {
                string value = document.GetCell(coord.Row, coord.Column);
                string replaced = _query.Replace(value, _replaceField?.value, _preserveCaseToggle.Value);

                if (replaced != value)
                    edits.Add((coord.Row, coord.Column, replaced));
            }

            if (edits.Count == 0)
                return;

            CommandRequested?.Invoke(new ReplaceCellsCommand("Replace All", edits));

            if (_countLabel != null)
                _countLabel.text = edits.Count == 1 ? "Replaced 1 cell" : $"Replaced {edits.Count} cells";
        }

        /// <summary>
        /// 등록한 콜백을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            CancelPendingRebuild();
            _searchField?.UnregisterValueChangedCallback(OnSearchValueChanged);

            _caseToggle.Dispose();
            _regexToggle.Dispose();
            _wordToggle.Dispose();
            _selectionToggle.Dispose();
            _verticalToggle.Dispose();
            _preserveCaseToggle.Dispose();

            if (_expandButton != null)
                _expandButton.clicked -= ToggleReplace;

            if (_previousButton != null)
                _previousButton.clicked -= SelectPrevious;

            if (_nextButton != null)
                _nextButton.clicked -= SelectNext;

            if (_closeButton != null)
                _closeButton.clicked -= Close;

            if (_replaceButton != null)
                _replaceButton.clicked -= ReplaceCurrent;

            if (_replaceAllButton != null)
                _replaceAllButton.clicked -= ReplaceAll;
        }

        private static void SetPlaceholder(TextField field, string placeholder)
        {
            if (field == null)
                return;

            field.textEdition.placeholder = placeholder;
            field.textEdition.hidePlaceholderOnFocus = false;
        }

        // TextField.Focus()는 래퍼에 포커스를 주는 경우가 있어 내부 입력 요소를 직접 지정한다.
        private static void FocusField(TextField field)
        {
            if (field == null)
                return;

            VisualElement input = field.Q(TextField.textInputUssName);

            if (input != null)
                input.Focus();
            else
                field.Focus();
        }

        private void ToggleReplace()
        {
            if (_replaceRow == null)
                return;

            bool expand = _replaceRow.ClassListContains(ReplaceRowHiddenClassName);
            _replaceRow.EnableInClassList(ReplaceRowHiddenClassName, expand == false);
            _expandButton?.EnableInClassList(ExpandedClassName, expand);

            FocusField(expand ? _replaceField : _searchField);
        }

        private void OnSelectionToggleChanged()
        {
            if (_selectionToggle.Value)
            {
                CellSelection selection = _gridView.Selection;
                _scopeFirstRow = selection.MinRow;
                _scopeFirstColumn = selection.MinColumn;
                _scopeLastRow = selection.MaxRow;
                _scopeLastColumn = selection.MaxColumn;
            }

            RebuildMatches();
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            CancelPendingRebuild();
            _pendingRebuild = _searchField.schedule.Execute(RebuildMatches).StartingIn(SearchDebounceMs);
        }

        private void CancelPendingRebuild()
        {
            _pendingRebuild?.Pause();
            _pendingRebuild = null;
        }

        // 입력 직후 Enter·바꾸기를 누르면 아직 이전 검색어의 결과라, 대기 중인 검색을 먼저 끝낸다.
        private void FlushPendingRebuild()
        {
            if (_pendingRebuild != null)
                RebuildMatches();
        }

        private void RebuildMatches()
        {
            // 즉시 검색(열기·문서 변경·옵션 토글)이 대기 중인 검색을 대신하므로 중복 실행을 막는다.
            CancelPendingRebuild();
            _currentIndex = -1;

            TableDocument document = _gridView.Document;

            _query = new TableFindQuery(
                _searchField?.value,
                _caseToggle.Value,
                _regexToggle.Value,
                _wordToggle.Value);

            if (document == null || _query.IsEmpty || _query.IsValid == false)
            {
                _matches.Clear();
                _matchSet.Clear();
                _matchesQuery = null;

                _gridView.SetSearchMatches(null);
                UpdateCountLabel();
                return;
            }

            if (CanNarrowMatches())
                NarrowMatches(document);
            else
                ScanCells(document);

            _matchesQuery = _query;
            _matchesInSelection = _selectionToggle.Value;
            _matchesVertical = _verticalToggle.Value;

            _gridView.SetSearchMatches(_matchSet);
            UpdateCountLabel();
        }

        // 새 검색어가 이전 검색어를 포함하면("ca" → "cat") 새 결과는 이전 결과의 부분집합이다.
        // 단순 모드끼리, 같은 대소문자·범위·순서일 때만 성립한다.
        private bool CanNarrowMatches()
        {
            if (_matchesQuery == null || _matchesQuery.IsPlain == false || _query.IsPlain == false)
                return false;

            if (_matchesQuery.MatchCase != _query.MatchCase
                || _matchesInSelection != _selectionToggle.Value
                || _matchesVertical != _verticalToggle.Value)
            {
                return false;
            }

            StringComparison comparison = _query.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return _query.Keyword.IndexOf(_matchesQuery.Keyword, comparison) >= 0;
        }

        private void ScanCells(TableDocument document)
        {
            _matches.Clear();
            _matchSet.Clear();

            bool inSelection = _selectionToggle.Value;
            int firstRow = inSelection ? Math.Min(_scopeFirstRow, _gridView.MaxRow) : 0;
            int firstColumn = inSelection ? Math.Min(_scopeFirstColumn, _gridView.MaxColumn) : 0;
            int lastRow = inSelection ? Math.Min(_scopeLastRow, _gridView.MaxRow) : _gridView.MaxRow;
            int lastColumn = inSelection ? Math.Min(_scopeLastColumn, _gridView.MaxColumn) : _gridView.MaxColumn;

            foreach (CellCoord coord in TableFindQuery.EnumerateCells(firstRow, firstColumn, lastRow, lastColumn, _verticalToggle.Value))
            {
                if (!_query.IsMatch(document, coord.Row, coord.Column))
                    continue;

                _matches.Add(coord);
                _matchSet.Add(coord);
            }
        }

        // 이전 결과를 순서대로 걸러내므로 검색 순서가 유지된다.
        private void NarrowMatches(TableDocument document)
        {
            _matches.RemoveAll(coord => !_query.IsMatch(document, coord.Row, coord.Column));

            _matchSet.Clear();
            _matchSet.UnionWith(_matches);
        }

        private void MoveToMatch(int direction)
        {
            FlushPendingRebuild();

            if (_matches.Count == 0)
                return;

            if (_currentIndex < 0)
                _currentIndex = direction > 0 ? -1 : 0;

            _currentIndex = (_currentIndex + direction + _matches.Count) % _matches.Count;
            SelectCurrentMatch();
        }

        // 바꾼 셀이 여전히 일치할 수도 있으므로(예: "a" → "aa") 인덱스가 아니라 검색 순서상 다음 셀로 넘어간다.
        private void SelectFirstMatchAfter(CellCoord coord)
        {
            FlushPendingRebuild();

            if (_matches.Count == 0)
            {
                UpdateCountLabel();
                return;
            }

            bool vertical = _verticalToggle.Value;
            _currentIndex = 0;

            for (int index = 0; index < _matches.Count; index++)
            {
                if (TableFindQuery.CompareOrder(_matches[index], coord, vertical) <= 0)
                    continue;

                _currentIndex = index;
                break;
            }

            SelectCurrentMatch();
        }

        private void SelectCurrentMatch()
        {
            _gridView.SetActiveCell(_matches[_currentIndex], false);
            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            if (_countLabel == null)
                return;

            if (_query.IsValid == false)
            {
                _countLabel.text = _query.ErrorMessage;
                return;
            }

            if (_query.IsEmpty)
            {
                _countLabel.text = string.Empty;
                return;
            }

            if (_matches.Count == 0)
            {
                _countLabel.text = "No results";
                return;
            }

            _countLabel.text = _currentIndex >= 0
                ? $"{_currentIndex + 1}/{_matches.Count}"
                : _matches.Count == 1 ? "1 match" : $"{_matches.Count} matches";
        }

        // 버튼을 눌러 켜고 끄는 옵션 토글이다. 켜진 상태는 클래스로 표시한다.
        private sealed class OptionToggle : IDisposable
        {
            private readonly Button _button;
            private readonly Action _changed;

            public OptionToggle(Button button, Action changed)
            {
                _button = button;
                _changed = changed;

                if (_button == null)
                    return;

                _button.focusable = false;
                _button.clicked += OnClicked;
            }

            public bool Value { get; private set; }

            public void Dispose()
            {
                if (_button != null)
                    _button.clicked -= OnClicked;
            }

            private void OnClicked()
            {
                Value = !Value;
                _button.EnableInClassList(ToggleCheckedClassName, Value);
                _changed?.Invoke();
            }
        }
    }
}
