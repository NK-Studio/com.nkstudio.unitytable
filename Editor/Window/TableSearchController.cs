using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 셀 내용 검색과 일치 항목 사이 이동을 담당합니다.
    /// </summary>
    public sealed class TableSearchController : IDisposable
    {
        private const string HiddenClassName = "table-editor__search-bar--hidden";

        private readonly TableGridView _gridView;
        private readonly VisualElement _searchBar;
        private readonly ToolbarSearchField _searchField;
        private readonly Label _countLabel;
        private readonly ToolbarToggle _caseToggle;
        private readonly ToolbarButton _previousButton;
        private readonly ToolbarButton _nextButton;
        private readonly ToolbarButton _closeButton;

        private readonly List<CellCoord> _matches = new();
        private readonly HashSet<CellCoord> _matchSet = new();

        private int _currentIndex = -1;

        /// <summary>
        /// 검색 컨트롤러를 생성하고 검색 바 요소를 캐싱합니다.
        /// </summary>
        /// <param name="root">윈도우의 최상위 VisualElement입니다.</param>
        /// <param name="gridView">검색 결과를 표시할 그리드 View입니다.</param>
        public TableSearchController(VisualElement root, TableGridView gridView)
        {
            _gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));

            _searchBar = root.Q<VisualElement>("table-editor__search-bar");
            _searchField = root.Q<ToolbarSearchField>("table-editor__search-field");
            _countLabel = root.Q<Label>("table-editor__search-count");
            _caseToggle = root.Q<ToolbarToggle>("table-editor__search-case-toggle");
            _previousButton = root.Q<ToolbarButton>("table-editor__search-previous-button");
            _nextButton = root.Q<ToolbarButton>("table-editor__search-next-button");
            _closeButton = root.Q<ToolbarButton>("table-editor__search-close-button");

            _searchField?.RegisterValueChangedCallback(OnSearchValueChanged);
            _caseToggle?.RegisterValueChangedCallback(OnCaseToggleChanged);

            if (_previousButton != null)
                _previousButton.clicked += SelectPrevious;

            if (_nextButton != null)
                _nextButton.clicked += SelectNext;

            if (_closeButton != null)
                _closeButton.clicked += Close;
        }

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
        /// 검색 바를 열고 입력 필드에 포커스를 줍니다.
        /// </summary>
        public void Open()
        {
            if (_searchBar == null)
                return;

            _searchBar.RemoveFromClassList(HiddenClassName);
            RebuildMatches();

            // display가 켜진 것이 resolvedStyle에 반영되기 전에는 포커스를 받지 못하므로 다음 프레임에 준다.
            _searchBar.schedule.Execute(() => _searchField?.Focus()).ExecuteLater(0);
        }

        /// <summary>
        /// 검색 바를 닫고 강조 표시를 지웁니다.
        /// </summary>
        public void Close()
        {
            if (_searchBar == null)
                return;

            _searchBar.AddToClassList(HiddenClassName);
            _matches.Clear();
            _matchSet.Clear();
            _currentIndex = -1;

            _gridView.SetSearchMatches(null);
            _gridView.FocusGrid();
        }

        /// <summary>
        /// 문서가 바뀌었을 때 검색 결과를 다시 계산합니다.
        /// </summary>
        public void Refresh()
        {
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
        /// 등록한 콜백을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _searchField?.UnregisterValueChangedCallback(OnSearchValueChanged);
            _caseToggle?.UnregisterValueChangedCallback(OnCaseToggleChanged);

            if (_previousButton != null)
                _previousButton.clicked -= SelectPrevious;

            if (_nextButton != null)
                _nextButton.clicked -= SelectNext;

            if (_closeButton != null)
                _closeButton.clicked -= Close;
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            RebuildMatches();
        }

        private void OnCaseToggleChanged(ChangeEvent<bool> evt)
        {
            RebuildMatches();
        }

        private void RebuildMatches()
        {
            _matches.Clear();
            _matchSet.Clear();
            _currentIndex = -1;

            TableDocument document = _gridView.Document;
            string keyword = _searchField?.value ?? string.Empty;

            if (document == null || string.IsNullOrEmpty(keyword))
            {
                _gridView.SetSearchMatches(null);
                UpdateCountLabel();
                return;
            }

            bool matchCase = _caseToggle != null && _caseToggle.value;

            StringComparison comparison = matchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            for (int row = _gridView.MinRow; row <= _gridView.MaxRow; row++)
            {
                for (int column = 0; column <= _gridView.MaxColumn; column++)
                {
                    string value = document.GetCell(row, column);

                    if (string.IsNullOrEmpty(value))
                        continue;

                    if (value.IndexOf(keyword, comparison) < 0)
                        continue;

                    CellCoord coord = new(row, column);
                    _matches.Add(coord);
                    _matchSet.Add(coord);
                }
            }

            _gridView.SetSearchMatches(_matchSet);
            UpdateCountLabel();
        }

        private void MoveToMatch(int direction)
        {
            if (_matches.Count == 0)
                return;

            if (_currentIndex < 0)
                _currentIndex = direction > 0 ? -1 : 0;

            _currentIndex = (_currentIndex + direction + _matches.Count) % _matches.Count;

            _gridView.SetActiveCell(_matches[_currentIndex], false);
            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            if (_countLabel == null)
                return;

            int current = _currentIndex >= 0 ? _currentIndex + 1 : 0;
            _countLabel.text = $"{current} / {_matches.Count}";
        }
    }
}
