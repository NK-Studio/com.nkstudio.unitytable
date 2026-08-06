using System.IO;
using NKStudio.TabularEditor.Commands;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// CSV/TSV 파일을 스프레드시트처럼 편집하는 에디터 윈도우입니다.
    /// 문서 수명, 파일 저장, Undo 스택 관리를 담당합니다.
    /// </summary>
    public sealed class TableEditorWindow : EditorWindow
    {
        private const string UxmlPath =
            "Packages/com.nkstudio.unitytable/Editor/Window/TableEditorWindow.uxml";

        [SerializeField]
        private string assetPath = string.Empty;

        [SerializeField]
        private bool useFirstRowAsHeader = true;

        private readonly TableCommandStack _commandStack = new();

        private TableDocument _document;
        private TableGridView _gridView;
        private TableSearchController _searchController;
        private TableInputRouter _inputRouter;

        private ToolbarButton _saveButton;
        private ToolbarButton _reloadButton;
        private ToolbarButton _searchButton;
        private Button _addRowButton;
        private Button _addColumnButton;
        private ToolbarToggle _headerToggle;
        private Label _positionLabel;
        private Label _sizeLabel;
        private Label _stateLabel;

        private string _loadedFileHash = string.Empty;

        /// <summary>
        /// 지정한 파일을 테이블 에디터로 엽니다. 이미 같은 파일을 연 창이 있으면 그 창을 활성화합니다.
        /// </summary>
        /// <param name="projectRelativePath">열 파일의 프로젝트 상대 경로입니다.</param>
        /// <returns>파일을 표시하는 윈도우입니다.</returns>
        public static TableEditorWindow Open(string projectRelativePath)
        {
            TableEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TableEditorWindow>();

            foreach (TableEditorWindow existing in windows)
            {
                if (existing.assetPath != projectRelativePath)
                    continue;

                existing.Focus();
                return existing;
            }

            TableEditorWindow window = CreateWindow<TableEditorWindow>();
            window.minSize = new Vector2(420f, 220f);
            window.LoadDocument(projectRelativePath);
            window.Show();
            window.Focus();

            return window;
        }

        [MenuItem("Window/NKStudio/Tabular Editor")]
        private static void OpenEmpty()
        {
            TableEditorWindow window = CreateWindow<TableEditorWindow>();
            window.minSize = new Vector2(420f, 220f);
            window.Show();
        }

        /// <summary>
        /// UXML을 인스턴스화하고 View, 검색, 입력 라우터를 구성합니다.
        /// </summary>
        public void CreateGUI()
        {
            VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

            if (treeAsset == null)
            {
                ShowLoadError();
                return;
            }

            treeAsset.CloneTree(rootVisualElement);

            VisualElement gridContainer = rootVisualElement.Q<VisualElement>("table-editor__grid-container");

            if (gridContainer == null)
            {
                ShowLoadError();
                return;
            }

            CacheToolbarElements();

            _gridView = new TableGridView(gridContainer);
            _gridView.CommandRequested += OnCommandRequested;
            _gridView.Selection.Changed += UpdateStatusBar;
            _gridView.CopyRequested += CopySelection;
            _gridView.CutRequested += CutSelection;
            _gridView.PasteRequested += PasteClipboard;
            _gridView.ClearRequested += ClearSelection;

            _searchController = new TableSearchController(rootVisualElement, _gridView);

            _inputRouter = new TableInputRouter(rootVisualElement, _gridView, _searchController);
            _inputRouter.SaveRequested += SaveDocument;
            _inputRouter.UndoRequested += UndoCommand;
            _inputRouter.RedoRequested += RedoCommand;
            _inputRouter.CopyRequested += CopySelection;
            _inputRouter.CutRequested += CutSelection;
            _inputRouter.PasteRequested += PasteClipboard;
            _inputRouter.DeleteRequested += DeleteSelection;
            _inputRouter.SearchOpenRequested += OpenSearch;

            _commandStack.Changed += OnCommandStackChanged;

            RegisterToolbarCallbacks();

            _gridView.SetUseFirstRowAsHeader(useFirstRowAsHeader);

            if (_headerToggle != null)
                _headerToggle.SetValueWithoutNotify(useFirstRowAsHeader);

            LoadDocument(assetPath);
        }

        private void OnDisable()
        {
            if (_inputRouter != null)
            {
                _inputRouter.SaveRequested -= SaveDocument;
                _inputRouter.UndoRequested -= UndoCommand;
                _inputRouter.RedoRequested -= RedoCommand;
                _inputRouter.CopyRequested -= CopySelection;
                _inputRouter.CutRequested -= CutSelection;
                _inputRouter.PasteRequested -= PasteClipboard;
                _inputRouter.DeleteRequested -= DeleteSelection;
                _inputRouter.SearchOpenRequested -= OpenSearch;
                _inputRouter.Dispose();
                _inputRouter = null;
            }

            _searchController?.Dispose();
            _searchController = null;

            if (_gridView != null)
            {
                _gridView.CommandRequested -= OnCommandRequested;
                _gridView.Selection.Changed -= UpdateStatusBar;
                _gridView.CopyRequested -= CopySelection;
                _gridView.CutRequested -= CutSelection;
                _gridView.PasteRequested -= PasteClipboard;
                _gridView.ClearRequested -= ClearSelection;
                _gridView.Dispose();
                _gridView = null;
            }

            _commandStack.Changed -= OnCommandStackChanged;

            UnregisterToolbarCallbacks();
        }

        /// <summary>
        /// 저장되지 않은 변경 사항을 파일에 기록합니다. 창을 닫을 때 Unity가 호출합니다.
        /// </summary>
        public override void SaveChanges()
        {
            SaveDocument();
            base.SaveChanges();
        }

        /// <summary>
        /// 저장되지 않은 변경 사항을 버립니다. 창을 닫을 때 Unity가 호출합니다.
        /// </summary>
        public override void DiscardChanges()
        {
            base.DiscardChanges();
        }

        /// <summary>
        /// 지정한 파일을 읽어 편집 대상으로 설정합니다.
        /// </summary>
        /// <param name="projectRelativePath">읽을 파일의 프로젝트 상대 경로입니다.</param>
        public void LoadDocument(string projectRelativePath)
        {
            assetPath = projectRelativePath ?? string.Empty;

            _document = string.IsNullOrEmpty(assetPath)
                ? CreateEmptyDocument()
                : TableDocumentIO.Load(assetPath);

            _loadedFileHash = TableDocumentIO.ComputeFileHash(assetPath);

            _commandStack.Clear();
            _gridView?.SetDocument(_document);
            _searchController?.Refresh();

            UpdateTitle();
            UpdateDirtyState();
            UpdateStatusBar();
        }

        private static TableDocument CreateEmptyDocument()
        {
            TableDocument document = new();
            document.SetContent(null);

            return document;
        }

        private void ShowLoadError()
        {
            rootVisualElement.Clear();

            Label label = new();
            label.text = $"UXML을 불러오지 못했습니다.\n{UxmlPath}";
            label.AddToClassList("table-editor__empty-message");
            rootVisualElement.Add(label);
        }

        private void CacheToolbarElements()
        {
            _saveButton = rootVisualElement.Q<ToolbarButton>("table-editor__save-button");
            _reloadButton = rootVisualElement.Q<ToolbarButton>("table-editor__reload-button");
            _searchButton = rootVisualElement.Q<ToolbarButton>("table-editor__search-button");
            _addRowButton = rootVisualElement.Q<Button>("table-editor__add-row-button");
            _addColumnButton = rootVisualElement.Q<Button>("table-editor__add-column-button");
            _headerToggle = rootVisualElement.Q<ToolbarToggle>("table-editor__header-toggle");
            _positionLabel = rootVisualElement.Q<Label>("table-editor__status-position");
            _sizeLabel = rootVisualElement.Q<Label>("table-editor__status-size");
            _stateLabel = rootVisualElement.Q<Label>("table-editor__status-state");
        }

        private void RegisterToolbarCallbacks()
        {
            if (_saveButton != null)
                _saveButton.clicked += SaveDocument;

            if (_reloadButton != null)
                _reloadButton.clicked += ReloadDocument;

            if (_searchButton != null)
                _searchButton.clicked += OpenSearch;

            if (_addRowButton != null)
                _addRowButton.clicked += AppendRow;

            if (_addColumnButton != null)
                _addColumnButton.clicked += AppendColumn;

            _headerToggle?.RegisterValueChangedCallback(OnHeaderToggleChanged);
        }

        private void UnregisterToolbarCallbacks()
        {
            if (_saveButton != null)
                _saveButton.clicked -= SaveDocument;

            if (_reloadButton != null)
                _reloadButton.clicked -= ReloadDocument;

            if (_searchButton != null)
                _searchButton.clicked -= OpenSearch;

            if (_addRowButton != null)
                _addRowButton.clicked -= AppendRow;

            if (_addColumnButton != null)
                _addColumnButton.clicked -= AppendColumn;

            _headerToggle?.UnregisterValueChangedCallback(OnHeaderToggleChanged);
        }

        private void OnHeaderToggleChanged(ChangeEvent<bool> evt)
        {
            useFirstRowAsHeader = evt.newValue;
            _gridView?.SetUseFirstRowAsHeader(evt.newValue);
            _searchController?.Refresh();
            UpdateStatusBar();
        }

        private void OnCommandRequested(ITableCommand command)
        {
            ExecuteCommand(command);
        }

        private void ExecuteCommand(ITableCommand command)
        {
            if (_document == null || command == null)
                return;

            _commandStack.Execute(_document, command);
            _searchController?.Refresh();
            UpdateStatusBar();
        }

        private void UndoCommand()
        {
            if (_document == null || !_commandStack.Undo(_document))
                return;

            _searchController?.Refresh();
            UpdateStatusBar();
        }

        private void RedoCommand()
        {
            if (_document == null || !_commandStack.Redo(_document))
                return;

            _searchController?.Refresh();
            UpdateStatusBar();
        }

        private void OnCommandStackChanged()
        {
            UpdateDirtyState();
            UpdateTitle();
            UpdateStatusBar();
        }

        private void SaveDocument()
        {
            if (_document == null || string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog(
                    "테이블 저장",
                    "저장할 파일 경로가 없습니다. 프로젝트 창에서 CSV 또는 TSV 파일을 열어 주세요.",
                    "확인");

                return;
            }

            if (!ConfirmExternalChange())
                return;

            TableDocumentIO.Save(_document);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            _loadedFileHash = TableDocumentIO.ComputeFileHash(assetPath);
            _commandStack.MarkSaved();
        }

        // 외부에서 파일이 바뀐 채로 덮어쓰면 다른 사람의 작업이 사라지므로 먼저 확인한다.
        private bool ConfirmExternalChange()
        {
            string currentHash = TableDocumentIO.ComputeFileHash(assetPath);

            if (string.IsNullOrEmpty(_loadedFileHash) || currentHash == _loadedFileHash)
                return true;

            return EditorUtility.DisplayDialog(
                "테이블 저장",
                "파일이 에디터 외부에서 변경되었습니다. 현재 편집 내용으로 덮어쓰겠습니까?",
                "덮어쓰기",
                "취소");
        }

        private void ReloadDocument()
        {
            if (_commandStack.IsDirty)
            {
                bool reload = EditorUtility.DisplayDialog(
                    "다시 불러오기",
                    "저장되지 않은 변경 사항을 버리겠습니까?",
                    "다시 불러오기",
                    "취소");

                if (!reload)
                    return;
            }

            LoadDocument(assetPath);
        }

        private void CopySelection()
        {
            if (_document == null || _gridView == null)
                return;

            TableClipboard.Copy(_document, _gridView.Selection);
        }

        private void CutSelection()
        {
            CopySelection();
            ClearSelection();
        }

        private void PasteClipboard()
        {
            if (_gridView == null)
                return;

            string[][] values = TableClipboard.ReadValues();

            if (values == null)
                return;

            CellSelection selection = _gridView.Selection;

            ExecuteCommand(new SetCellsCommand(
                "붙여넣기",
                selection.MinRow,
                selection.MinColumn,
                values));
        }

        private void ClearSelection()
        {
            if (_gridView == null)
                return;

            CellSelection selection = _gridView.Selection;

            ExecuteCommand(new SetCellsCommand(
                "범위 비우기",
                selection.MinRow,
                selection.MinColumn,
                TableClipboard.CreateEmptyValues(selection)));
        }

        private void DeleteSelection()
        {
            _gridView?.DeleteSelection();
        }

        private void OpenSearch()
        {
            _searchController?.Open();
        }

        private void AppendRow()
        {
            _gridView?.AppendRow();
        }

        private void AppendColumn()
        {
            _gridView?.AppendColumn();
        }

        private void UpdateDirtyState()
        {
            hasUnsavedChanges = _commandStack.IsDirty && !string.IsNullOrEmpty(assetPath);
            saveChangesMessage = "저장되지 않은 변경 사항이 있습니다. 저장하시겠습니까?";
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(assetPath)
                ? "새 테이블"
                : Path.GetFileName(assetPath);

            string suffix = _commandStack.IsDirty ? "*" : string.Empty;
            titleContent = new GUIContent($"{fileName}{suffix}");
        }

        private void UpdateStatusBar()
        {
            if (_gridView == null || _document == null)
                return;

            CellSelection selection = _gridView.Selection;

            if (_positionLabel != null)
            {
                string columnName = TableGridView.GetSpreadsheetColumnName(selection.Focus.Column);
                _positionLabel.text = $"{columnName}{selection.Focus.Row + 1}";
            }

            if (_sizeLabel != null)
                _sizeLabel.text = $"{_document.RowCount}행 x {_document.ColumnCount}열{DescribeSelection(selection)}";

            if (_stateLabel != null)
                _stateLabel.text = _commandStack.IsDirty ? "저장되지 않음" : string.Empty;
        }

        // Delete 키가 무엇을 지울지 미리 알 수 있도록 선택 종류를 상태 표시줄에 드러낸다.
        private static string DescribeSelection(CellSelection selection)
        {
            int rows = selection.MaxRow - selection.MinRow + 1;
            int columns = selection.MaxColumn - selection.MinColumn + 1;

            if (selection.Kind == CellSelectionKind.Rows)
                return $"   행 {rows}개 선택 · Delete로 삭제";

            if (selection.Kind == CellSelectionKind.Columns)
                return $"   열 {columns}개 선택 · Delete로 삭제";

            if (selection.IsSingleCell)
                return string.Empty;

            return $"   선택 {rows} x {columns}";
        }
    }
}
