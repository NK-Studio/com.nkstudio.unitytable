using System;
using System.IO;
using System.Threading.Tasks;
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

        // 작은 파일은 한 프레임 안에 끝나므로, 그보다 오래 걸릴 때만 안내를 띄워 깜박임을 막는다.
        private const long LoadingOverlayDelayMs = 150;

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

        // 불러오기를 시작할 때마다 올린다. 끝난 작업의 번호가 다르면 그 사이 다른 파일을 열었다는 뜻이라 결과를 버린다.
        private int _loadVersion;
        private bool _isLoading;
        private Label _loadingOverlay;
        private IVisualElementScheduledItem _loadingOverlayReveal;

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

            CreateLoadingOverlay(gridContainer);

            // Open()이 CreateGUI보다 먼저 불러오기를 시작하므로, 이미 진행 중이거나 끝났으면 다시 읽지 않는다.
            // 도메인 리로드 뒤에는 문서가 비어 있으므로 여기서 다시 읽는다.
            if (_isLoading)
                ShowLoadingOverlayLater();
            else if (_document != null)
                BindDocumentToViews();
            else
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
        /// 지정한 파일을 읽어 편집 대상으로 설정합니다. 파일 읽기와 파싱은 백그라운드 스레드에서 하므로
        /// 이 메서드는 바로 반환되고, 문서는 다 읽은 뒤 메인 스레드에서 적용된다.
        /// </summary>
        /// <param name="projectRelativePath">읽을 파일의 프로젝트 상대 경로입니다.</param>
        public void LoadDocument(string projectRelativePath)
        {
            assetPath = projectRelativePath ?? string.Empty;
            int version = ++_loadVersion;

            if (string.IsNullOrEmpty(assetPath))
            {
                ApplyLoadedDocument(CreateEmptyDocument(), string.Empty);
                return;
            }

            BeginLoading();
            _ = LoadDocumentInBackground(assetPath, version);
        }

        // await 뒤의 코드는 Unity 동기화 컨텍스트를 통해 메인 스레드에서 이어진다.
        private async Task LoadDocumentInBackground(string path, int version)
        {
            TableDocument document;
            string fileHash;

            try
            {
                (document, fileHash) = await Task.Run(() =>
                {
                    TableDocument loaded = TableDocumentIO.Load(path, out string hash);
                    return (loaded, hash);
                });
            }
            catch (Exception exception)
            {
                if (this == null || version != _loadVersion)
                    return;

                Debug.LogException(exception);
                EndLoading();
                ShowLoadingMessage($"파일을 불러오지 못했습니다.\n{path}");
                return;
            }

            // 그 사이 창이 닫혔거나 다른 파일을 열었으면 이 결과는 버린다.
            if (this == null || version != _loadVersion)
                return;

            ApplyLoadedDocument(document, fileHash);
        }

        // 불러오는 동안에는 문서를 비워 둔다. 저장·편집·Undo가 모두 문서가 없으면 아무 것도 하지 않으므로,
        // 다 읽기 전에 Ctrl+S를 눌러 빈 내용으로 파일을 덮어쓰는 일이 생기지 않는다.
        private void BeginLoading()
        {
            _isLoading = true;
            _document = null;
            _commandStack.Clear();

            UpdateTitle();
            UpdateDirtyState();
            ShowLoadingOverlayLater();
        }

        private void EndLoading()
        {
            _isLoading = false;
            _loadingOverlayReveal?.Pause();

            if (_loadingOverlay != null)
                _loadingOverlay.style.display = DisplayStyle.None;
        }

        private void ApplyLoadedDocument(TableDocument document, string fileHash)
        {
            EndLoading();

            _document = document;
            _loadedFileHash = fileHash;
            _commandStack.Clear();

            BindDocumentToViews();
        }

        private void BindDocumentToViews()
        {
            _gridView?.SetDocument(_document);
            _searchController?.Refresh();

            UpdateTitle();
            UpdateDirtyState();
            UpdateStatusBar();
        }

        // 그리드 위를 덮어 불러오는 동안 셀을 누르지 못하게 한다.
        private void CreateLoadingOverlay(VisualElement gridContainer)
        {
            _loadingOverlay = new Label();
            _loadingOverlay.AddToClassList("table-editor__loading-overlay");
            _loadingOverlay.style.display = DisplayStyle.None;
            gridContainer.Add(_loadingOverlay);
        }

        private void ShowLoadingOverlayLater()
        {
            if (_loadingOverlay == null)
                return;

            _loadingOverlayReveal?.Pause();
            _loadingOverlayReveal = _loadingOverlay.schedule
                .Execute(() => ShowLoadingMessage("불러오는 중…"))
                .StartingIn(LoadingOverlayDelayMs);
        }

        private void ShowLoadingMessage(string message)
        {
            if (_loadingOverlay == null)
                return;

            _loadingOverlay.text = message;
            _loadingOverlay.style.display = DisplayStyle.Flex;
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
            // 불러오는 중이거나 불러오지 못한 상태다. 저장할 내용이 없다.
            if (_document == null)
                return;

            if (string.IsNullOrEmpty(assetPath))
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
