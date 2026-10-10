using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// 테마는 창 오른쪽 위 ⋮ 메뉴(<see cref="IHasCustomMenu"/>)에서 바꿉니다.
    /// </summary>
    public sealed class TableEditorWindow : EditorWindow, IHasCustomMenu
    {
        private const string UxmlPath =
            "Packages/com.nkstudio.unitytable/Editor/Window/TableEditorWindow.uxml";

        private const string ExternalChangeBarHiddenClassName = "table-editor__external-change-bar--hidden";
        private const string StatusLabelClickableClassName = "table-editor__status-label--clickable";

        // 작은 파일은 한 프레임 안에 끝나므로, 그보다 오래 걸릴 때만 안내를 띄워 깜박임을 막는다.
        private const long LoadingOverlayDelayMs = 150;

        [SerializeField]
        private string assetPath = string.Empty;

        private readonly TableCommandStack _commandStack = new();

        private TableDocument _document;
        private TableGridView _gridView;
        private TableSearchController _searchController;
        private TableInputRouter _inputRouter;

        private ToolbarButton _searchButton;
        private Button _addRowButton;
        private Button _addColumnButton;
        private Label _positionLabel;
        private Label _sizeLabel;
        private Label _stateLabel;
        private Label _encodingLabel;
        private Label _newLineLabel;
        private Label _delimiterLabel;

        private string _loadedFileHash = string.Empty;

        // 불러오기를 시작할 때마다 올린다. 끝난 작업의 번호가 다르면 그 사이 다른 파일을 열었다는 뜻이라 결과를 버린다.
        private int _loadVersion;
        private bool _isLoading;
        private Label _loadingOverlay;
        private IVisualElementScheduledItem _loadingOverlayReveal;

        // 에디터 밖(Finder·다른 편집기)에서 파일이 바뀌는 것을 감시한다.
        private TableFileWatcher _fileWatcher;

        // 다시 읽는 동안 들어온 변경은 다 읽은 뒤 한 번 더 확인한다.
        private bool _isRecheckPendingAfterLoad;

        // 알림 바로 이미 알린 파일 해시다. 같은 변경으로 바를 다시 띄우지 않는다.
        private string _notifiedFileHash;

        private TableFileFormatDialog _formatDialog;
        private TableGoToBar _goToBar;

        // 이 파일의 에셋 경로 미리보기 켜짐 여부(.meta). 실제로는 Preferences의 전역 설정도 켜져 있어야 보인다.
        private bool _isFileAssetPreviewEnabled = true;

        private VisualElement _externalChangeBar;
        private Button _externalReloadButton;
        private Button _externalDismissButton;

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

            // UXML의 "@키" 문구를 현재 언어로 채운다. 언어가 바뀌면 ApplyLanguage가 다시 그린다.
            Localization.Localize(rootVisualElement);

            VisualElement gridContainer = rootVisualElement.Q<VisualElement>("table-editor__grid-container");

            if (gridContainer == null)
            {
                ShowLoadError();
                return;
            }

            CacheToolbarElements();
            CacheExternalChangeBar();
            CreateFormatDialog();
            ApplyTheme();

            _gridView = new TableGridView(gridContainer);
            _gridView.CommandRequested += OnCommandRequested;
            _gridView.Selection.Changed += UpdateStatusBar;
            _gridView.CopyRequested += CopySelection;
            _gridView.CutRequested += CutSelection;
            _gridView.PasteRequested += PasteClipboard;
            _gridView.ClearRequested += ClearSelection;
            _gridView.HeaderRowsRequested += OnHeaderRowsRequested;

            _searchController = new TableSearchController(rootVisualElement, _gridView);
            _searchController.CommandRequested += OnCommandRequested;

            _inputRouter = new TableInputRouter(rootVisualElement, _gridView, _searchController);
            _inputRouter.SaveRequested += SaveDocument;
            _inputRouter.UndoRequested += UndoCommand;
            _inputRouter.RedoRequested += RedoCommand;
            _inputRouter.CopyRequested += CopySelection;
            _inputRouter.CutRequested += CutSelection;
            _inputRouter.PasteRequested += PasteClipboard;
            _inputRouter.DeleteRequested += DeleteSelection;
            _inputRouter.SearchOpenRequested += OpenSearch;
            _inputRouter.GoToRequested += OpenGoTo;

            _goToBar = new TableGoToBar(gridContainer);
            _goToBar.Submitted += OnGoToSubmitted;
            _goToBar.Closed += OnGoToClosed;

            _commandStack.Changed += OnCommandStackChanged;

            RegisterToolbarCallbacks();

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

        private void OnEnable()
        {
            TableEditorTheme.Changed += ApplyTheme;
            TableEditorSettings.Changed += OnSettingsChanged;
            Localization.Changed += ApplyLanguage;
        }

        // 묶어 둔 문구(UXML·대화상자·열 메뉴·검색 바)를 다시 그리고, 그때그때 만드는 문구(탭 제목·상태 표시줄·검색 개수)를 새로 쓴다.
        // 우클릭·⋮ 메뉴는 열 때마다 만들므로 따로 할 일이 없다.
        private void ApplyLanguage()
        {
            Localization.Refresh(rootVisualElement);
            _formatDialog?.Close();
            _goToBar?.Close();
            _searchController?.RefreshLanguage();
            UpdateTitle();
            UpdateDirtyState();
            UpdateStatusBar();
        }

        // 글꼴 크기는 Preferences와 Ctrl/Cmd+휠 줌이 함께 바꾼다. 다른 설정(자동 맞춤·새 CSV)은 쓸 때마다 읽으므로 따로 할 일이 없다.
        private void OnSettingsChanged()
        {
            _gridView?.ApplyFontSize(TableEditorSettings.FontSize);
            ApplyAssetPathPreview();
        }

        // 전역(Preferences)과 이 파일(.meta)이 모두 켜져 있어야 에셋 경로 미리보기를 보인다.
        private void ApplyAssetPathPreview()
        {
            _gridView?.ApplyAssetPathPreview(TableEditorSettings.AssetPathPreview && _isFileAssetPreviewEnabled);
        }

        private void ToggleFileAssetPreview()
        {
            _isFileAssetPreviewEnabled = _isFileAssetPreviewEnabled == false;

            // 다른 도구가 .meta userData를 쓰고 있으면 기록하지 못한다(경고는 TableAssetSettings가 남긴다). 그래도 이번 세션에는 반영한다.
            TableAssetSettings.SaveAssetPreviewEnabled(assetPath, _isFileAssetPreviewEnabled);
            ApplyAssetPathPreview();
        }

        private void OnDisable()
        {
            TableEditorTheme.Changed -= ApplyTheme;
            TableEditorSettings.Changed -= OnSettingsChanged;
            Localization.Changed -= ApplyLanguage;

            _fileWatcher?.Dispose();
            _fileWatcher = null;

            if (_formatDialog != null)
            {
                _formatDialog.ReopenRequested -= OnFormatReopenRequested;
                _formatDialog.ApplyRequested -= OnFormatApplyRequested;
                _formatDialog.Closed -= OnFormatDialogClosed;
                _formatDialog.Dispose();
                _formatDialog = null;
            }

            UnregisterFormatLabelCallbacks();

            if (_goToBar != null)
            {
                _goToBar.Submitted -= OnGoToSubmitted;
                _goToBar.Closed -= OnGoToClosed;
                _goToBar.Dispose();
                _goToBar = null;
            }

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
                _inputRouter.GoToRequested -= OpenGoTo;
                _inputRouter.Dispose();
                _inputRouter = null;
            }

            if (_searchController != null)
            {
                _searchController.CommandRequested -= OnCommandRequested;
                _searchController.Dispose();
                _searchController = null;
            }

            if (_gridView != null)
            {
                _gridView.CommandRequested -= OnCommandRequested;
                _gridView.Selection.Changed -= UpdateStatusBar;
                _gridView.CopyRequested -= CopySelection;
                _gridView.CutRequested -= CutSelection;
                _gridView.PasteRequested -= PasteClipboard;
                _gridView.ClearRequested -= ClearSelection;
                _gridView.HeaderRowsRequested -= OnHeaderRowsRequested;
                _gridView.Dispose();
                _gridView = null;
            }

            _commandStack.Changed -= OnCommandStackChanged;

            UnregisterToolbarCallbacks();

            if (_externalReloadButton != null)
                _externalReloadButton.clicked -= ReloadFromDisk;

            if (_externalDismissButton != null)
                _externalDismissButton.clicked -= HideExternalChangeBar;
        }

        /// <summary>
        /// 창 오른쪽 위 ⋮ 메뉴에 테마·언어 선택과 환경 설정(Preferences > Tabular Editor) 열기 항목을 추가합니다.
        /// </summary>
        public void AddItemsToMenu(GenericMenu menu)
        {
            string theme = Localization.Get("menu.theme");

            foreach (TableEditorThemeStyle style in Enum.GetValues(typeof(TableEditorThemeStyle)))
            {
                menu.AddItem(
                    new GUIContent($"{theme}/{TableEditorTheme.DisplayName(style)}"),
                    TableEditorTheme.Style == style,
                    () => TableEditorTheme.Style = style);
            }

            // 언어 이름은 그 언어로 적어, 지금 언어를 읽지 못해도 자기 언어를 찾을 수 있게 한다.
            string language = Localization.Get("menu.language");

            foreach (Language option in Enum.GetValues(typeof(Language)))
            {
                menu.AddItem(
                    new GUIContent($"{language}/{Localization.DisplayName(option)}"),
                    Localization.Current == option,
                    () => Localization.Current = option);
            }

            // 이 파일만 끄고 켠다(.meta에 저장해 팀과 공유). 전역 설정이 꺼져 있으면 고를 수 없다.
            if (string.IsNullOrEmpty(assetPath) == false)
            {
                GUIContent fileAssetPreview = new(Localization.Get("menu.assetPreviewForFile"));

                if (TableEditorSettings.AssetPathPreview)
                    menu.AddItem(fileAssetPreview, _isFileAssetPreviewEnabled, ToggleFileAssetPreview);
                else
                    menu.AddDisabledItem(fileAssetPreview, _isFileAssetPreviewEnabled);
            }

            // 글꼴 크기·자동 맞춤·새 CSV 형식 등 나머지 설정은 Preferences 페이지에 있다.
            menu.AddItem(
                new GUIContent(Localization.Get("menu.preferences")),
                false,
                () => SettingsService.OpenUserPreferences(TableEditorPreferences.Path));
        }

        private void ApplyTheme()
        {
            TableEditorTheme.Apply(rootVisualElement.Q<VisualElement>("table-editor"));
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
            LoadDocument(projectRelativePath, false);
        }

        // preserveView는 같은 파일을 다시 읽을 때다. 불러오는 중 안내를 띄우지 않고 선택·스크롤·열 폭을 유지한다.
        // forcedOptions는 파일 형식 대화상자의 '다시 열기'다. .meta 대신 그 형식으로 읽는다.
        private void LoadDocument(string projectRelativePath, bool preserveView, TableFileOptions forcedOptions = null)
        {
            assetPath = projectRelativePath ?? string.Empty;
            int version = ++_loadVersion;

            WatchFile(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                ApplyLoadedDocument(CreateEmptyDocument(), string.Empty, false, null);
                return;
            }

            // .meta는 Unity API라 메인 스레드에서 미리 읽어 넘긴다.
            TableFileFormatOverride format = forcedOptions != null
                ? new TableFileFormatOverride
                {
                    EncodingCodePage = forcedOptions.Encoding.CodePage,
                    Delimiter = forcedOptions.Delimiter,
                    Quote = forcedOptions.Quote,
                    QuoteMode = forcedOptions.QuoteMode,
                }
                : TableAssetSettings.LoadFileFormat(assetPath);

            BeginLoading(preserveView == false);
            _ = LoadDocumentInBackground(assetPath, version, preserveView, format, forcedOptions);
        }

        // await 뒤의 코드는 Unity 동기화 컨텍스트를 통해 메인 스레드에서 이어진다.
        private async Task LoadDocumentInBackground(
            string path,
            int version,
            bool preserveView,
            TableFileFormatOverride format,
            TableFileOptions forcedOptions)
        {
            TableDocument document;
            string fileHash;

            try
            {
                (document, fileHash) = await Task.Run(() =>
                {
                    TableDocument loaded = TableDocumentIO.Load(path, format, out string hash);
                    return (loaded, hash);
                });
            }
            catch (Exception exception)
            {
                if (this == null || version != _loadVersion)
                    return;

                Debug.LogException(exception);
                EndLoading();
                ShowLoadingMessage(Localization.Format("window.loadFailed", path));
                return;
            }

            // 그 사이 창이 닫혔거나 다른 파일을 열었으면 이 결과는 버린다.
            if (this == null || version != _loadVersion)
                return;

            ApplyLoadedDocument(document, fileHash, preserveView, forcedOptions);
        }

        // 불러오는 동안에는 문서를 비워 둔다. 저장·편집·Undo가 모두 문서가 없으면 아무 것도 하지 않으므로,
        // 다 읽기 전에 Ctrl+S를 눌러 빈 내용으로 파일을 덮어쓰는 일이 생기지 않는다.
        // 다시 읽기(showOverlay=false)에서는 그리드가 이전 내용을 계속 보여 주다가 새 내용으로 바로 바뀐다.
        private void BeginLoading(bool showOverlay)
        {
            _isLoading = true;
            _document = null;
            _commandStack.Clear();

            UpdateTitle();
            UpdateDirtyState();

            if (showOverlay)
                ShowLoadingOverlayLater();
        }

        private void EndLoading()
        {
            _isLoading = false;
            _loadingOverlayReveal?.Pause();

            if (_loadingOverlay != null)
                _loadingOverlay.style.display = DisplayStyle.None;
        }

        private void ApplyLoadedDocument(TableDocument document, string fileHash, bool preserveView, TableFileOptions forcedOptions)
        {
            EndLoading();

            // '다시 열기'로 고른 개행·마지막 줄바꿈·BOM 여부는 다음 저장에 쓴다. 파일에 BOM이 있었으면 그 인코딩이 맞으므로 그대로 둔다.
            if (forcedOptions != null)
            {
                bool hasBom = document.FileOptions.Encoding.GetPreamble().Length > 0;

                if (hasBom == false)
                    document.FileOptions.Encoding = forcedOptions.Encoding;

                document.FileOptions.NewLine = forcedOptions.NewLine;
                document.FileOptions.EndsWithNewLine = forcedOptions.EndsWithNewLine;
            }

            _document = document;
            _loadedFileHash = fileHash;
            _commandStack.Clear();
            HideExternalChangeBar();

            BindDocumentToViews(preserveView);

            // 에디터가 뒤에 있어도 바뀐 내용이 바로 보이게 다시 그린다.
            Repaint();

            if (_isRecheckPendingAfterLoad)
            {
                _isRecheckPendingAfterLoad = false;
                OnFileChangedExternally();
            }
        }

        private void BindDocumentToViews(bool preserveView = false)
        {
            // 문서를 붙이기 전에 정해야, 꺼 둔 파일에서 에셋 경로 열을 괜히 가리지 않는다.
            _isFileAssetPreviewEnabled = TableAssetSettings.LoadAssetPreviewEnabled(assetPath);
            ApplyAssetPathPreview();

            _gridView?.SetDocument(_document, preserveView);
            _gridView?.SetHeaderRowCount(TableAssetSettings.LoadHeaderRowCount(assetPath));
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
                .Execute(() => ShowLoadingMessage(Localization.Get("window.loading")))
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
            label.text = Localization.Format("window.uxmlFailed", UxmlPath);
            label.AddToClassList("table-editor__empty-message");
            rootVisualElement.Add(label);
        }

        private void CacheToolbarElements()
        {
            _searchButton = rootVisualElement.Q<ToolbarButton>("table-editor__search-button");
            _addRowButton = rootVisualElement.Q<Button>("table-editor__add-row-button");
            _addColumnButton = rootVisualElement.Q<Button>("table-editor__add-column-button");
            _positionLabel = rootVisualElement.Q<Label>("table-editor__status-position");
            _sizeLabel = rootVisualElement.Q<Label>("table-editor__status-size");
            _stateLabel = rootVisualElement.Q<Label>("table-editor__status-state");
            _encodingLabel = rootVisualElement.Q<Label>("table-editor__status-encoding");
            _newLineLabel = rootVisualElement.Q<Label>("table-editor__status-newline");
            _delimiterLabel = rootVisualElement.Q<Label>("table-editor__status-delimiter");
        }

        private void RegisterToolbarCallbacks()
        {
            if (_searchButton != null)
                _searchButton.clicked += OpenSearch;

            if (_addRowButton != null)
                _addRowButton.clicked += AppendRow;

            if (_addColumnButton != null)
                _addColumnButton.clicked += AppendColumn;
        }

        private void UnregisterToolbarCallbacks()
        {
            if (_searchButton != null)
                _searchButton.clicked -= OpenSearch;

            if (_addRowButton != null)
                _addRowButton.clicked -= AppendRow;

            if (_addColumnButton != null)
                _addColumnButton.clicked -= AppendColumn;
        }

        // 헤더 행 수는 파일의 .meta에 기록해 팀원과 공유한다. 다른 도구의 userData가 있어 기록하지 못하면 반영하지 않는다.
        private void OnHeaderRowsRequested(int headerRowCount)
        {
            if (TableAssetSettings.SaveHeaderRowCount(assetPath, headerRowCount) == false)
                return;

            _gridView?.SetHeaderRowCount(headerRowCount);
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
                    Localization.Get("window.saveTitle"),
                    Localization.Get("window.noPath"),
                    Localization.Get("common.ok"));

                return;
            }

            if (!ConfirmExternalChange())
                return;

            TableDocumentIO.Save(_document);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            _loadedFileHash = TableDocumentIO.ComputeFileHash(assetPath);
            _commandStack.MarkSaved();

            // 이제 파일이 이 형식으로 저장됐으니, 파일에서 다시 알아낼 수 없는 부분(레거시 인코딩·구분 기호·따옴표)을 .meta에 남긴다.
            TableAssetSettings.SaveFileFormat(assetPath, TableFileFormatOverride.From(_document.FileOptions, _document.Format));

            // 덮어쓰기로 결정했으니 바깥 변경 알림은 더 이상 의미가 없다.
            HideExternalChangeBar();
        }

        // 상태 표시줄의 인코딩·개행·구분 기호를 누르면 파일 형식 대화상자를 연다.
        private void CreateFormatDialog()
        {
            VisualElement themeRoot = rootVisualElement.Q<VisualElement>("table-editor");

            if (themeRoot == null)
                return;

            // 테마 클래스가 붙는 요소 안에 두어야 Unity·Android Studio 색을 그대로 받는다.
            _formatDialog = new TableFileFormatDialog(themeRoot);
            _formatDialog.ReopenRequested += OnFormatReopenRequested;
            _formatDialog.ApplyRequested += OnFormatApplyRequested;
            _formatDialog.Closed += OnFormatDialogClosed;

            foreach (Label label in FormatLabels())
            {
                label.AddToClassList(StatusLabelClickableClassName);
                Localization.BindTooltip(label, "status.changeFormatTip");
                label.RegisterCallback<ClickEvent>(OnFormatLabelClicked);
            }
        }

        private void UnregisterFormatLabelCallbacks()
        {
            foreach (Label label in FormatLabels())
                label.UnregisterCallback<ClickEvent>(OnFormatLabelClicked);
        }

        private IEnumerable<Label> FormatLabels()
        {
            if (_encodingLabel != null)
                yield return _encodingLabel;

            if (_newLineLabel != null)
                yield return _newLineLabel;

            if (_delimiterLabel != null)
                yield return _delimiterLabel;
        }

        private void OnFormatLabelClicked(ClickEvent evt)
        {
            if (_document == null || _formatDialog == null)
                return;

            _gridView?.CommitEdit();

            if (_inputRouter != null)
                _inputRouter.IsModalOpen = true;

            _formatDialog.Open(_document.FileOptions, _document.Format);
        }

        private void OnFormatDialogClosed()
        {
            if (_inputRouter != null)
                _inputRouter.IsModalOpen = false;

            _gridView?.FocusGrid();
        }

        // 다시 열기: 고른 형식으로 파일을 다시 읽는다. 다시 연 형식은 .meta에 남겨 다음에도(팀원도) 같은 형식으로 읽게 한다.
        private void OnFormatReopenRequested(TableFileOptions options)
        {
            if (_document == null || string.IsNullOrEmpty(assetPath))
                return;

            if (_commandStack.IsDirty)
            {
                bool reopen = EditorUtility.DisplayDialog(
                    Localization.Get("dialog.reopen"),
                    Localization.Get("window.reopenBody"),
                    Localization.Get("dialog.reopen"),
                    Localization.Get("common.cancel"));

                if (reopen == false)
                    return;
            }

            TableAssetSettings.SaveFileFormat(assetPath, TableFileFormatOverride.From(options, _document.Format));
            LoadDocument(assetPath, false, options);
        }

        // 적용: 셀 값은 그대로 두고 저장할 형식만 바꾼다. Undo할 수 있고, 저장할 때 파일과 .meta에 반영된다.
        private void OnFormatApplyRequested(TableFileOptions options)
        {
            ExecuteCommand(new ChangeFileOptionsCommand(options));
        }

        private void WatchFile(string projectRelativePath)
        {
            string fullPath = string.IsNullOrEmpty(projectRelativePath) ? string.Empty : Path.GetFullPath(projectRelativePath);

            if (_fileWatcher != null && _fileWatcher.FullPath == fullPath)
                return;

            _fileWatcher?.Dispose();
            _fileWatcher = null;

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return;

            _fileWatcher = new TableFileWatcher(fullPath, OnFileChangedExternally);
        }

        // 에디터 밖에서 파일이 바뀌었다. 잃을 편집이 없으면 화면을 유지한 채 다시 읽고, 있으면 알림 바로 묻는다.
        private void OnFileChangedExternally()
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (_isLoading)
            {
                _isRecheckPendingAfterLoad = true;
                return;
            }

            string fileHash;

            try
            {
                fileHash = TableDocumentIO.ComputeFileHash(assetPath);
            }
            catch (IOException)
            {
                // 다른 프로그램이 아직 쓰는 중이라 읽을 수 없다. 조금 뒤 다시 본다.
                _fileWatcher?.RequestRecheck();
                return;
            }

            // 지워졌거나 이름이 바뀌어 사라진 순간이다. 다시 생기면 그때 반영한다.
            if (string.IsNullOrEmpty(fileHash))
                return;

            bool isContentChanged = fileHash != _loadedFileHash;
            bool isEditing = _gridView != null && _gridView.IsEditing;

            switch (ExternalChangeDecision.Decide(isContentChanged, _commandStack.IsDirty, isEditing))
            {
                case ExternalChangeAction.Ignore:
                    // 바깥에서 원래 내용으로 되돌렸으면 띄워 둔 알림도 거둔다.
                    HideExternalChangeBar();
                    return;

                case ExternalChangeAction.Reload:
                    LoadDocument(assetPath, true);
                    return;

                case ExternalChangeAction.Notify:
                    if (fileHash == _notifiedFileHash)
                        return;

                    _notifiedFileHash = fileHash;
                    ShowExternalChangeBar();
                    return;
            }
        }

        private void CacheExternalChangeBar()
        {
            _externalChangeBar = rootVisualElement.Q<VisualElement>("table-editor__external-change-bar");
            _externalReloadButton = rootVisualElement.Q<Button>("table-editor__external-change-reload-button");
            _externalDismissButton = rootVisualElement.Q<Button>("table-editor__external-change-dismiss-button");

            if (_externalReloadButton != null)
                _externalReloadButton.clicked += ReloadFromDisk;

            if (_externalDismissButton != null)
                _externalDismissButton.clicked += HideExternalChangeBar;
        }

        private void ShowExternalChangeBar()
        {
            _externalChangeBar?.RemoveFromClassList(ExternalChangeBarHiddenClassName);
            Repaint();
        }

        // 무시해도 _loadedFileHash는 그대로라, 저장할 때 ConfirmExternalChange가 덮어쓸지 다시 묻는다.
        private void HideExternalChangeBar()
        {
            _notifiedFileHash = null;
            _externalChangeBar?.AddToClassList(ExternalChangeBarHiddenClassName);
        }

        // 알림 바의 [다시 불러오기]: 저장 안 한 편집을 버리고 파일 내용으로 바꾼다.
        private void ReloadFromDisk()
        {
            _gridView?.CancelEdit();
            LoadDocument(assetPath, true);
        }

        // 외부에서 파일이 바뀐 채로 덮어쓰면 다른 사람의 작업이 사라지므로 먼저 확인한다.
        private bool ConfirmExternalChange()
        {
            string currentHash = TableDocumentIO.ComputeFileHash(assetPath);

            if (string.IsNullOrEmpty(_loadedFileHash) || currentHash == _loadedFileHash)
                return true;

            return EditorUtility.DisplayDialog(
                Localization.Get("window.saveTitle"),
                Localization.Get("window.overwriteBody"),
                Localization.Get("window.overwrite"),
                Localization.Get("common.cancel"));
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
                Localization.Get("undo.paste"),
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
                Localization.Get("undo.clearRange"),
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

        // 파일 형식 대화상자처럼 열려 있는 동안은 키 입력을 그리드로 보내지 않는다(입력칸이 Enter·Esc를 직접 받는다).
        private void OpenGoTo()
        {
            if (_document == null || _gridView == null || _goToBar == null)
                return;

            _gridView.CommitEdit();

            if (_inputRouter != null)
                _inputRouter.IsModalOpen = true;

            _goToBar.Open(_document.RowCount, _document.ColumnCount, _gridView.Selection.Anchor.Column);
        }

        private void OnGoToSubmitted(CellCoord target)
        {
            _gridView?.SetActiveCell(target, false);
        }

        // TRAP: Enter는 키 이벤트와 문자('\n') 이벤트가 따로 온다. 키 이벤트에서 닫으며 곧바로 표에 포커스를 주면
        // 뒤이은 문자 이벤트가 표의 숨은 편집칸에 들어가 셀 편집이 시작된다. 포커스는 다음 프레임에 돌려준다.
        private void OnGoToClosed()
        {
            if (_inputRouter != null)
                _inputRouter.IsModalOpen = false;

            rootVisualElement.schedule.Execute(() => _gridView?.FocusGrid()).ExecuteLater(0);
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
            saveChangesMessage = Localization.Get("window.unsavedChanges");
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(assetPath)
                ? Localization.Get("window.newTable")
                : Path.GetFileName(assetPath);

            // 저장 안 한 표시(*)는 붙이지 않는다. hasUnsavedChanges가 켜지면 Unity 탭이 스스로 '*'를 붙이므로,
            // 여기서도 붙이면 '**'로 두 번 보인다.
            titleContent = new GUIContent(fileName);
        }

        private void UpdateStatusBar()
        {
            if (_gridView == null || _document == null)
                return;

            CellSelection selection = _gridView.Selection;

            if (_sizeLabel != null)
                _sizeLabel.text = Localization.Format(
                    "status.size",
                    Localization.Count("count.row", _document.RowCount),
                    Localization.Count("count.column", _document.ColumnCount));

            if (_positionLabel != null)
                {
                // 열 제목(글자)과 행 번호(숫자)를 그대로 이어 쓴 셀 주소다(예: C12). Ctrl/Cmd+G에 그대로 넣을 수 있다.
                string address = TableGridView.GetSpreadsheetColumnName(selection.Anchor.Column) + (selection.Anchor.Row + 1);
                _positionLabel.text = $"{address} ({DescribeSelection(selection)})";
            }

            if (_stateLabel != null)
                _stateLabel.text = _commandStack.IsDirty ? Localization.Get("status.unsaved") : string.Empty;

            if (_encodingLabel != null)
                _encodingLabel.text = TableFormatUtility.DescribeEncoding(_document.FileOptions?.Encoding);

            if (_newLineLabel != null)
                _newLineLabel.text = TableFormatUtility.DescribeNewLine(_document.FileOptions?.NewLine);

            if (_delimiterLabel != null)
                _delimiterLabel.text = TableFormatUtility.DescribeDelimiter(_document.FileOptions);
        }

        // 셀 하나면 그 셀의 글자 수, 범위면 셀 개수를 보여 준다(예: "4 chars", "54 cells").
        // 행·열 전체를 고른 경우에는 Delete 키가 무엇을 지울지 미리 알 수 있도록 덧붙인다.
        private string DescribeSelection(CellSelection selection)
        {
            int rows = selection.MaxRow - selection.MinRow + 1;
            int columns = selection.MaxColumn - selection.MinColumn + 1;

            if (selection.Kind == CellSelectionKind.Rows)
                return Localization.Format("status.deleteHint", Localization.Count("count.selectedRow", rows));

            if (selection.Kind == CellSelectionKind.Columns)
                return Localization.Format("status.deleteHint", Localization.Count("count.selectedColumn", columns));

            if (selection.IsSingleCell == false)
                return Localization.Count("count.cell", rows * columns);

            // 한글·이모지 결합 문자를 한 글자로 센다.
            string value = _document.GetCell(selection.Anchor.Row, selection.Anchor.Column);
            return Localization.Count("count.char", new StringInfo(value).LengthInTextElements);
        }
    }
}
