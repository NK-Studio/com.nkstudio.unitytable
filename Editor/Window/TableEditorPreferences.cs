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
                    "csv", "tsv", "table", "font", "zoom", "auto fit", "column", "encoding", "delimiter", "language",
                    "글꼴", "줌", "열 너비", "자동 맞춤", "인코딩", "구분 기호", "테마", "언어",
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

                if (GUILayout.Button(Localization.Get("prefs.reset"), GUILayout.Width(160)))
                    TableEditorSettings.ResetToDefaults();
            }
        }

        private static void DrawDisplaySection()
        {
            Section(Localization.Get("prefs.display"));

            Language[] languages = (Language[])Enum.GetValues(typeof(Language));
            int languageIndex = EditorGUILayout.Popup(
                Localization.Get("prefs.language"),
                Array.IndexOf(languages, Localization.Current),
                languages.Select(Localization.DisplayName).ToArray());
            Localization.Current = languages[languageIndex];
            Description(Localization.Get("prefs.languageDesc"));

            string[] themes = Enum.GetValues(typeof(TableEditorThemeStyle))
                .Cast<TableEditorThemeStyle>()
                .Select(TableEditorTheme.DisplayName)
                .ToArray();

            TableEditorTheme.Style = (TableEditorThemeStyle)EditorGUILayout.Popup(Localization.Get("prefs.theme"), (int)TableEditorTheme.Style, themes);
            Description(Localization.Get("prefs.themeDesc"));

            TableEditorSettings.FontSize = EditorGUILayout.IntSlider(
                Localization.Get("prefs.fontSize"),
                TableEditorSettings.FontSize,
                TableEditorSettings.MinFontSize,
                TableEditorSettings.MaxFontSize);
            Description(Localization.Get("prefs.fontSizeDesc"));

            TableEditorSettings.MouseWheelZoom = EditorGUILayout.Toggle(Localization.Get("prefs.wheelZoom"), TableEditorSettings.MouseWheelZoom);
            Description(Localization.Get("prefs.wheelZoomDesc"));

            TableEditorSettings.AssetPathPreview = EditorGUILayout.Toggle(Localization.Get("prefs.assetPreview"), TableEditorSettings.AssetPathPreview);
            Description(Localization.Get("prefs.assetPreviewDesc"));
        }

        private static void DrawAutoFitSection()
        {
            Section(Localization.Get("prefs.autoFit"));

            TableEditorSettings.AutoFitOnOpen = EditorGUILayout.Toggle(Localization.Get("prefs.onOpen"), TableEditorSettings.AutoFitOnOpen);
            Description(Localization.Get("prefs.onOpenDesc"));

            TableEditorSettings.AutoFitOnEdit = EditorGUILayout.Toggle(Localization.Get("prefs.onEdit"), TableEditorSettings.AutoFitOnEdit);
            Description(Localization.Get("prefs.onEditDesc"));

            TableEditorSettings.AutoFitScanRows = EditorGUILayout.DelayedIntField(Localization.Get("prefs.scanRows"), TableEditorSettings.AutoFitScanRows);
            Description(Localization.Get("prefs.scanRowsDesc"));

            TableEditorSettings.AutoFitMaxWidthPercent = EditorGUILayout.IntSlider(
                Localization.Get("prefs.maxWidth"),
                TableEditorSettings.AutoFitMaxWidthPercent,
                1,
                100);
            Description(Localization.Get("prefs.maxWidthDesc"));
        }

        private static void DrawNewTableSection()
        {
            Section(Localization.Get("prefs.newTable"));
            Description(Localization.Get("prefs.newTableDesc"));

            TableEditorSettings.NewTableRows = EditorGUILayout.DelayedIntField(Localization.Get("prefs.rows"), TableEditorSettings.NewTableRows);
            TableEditorSettings.NewTableColumns = EditorGUILayout.DelayedIntField(Localization.Get("prefs.columns"), TableEditorSettings.NewTableColumns);

            string[] encodings = TableEncodings.Entries.Select(entry => entry.Label).ToArray();
            TableEditorSettings.NewTableEncodingIndex = EditorGUILayout.Popup(Localization.Get("format.encoding"), TableEditorSettings.NewTableEncodingIndex, encodings);

            TableEditorSettings.NewTableDelimiter = CharPopup("format.delimiter", TableFormatChoices.Delimiters, TableEditorSettings.NewTableDelimiter);
            TableEditorSettings.NewTableQuote = CharPopup("format.quote", TableFormatChoices.Quotes, TableEditorSettings.NewTableQuote);

            TableEditorSettings.NewTableQuoteMode = (TableQuoteMode)EditorGUILayout.Popup(
                Localization.Get("format.quoteMode"),
                (int)TableEditorSettings.NewTableQuoteMode,
                TableFormatChoices.QuoteModes);

            int newLine = EditorGUILayout.Popup(
                Localization.Get("format.lineEnding"),
                TableEditorSettings.NewTableUsesCrlf ? 1 : 0,
                TableFormatChoices.NewLines.Select(choice => choice.Label).ToArray());
            TableEditorSettings.NewTableUsesCrlf = newLine == 1;

            TableEditorSettings.NewTableEndsWithNewLine = EditorGUILayout.Toggle(
                Localization.Get("format.finalNewline"),
                TableEditorSettings.NewTableEndsWithNewLine);

            Description(Localization.Get("prefs.newTableFormatDesc"));
        }

        // '기타'를 골랐지만 아직 글자를 입력하지 않은 항목이다. IMGUI는 프레임 사이에 상태가 없어,
        // 기억해 두지 않으면 다음 프레임에 값(목록에 있는 문자)을 보고 원래 선택으로 되돌아간다.
        private static readonly HashSet<string> OtherSelected = new();

        // 목록에 없는 문자는 '기타'로 보여 주고, 그 옆 칸에서 한 글자를 직접 입력한다.
        // labelKey는 '기타'를 고른 상태를 기억하는 키도 겸한다. 문구로 기억하면 언어를 바꿀 때 상태를 잃는다.
        private static char CharPopup(string labelKey, (string Label, char Value)[] choices, char value)
        {
            string label = Localization.Get(labelKey);
            string[] labels = choices.Select(choice => choice.Label).Append(TableFormatChoices.OtherLabel).ToArray();
            int listedIndex = TableFormatChoices.IndexOf(choices, value);
            int index = OtherSelected.Contains(labelKey) ? choices.Length : listedIndex;

            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = EditorGUILayout.Popup(label, index, labels);

                if (selected < choices.Length)
                {
                    OtherSelected.Remove(labelKey);
                    return choices[selected].Value;
                }

                OtherSelected.Add(labelKey);

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
