using System;
using System.Collections.Generic;
using System.Linq;
using NKStudio.TabularEditor.Data;
using UnityEditor;
using UnityEngine;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// Preferences > Tabular Editor 페이지입니다. 사용자별 설정(EditorPrefs)이라 팀원마다 다르게 둘 수 있습니다.
    /// </summary>
    internal static class TableEditorPreferences
    {
        public const string Path = "Preferences/Tabular Editor";

        private static GUIStyle _sectionStyle;
        private static GUIStyle _descriptionStyle;

        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider(Path, SettingsScope.User)
            {
                label = "Tabular Editor",
                keywords = new HashSet<string>
                {
                    "csv", "tsv", "table", "font", "zoom", "auto fit", "column", "encoding", "delimiter",
                    "글꼴", "줌", "열 너비", "자동 맞춤", "인코딩", "구분 기호", "테마",
                },
                guiHandler = _ => DrawGui(),
            };
        }

        private static void DrawGui()
        {
            EnsureStyles();
            EditorGUIUtility.labelWidth = 200;

            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(10, 10, 6, 10) }))
            {
                DrawDisplaySection();
                DrawAutoFitSection();
                DrawNewTableSection();

                EditorGUILayout.Space(16);

                if (GUILayout.Button("기본값으로 되돌리기", GUILayout.Width(160)))
                    TableEditorSettings.ResetToDefaults();
            }
        }

        private static void DrawDisplaySection()
        {
            Section("표시");

            string[] themes = Enum.GetValues(typeof(TableEditorThemeStyle))
                .Cast<TableEditorThemeStyle>()
                .Select(TableEditorTheme.DisplayName)
                .ToArray();

            TableEditorTheme.Style = (TableEditorThemeStyle)EditorGUILayout.Popup("테마", (int)TableEditorTheme.Style, themes);
            Description("창 오른쪽 위 ⋮ 메뉴에서도 바꿀 수 있습니다. 다크/라이트는 Unity 에디터 스킨을 따릅니다.");

            TableEditorSettings.FontSize = EditorGUILayout.IntSlider(
                "글꼴 크기",
                TableEditorSettings.FontSize,
                TableEditorSettings.MinFontSize,
                TableEditorSettings.MaxFontSize);
            Description("표의 글꼴 크기입니다. 행 높이와 행 번호 폭도 함께 바뀝니다.");

            TableEditorSettings.MouseWheelZoom = EditorGUILayout.Toggle("마우스 휠 줌", TableEditorSettings.MouseWheelZoom);
            Description("Ctrl(macOS에서는 Cmd) + 마우스 휠로 글꼴 크기를 키우고 줄입니다.");
        }

        private static void DrawAutoFitSection()
        {
            Section("열 너비 자동 맞춤");

            TableEditorSettings.AutoFitOnOpen = EditorGUILayout.Toggle("파일을 열 때", TableEditorSettings.AutoFitOnOpen);
            Description("파일을 열 때 값이 있는 열의 너비를 내용에 맞춥니다. 끄면 모든 열이 기본 너비로 시작합니다.");

            TableEditorSettings.AutoFitOnEdit = EditorGUILayout.Toggle("셀을 편집할 때", TableEditorSettings.AutoFitOnEdit);
            Description("셀 값을 바꿨을 때 그 열이 내용보다 좁으면 넓힙니다. 직접 넓혀 둔 열을 줄이지는 않습니다.");

            TableEditorSettings.AutoFitScanRows = EditorGUILayout.DelayedIntField("스캔할 행 수", TableEditorSettings.AutoFitScanRows);
            Description("너비를 정할 때 위에서부터 살펴볼 행 수입니다. 헤더 행은 항상 살펴봅니다.");

            TableEditorSettings.AutoFitMaxWidthPercent = EditorGUILayout.IntSlider(
                "최대 너비 (창 너비 %)",
                TableEditorSettings.AutoFitMaxWidthPercent,
                1,
                100);
            Description("자동 맞춤 열이 넓어질 수 있는 최대 너비입니다. 넘치는 값은 …로 잘립니다. 경계를 더블클릭해 맞출 때도 같습니다.");
        }

        private static void DrawNewTableSection()
        {
            Section("새 CSV 파일");
            Description("Assets > Create > Scripting > CSV File로 만드는 파일의 크기와 형식입니다.");

            TableEditorSettings.NewTableRows = EditorGUILayout.DelayedIntField("행 수", TableEditorSettings.NewTableRows);
            TableEditorSettings.NewTableColumns = EditorGUILayout.DelayedIntField("열 수", TableEditorSettings.NewTableColumns);

            string[] encodings = TableEncodings.Entries.Select(entry => entry.Label).ToArray();
            TableEditorSettings.NewTableEncodingIndex = EditorGUILayout.Popup("인코딩", TableEditorSettings.NewTableEncodingIndex, encodings);

            TableEditorSettings.NewTableDelimiter = CharPopup("구분 기호", TableFormatChoices.Delimiters, TableEditorSettings.NewTableDelimiter);
            TableEditorSettings.NewTableQuote = CharPopup("따옴표", TableFormatChoices.Quotes, TableEditorSettings.NewTableQuote);

            TableEditorSettings.NewTableQuoteMode = (TableQuoteMode)EditorGUILayout.Popup(
                "따옴표 모드",
                (int)TableEditorSettings.NewTableQuoteMode,
                TableFormatChoices.QuoteModes);

            int newLine = EditorGUILayout.Popup(
                "줄 끝",
                TableEditorSettings.NewTableUsesCrlf ? 1 : 0,
                TableFormatChoices.NewLines.Select(choice => choice.Label).ToArray());
            TableEditorSettings.NewTableUsesCrlf = newLine == 1;

            TableEditorSettings.NewTableEndsWithNewLine = EditorGUILayout.Toggle(
                "마지막 줄 바꿈 추가",
                TableEditorSettings.NewTableEndsWithNewLine);

            Description("기본(UTF-8·쉼표·큰따옴표·최소)과 다르게 만든 파일은 그 형식이 .meta에 기록되어 열 때 그대로 읽힙니다.");
        }

        // '기타'를 골랐지만 아직 글자를 입력하지 않은 항목이다. IMGUI는 프레임 사이에 상태가 없어,
        // 기억해 두지 않으면 다음 프레임에 값(목록에 있는 문자)을 보고 원래 선택으로 되돌아간다.
        private static readonly HashSet<string> OtherSelected = new();

        // 목록에 없는 문자는 '기타'로 보여 주고, 그 옆 칸에서 한 글자를 직접 입력한다.
        private static char CharPopup(string label, (string Label, char Value)[] choices, char value)
        {
            string[] labels = choices.Select(choice => choice.Label).Append(TableFormatChoices.OtherLabel).ToArray();
            int listedIndex = TableFormatChoices.IndexOf(choices, value);
            int index = OtherSelected.Contains(label) ? choices.Length : listedIndex;

            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = EditorGUILayout.Popup(label, index, labels);

                if (selected < choices.Length)
                {
                    OtherSelected.Remove(label);
                    return choices[selected].Value;
                }

                OtherSelected.Add(label);

                string current = listedIndex == choices.Length ? value.ToString() : string.Empty;
                string typed = EditorGUILayout.DelayedTextField(current, GUILayout.Width(40));

                return string.IsNullOrEmpty(typed) ? value : typed[0];
            }
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(title, _sectionStyle);
            EditorGUILayout.Space(2);
        }

        private static void Description(string text)
        {
            EditorGUILayout.LabelField(text, _descriptionStyle);
            EditorGUILayout.Space(4);
        }

        private static void EnsureStyles()
        {
            _sectionStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _descriptionStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { padding = new RectOffset(4, 0, 0, 0) };
        }
    }
}
