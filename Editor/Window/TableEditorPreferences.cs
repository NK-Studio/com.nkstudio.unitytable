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

                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(160)))
                    TableEditorSettings.ResetToDefaults();
            }
        }

        private static void DrawDisplaySection()
        {
            Section("Display");

            string[] themes = Enum.GetValues(typeof(TableEditorThemeStyle))
                .Cast<TableEditorThemeStyle>()
                .Select(TableEditorTheme.DisplayName)
                .ToArray();

            TableEditorTheme.Style = (TableEditorThemeStyle)EditorGUILayout.Popup("Theme", (int)TableEditorTheme.Style, themes);
            Description("Also available from the ⋮ menu at the top right of the window. Dark/light follows the Unity Editor skin.");

            TableEditorSettings.FontSize = EditorGUILayout.IntSlider(
                "Font Size",
                TableEditorSettings.FontSize,
                TableEditorSettings.MinFontSize,
                TableEditorSettings.MaxFontSize);
            Description("Font size of the table. Row height and row number width scale with it.");

            TableEditorSettings.MouseWheelZoom = EditorGUILayout.Toggle("Mouse Wheel Zoom", TableEditorSettings.MouseWheelZoom);
            Description("Ctrl (Cmd on macOS) + mouse wheel increases or decreases the font size.");
        }

        private static void DrawAutoFitSection()
        {
            Section("Column Auto-Fit");

            TableEditorSettings.AutoFitOnOpen = EditorGUILayout.Toggle("On File Open", TableEditorSettings.AutoFitOnOpen);
            Description("Fits columns that have values to their content when a file opens. When off, all columns start at the default width.");

            TableEditorSettings.AutoFitOnEdit = EditorGUILayout.Toggle("On Cell Edit", TableEditorSettings.AutoFitOnEdit);
            Description("Widens a column when an edited value no longer fits. Never shrinks a column you widened yourself.");

            TableEditorSettings.AutoFitScanRows = EditorGUILayout.DelayedIntField("Rows to Scan", TableEditorSettings.AutoFitScanRows);
            Description("Number of rows, from the top, examined to decide a width. Header rows are always examined.");

            TableEditorSettings.AutoFitMaxWidthPercent = EditorGUILayout.IntSlider(
                "Max Width (% of Window)",
                TableEditorSettings.AutoFitMaxWidthPercent,
                1,
                100);
            Description("Maximum width an auto-fitted column can grow to. Longer values are truncated with …. Also applies when double-clicking a column border.");
        }

        private static void DrawNewTableSection()
        {
            Section("New CSV File");
            Description("Size and format of files created with Assets > Create > Scripting > CSV File.");

            TableEditorSettings.NewTableRows = EditorGUILayout.DelayedIntField("Rows", TableEditorSettings.NewTableRows);
            TableEditorSettings.NewTableColumns = EditorGUILayout.DelayedIntField("Columns", TableEditorSettings.NewTableColumns);

            string[] encodings = TableEncodings.Entries.Select(entry => entry.Label).ToArray();
            TableEditorSettings.NewTableEncodingIndex = EditorGUILayout.Popup("Encoding", TableEditorSettings.NewTableEncodingIndex, encodings);

            TableEditorSettings.NewTableDelimiter = CharPopup("Delimiter", TableFormatChoices.Delimiters, TableEditorSettings.NewTableDelimiter);
            TableEditorSettings.NewTableQuote = CharPopup("Quote", TableFormatChoices.Quotes, TableEditorSettings.NewTableQuote);

            TableEditorSettings.NewTableQuoteMode = (TableQuoteMode)EditorGUILayout.Popup(
                "Quote Mode",
                (int)TableEditorSettings.NewTableQuoteMode,
                TableFormatChoices.QuoteModes);

            int newLine = EditorGUILayout.Popup(
                "Line Ending",
                TableEditorSettings.NewTableUsesCrlf ? 1 : 0,
                TableFormatChoices.NewLines.Select(choice => choice.Label).ToArray());
            TableEditorSettings.NewTableUsesCrlf = newLine == 1;

            TableEditorSettings.NewTableEndsWithNewLine = EditorGUILayout.Toggle(
                "Final Newline",
                TableEditorSettings.NewTableEndsWithNewLine);

            Description("Files created with a format other than the default (UTF-8, comma, double quote, minimal) record it in their .meta so they are read the same way when opened.");
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
