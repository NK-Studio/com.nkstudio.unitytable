using System;
using System.Collections.Generic;
using System.Text;
using NKStudio.TabularEditor.Data;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 상태 표시줄의 인코딩·개행·구분 기호를 누르면 뜨는 파일 형식 대화상자입니다(SmoothCSV 방식, 창 안 오버레이).
    /// <list type="bullet">
    /// <item>다시 열기: 고른 형식으로 파일을 디스크에서 다시 읽는다. 저장하지 않은 편집은 버린다(호출한 쪽이 확인).</item>
    /// <item>적용: 셀 값은 그대로 두고 저장할 형식만 바꾼다. 저장해야 파일에 반영된다.</item>
    /// </list>
    /// </summary>
    public sealed class TableFileFormatDialog : IDisposable
    {
        private const string OverlayClassName = "table-editor__dialog-overlay";
        private const string HiddenClassName = "table-editor__dialog-overlay--hidden";
        private const string OtherChoice = TableFormatChoices.OtherLabel;

        private static readonly (string Label, char Value)[] DelimiterChoices = TableFormatChoices.Delimiters;
        private static readonly (string Label, char Value)[] QuoteChoices = TableFormatChoices.Quotes;
        private static readonly string[] QuoteModeChoices = TableFormatChoices.QuoteModes;
        private static readonly (string Label, string Value)[] NewLineChoices = TableFormatChoices.NewLines;

        private readonly VisualElement _overlay;
        private readonly VisualElement _card;
        private readonly Label _title;
        private readonly DropdownField _encodingField;
        private readonly DropdownField _delimiterField;
        private readonly TextField _delimiterOtherField;
        private readonly DropdownField _quoteField;
        private readonly TextField _quoteOtherField;
        private readonly DropdownField _quoteModeField;
        private readonly DropdownField _newLineField;
        private readonly Toggle _endsWithNewLineToggle;
        private readonly Button _closeButton;
        private readonly Button _cancelButton;
        private readonly Button _reopenButton;
        private readonly Button _applyButton;

        // 지금 고를 수 있는 인코딩이다. 목록에 없는 인코딩으로 읽은 파일이면 그 인코딩을 맨 앞에 더해 그대로 고를 수 있게 한다.
        private readonly List<Func<Encoding>> _encodingFactories = new();

        /// <summary>
        /// 대화상자를 만들어 root 위에 숨겨 둡니다.
        /// </summary>
        public TableFileFormatDialog(VisualElement root)
        {
            _overlay = new VisualElement();
            _overlay.AddToClassList(OverlayClassName);
            _overlay.AddToClassList(HiddenClassName);

            _card = new VisualElement();
            _card.AddToClassList("table-editor__dialog");
            _card.focusable = true;
            _card.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _overlay.Add(_card);

            VisualElement header = new();
            header.AddToClassList("table-editor__dialog-header");
            _title = new Label();
            _title.AddToClassList("table-editor__dialog-title");
            header.Add(_title);

            _closeButton = new Button(Close);
            _closeButton.AddToClassList("table-editor__icon-button");
            _closeButton.tooltip = "닫기 (Esc)";
            VisualElement closeIcon = new();
            closeIcon.AddToClassList("table-editor__icon");
            closeIcon.AddToClassList("table-editor__icon--close");
            _closeButton.Add(closeIcon);
            header.Add(_closeButton);
            _card.Add(header);

            _encodingField = AddDropdown("인코딩");

            _delimiterField = AddDropdown("구분 기호");
            _delimiterField.choices = BuildChoices(DelimiterChoices);
            _delimiterOtherField = AddOtherField(_delimiterField, "구분 기호 문자 하나");

            _quoteField = AddDropdown("따옴표");
            _quoteField.choices = BuildChoices(QuoteChoices);
            _quoteOtherField = AddOtherField(_quoteField, "따옴표 문자 하나");

            _quoteModeField = AddDropdown("따옴표 모드");
            _quoteModeField.choices = new List<string>(QuoteModeChoices);

            _newLineField = AddDropdown("줄 끝");
            _newLineField.choices = new List<string> { NewLineChoices[0].Label, NewLineChoices[1].Label };

            _endsWithNewLineToggle = new Toggle("마지막 줄 바꿈 추가");
            _endsWithNewLineToggle.AddToClassList("table-editor__dialog-toggle");
            _card.Add(_endsWithNewLineToggle);

            VisualElement footer = new();
            footer.AddToClassList("table-editor__dialog-footer");

            _cancelButton = CreateFooterButton("취소", "table-editor__dialog-button--secondary", Close,
                "바꾸지 않고 닫습니다.");
            _reopenButton = CreateFooterButton("다시 열기", "table-editor__dialog-button--primary", OnReopenClicked,
                "이 형식으로 파일을 다시 읽습니다. 저장하지 않은 편집은 버립니다.");
            _applyButton = CreateFooterButton("적용", "table-editor__dialog-button--primary", OnApplyClicked,
                "셀 값은 그대로 두고 저장할 형식만 바꿉니다. 저장해야 파일에 반영됩니다.");

            footer.Add(_cancelButton);
            footer.Add(_reopenButton);
            footer.Add(_applyButton);
            _card.Add(footer);

            root.Add(_overlay);
        }

        /// <summary>
        /// '다시 열기'를 눌렀을 때 고른 형식과 함께 호출됩니다.
        /// </summary>
        public event Action<TableFileOptions> ReopenRequested;

        /// <summary>
        /// '적용'을 눌렀을 때 고른 형식과 함께 호출됩니다.
        /// </summary>
        public event Action<TableFileOptions> ApplyRequested;

        /// <summary>
        /// 닫혔을 때 호출됩니다.
        /// </summary>
        public event Action Closed;

        public bool IsOpen => _overlay.ClassListContains(HiddenClassName) == false;

        /// <summary>
        /// 현재 형식으로 값을 채우고 엽니다.
        /// </summary>
        public void Open(TableFileOptions current, TableFormat format)
        {
            current ??= new TableFileOptions();

            _title.text = format == TableFormat.Tsv ? "TSV 파일 형식" : "CSV 파일 형식";

            FillEncodingChoices(current.Encoding);
            SelectChar(_delimiterField, _delimiterOtherField, DelimiterChoices, current.Delimiter);
            SelectChar(_quoteField, _quoteOtherField, QuoteChoices, current.Quote);
            _quoteModeField.index = (int)current.QuoteMode;
            _newLineField.index = current.NewLine == "\r\n" ? 1 : 0;
            _endsWithNewLineToggle.SetValueWithoutNotify(current.EndsWithNewLine);

            _overlay.RemoveFromClassList(HiddenClassName);

            // 그리드의 숨은 편집 입력칸이 포커스를 갖고 있으면 대화상자에서 친 글자가 셀로 들어간다. 대화상자로 옮긴다.
            _card.schedule.Execute(() => _card.Focus()).ExecuteLater(0);
        }

        public void Close()
        {
            if (IsOpen == false)
                return;

            _overlay.AddToClassList(HiddenClassName);
            Closed?.Invoke();
        }

        public void Dispose()
        {
            _card.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _overlay.RemoveFromHierarchy();
        }

        private DropdownField AddDropdown(string label)
        {
            Label caption = new(label);
            caption.AddToClassList("table-editor__dialog-label");
            _card.Add(caption);

            DropdownField field = new();
            field.AddToClassList("table-editor__dialog-dropdown");
            _card.Add(field);

            return field;
        }

        // '기타...'를 고르면 그 아래에 한 글자 입력칸을 보여 준다.
        private TextField AddOtherField(DropdownField owner, string placeholder)
        {
            TextField field = new() { maxLength = 1 };
            field.AddToClassList("table-editor__dialog-other-field");
            field.textEdition.placeholder = placeholder;
            field.style.display = DisplayStyle.None;
            _card.Add(field);

            owner.RegisterValueChangedCallback(evt =>
                field.style.display = evt.newValue == OtherChoice ? DisplayStyle.Flex : DisplayStyle.None);

            return field;
        }

        private Button CreateFooterButton(string text, string variantClass, Action clicked, string tooltip)
        {
            Button button = new(clicked) { text = text, tooltip = tooltip };
            button.AddToClassList("table-editor__dialog-button");
            button.AddToClassList(variantClass);
            return button;
        }

        private static List<string> BuildChoices((string Label, char Value)[] choices)
        {
            List<string> labels = new();

            foreach ((string label, char _) in choices)
                labels.Add(label);

            labels.Add(OtherChoice);
            return labels;
        }

        private void FillEncodingChoices(Encoding current)
        {
            List<string> labels = new();
            _encodingFactories.Clear();

            int selected = TableEncodings.IndexOf(current);

            if (selected < 0)
            {
                labels.Add($"{current.EncodingName} (현재)");
                _encodingFactories.Add(() => current);
            }

            foreach (TableEncodings.Entry entry in TableEncodings.Entries)
            {
                labels.Add(entry.Label);
                _encodingFactories.Add(entry.Create);
            }

            _encodingField.choices = labels;

            // 목록에 없던 인코딩은 맨 앞에 더했으므로 0번이다.
            _encodingField.index = selected < 0 ? 0 : selected;
        }

        private static void SelectChar(DropdownField field, TextField otherField, (string Label, char Value)[] choices, char value)
        {
            for (int index = 0; index < choices.Length; index++)
            {
                if (choices[index].Value != value)
                    continue;

                field.index = index;
                otherField.SetValueWithoutNotify(string.Empty);
                otherField.style.display = DisplayStyle.None;
                return;
            }

            field.index = choices.Length;
            otherField.SetValueWithoutNotify(value.ToString());
            otherField.style.display = DisplayStyle.Flex;
        }

        // '기타'인데 글자를 비워 두었으면 고를 수 없는 상태라 null이다.
        private static char? ReadChar(DropdownField field, TextField otherField, (string Label, char Value)[] choices)
        {
            if (field.index >= 0 && field.index < choices.Length)
                return choices[field.index].Value;

            string text = otherField.value;
            return string.IsNullOrEmpty(text) ? null : text[0];
        }

        private TableFileOptions ReadOptions(char delimiter, char quote)
        {
            int encodingIndex = Math.Clamp(_encodingField.index, 0, _encodingFactories.Count - 1);

            return new TableFileOptions
            {
                Encoding = _encodingFactories[encodingIndex](),
                Delimiter = delimiter,
                Quote = quote,
                QuoteMode = (TableQuoteMode)Math.Clamp(_quoteModeField.index, 0, QuoteModeChoices.Length - 1),
                NewLine = NewLineChoices[Math.Clamp(_newLineField.index, 0, NewLineChoices.Length - 1)].Value,
                EndsWithNewLine = _endsWithNewLineToggle.value,
            };
        }

        private void OnReopenClicked()
        {
            Submit(ReopenRequested);
        }

        private void OnApplyClicked()
        {
            Submit(ApplyRequested);
        }

        private void Submit(Action<TableFileOptions> handler)
        {
            char? delimiter = ReadChar(_delimiterField, _delimiterOtherField, DelimiterChoices);
            char? quote = ReadChar(_quoteField, _quoteOtherField, QuoteChoices);

            // '기타'를 고르고 글자를 비워 두었다. 그 칸으로 포커스를 옮겨 채우게 한다.
            if (delimiter == null)
            {
                _delimiterOtherField.Focus();
                return;
            }

            if (quote == null)
            {
                _quoteOtherField.Focus();
                return;
            }

            TableFileOptions options = ReadOptions(delimiter.Value, quote.Value);

            Close();
            handler?.Invoke(options);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != UnityEngine.KeyCode.Escape)
                return;

            evt.StopPropagation();
            Close();
        }
    }
}
