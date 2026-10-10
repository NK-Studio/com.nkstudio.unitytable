using System.Collections.Generic;

namespace NKStudio.TabularEditor
{
    /// <summary>
    /// 문구 표. 키 → (영어, 한국어). {0}…은 string.Format 인자. ".one"/".other" 쌍은 <see cref="Localization.Count"/>가 고른다.
    /// 예외·로그 메시지는 개발자용이라 이 표에 넣지 않고 영어로 둔다.
    /// </summary>
    internal static class Strings
    {
        public static readonly Dictionary<string, (string en, string ko)> Table = new()
        {
            // 공통
            ["common.ok"] = ("OK", "확인"),
            ["common.cancel"] = ("Cancel", "취소"),
            ["common.closeEsc"] = ("Close (Esc)", "닫기 (Esc)"),

            // ⋮ 메뉴
            ["menu.theme"] = ("Theme", "테마"),
            ["menu.language"] = ("Language", "언어"),
            ["menu.preferences"] = ("Preferences...", "환경 설정..."),
            ["menu.assetPreviewForFile"] = ("Asset Path Preview for This File", "이 파일에서 에셋 경로 미리보기"),

            // Preferences
            ["prefs.reset"] = ("Reset to Defaults", "기본값으로 되돌리기"),
            ["prefs.display"] = ("Display", "표시"),
            ["prefs.language"] = ("Language", "언어"),
            ["prefs.languageDesc"] = (
                "Language of the window, menus and this page. Also available from the ⋮ menu at the top right of the window.",
                "창·메뉴·이 페이지의 언어입니다. 창 오른쪽 위 ⋮ 메뉴에서도 바꿀 수 있습니다."),
            ["prefs.theme"] = ("Theme", "테마"),
            ["prefs.themeDesc"] = (
                "Also available from the ⋮ menu at the top right of the window. Dark/light follows the Unity Editor skin.",
                "창 오른쪽 위 ⋮ 메뉴에서도 바꿀 수 있습니다. 다크/라이트는 Unity 에디터 스킨을 따릅니다."),
            ["prefs.fontSize"] = ("Font Size", "글꼴 크기"),
            ["prefs.fontSizeDesc"] = (
                "Font size of the table. Row height and row number width scale with it.",
                "표의 글꼴 크기입니다. 행 높이와 행 번호 폭도 함께 바뀝니다."),
            ["prefs.wheelZoom"] = ("Mouse Wheel Zoom", "마우스 휠 줌"),
            ["prefs.wheelZoomDesc"] = (
                "Ctrl (Cmd on macOS) + mouse wheel increases or decreases the font size.",
                "Ctrl(macOS에서는 Cmd) + 마우스 휠로 글꼴 크기를 키우고 줄입니다."),
            ["prefs.assetPreview"] = ("Asset Path Preview", "에셋 경로 미리보기"),
            ["prefs.assetPreviewDesc"] = (
                "Columns whose values are asset paths (Resources paths like Art/Sprite/icon, or Assets/… paths) are detected automatically. Their cells show the asset icon, hovering shows a preview, and Ctrl (Cmd on macOS) + click pings the asset in the Project window. Turning this off disables it for every file; to turn it off for one file only, use the ⋮ menu of that file's window (saved in its .meta).",
                "값이 에셋 경로(Art/Sprite/icon 같은 Resources 경로, 또는 Assets/… 경로)인 열을 자동으로 찾습니다. 그 열의 셀에 에셋 아이콘을 붙이고, 마우스를 올리면 미리보기를 보여 주며, Ctrl(macOS는 Cmd)+클릭하면 Project 창에서 에셋을 찾아 줍니다. 여기서 끄면 모든 파일에서 꺼지고, 한 파일만 끄려면 그 파일 창의 ⋮ 메뉴를 씁니다(.meta에 저장)."),
            ["prefs.autoFit"] = ("Column Auto-Fit", "열 너비 자동 맞춤"),
            ["prefs.onOpen"] = ("On File Open", "파일을 열 때"),
            ["prefs.onOpenDesc"] = (
                "Fits columns that have values to their content when a file opens. When off, all columns start at the default width.",
                "파일을 열 때 값이 있는 열의 너비를 내용에 맞춥니다. 끄면 모든 열이 기본 너비로 시작합니다."),
            ["prefs.onEdit"] = ("On Cell Edit", "셀을 편집할 때"),
            ["prefs.onEditDesc"] = (
                "Widens a column when an edited value no longer fits. Never shrinks a column you widened yourself.",
                "셀 값을 바꿨을 때 그 열이 내용보다 좁으면 넓힙니다. 직접 넓혀 둔 열을 줄이지는 않습니다."),
            ["prefs.scanRows"] = ("Rows to Scan", "스캔할 행 수"),
            ["prefs.scanRowsDesc"] = (
                "Number of rows, from the top, examined to decide a width. Header rows are always examined.",
                "너비를 정할 때 위에서부터 살펴볼 행 수입니다. 헤더 행은 항상 살펴봅니다."),
            ["prefs.maxWidth"] = ("Max Width (% of Window)", "최대 너비 (창 너비 %)"),
            ["prefs.maxWidthDesc"] = (
                "Maximum width an auto-fitted column can grow to. Longer values are truncated with …. Also applies when double-clicking a column border.",
                "자동 맞춤 열이 넓어질 수 있는 최대 너비입니다. 넘치는 값은 …로 잘립니다. 경계를 더블클릭해 맞출 때도 같습니다."),
            ["prefs.newTable"] = ("New CSV File", "새 CSV 파일"),
            ["prefs.newTableDesc"] = (
                "Size and format of files created with Assets > Create > Scripting > CSV File.",
                "Assets > Create > Scripting > CSV File로 만드는 파일의 크기와 형식입니다."),
            ["prefs.rows"] = ("Rows", "행 수"),
            ["prefs.columns"] = ("Columns", "열 수"),
            ["prefs.newTableFormatDesc"] = (
                "Files created with a format other than the default (UTF-8, comma, double quote, minimal) record it in their .meta so they are read the same way when opened.",
                "기본(UTF-8·쉼표·큰따옴표·최소)과 다르게 만든 파일은 그 형식이 .meta에 기록되어 열 때 그대로 읽힙니다."),

            // 파일 형식 항목 (Preferences·대화상자 공용)
            ["format.encoding"] = ("Encoding", "인코딩"),
            ["format.delimiter"] = ("Delimiter", "구분 기호"),
            ["format.quote"] = ("Quote", "따옴표"),
            ["format.quoteMode"] = ("Quote Mode", "따옴표 모드"),
            ["format.lineEnding"] = ("Line Ending", "줄 끝"),
            ["format.finalNewline"] = ("Final Newline", "마지막 줄 바꿈 추가"),

            // 선택지
            ["choice.otherCharacter"] = ("Other...", "기타..."),
            ["choice.comma"] = ("Comma ( , )", "쉼표 ( , )"),
            ["choice.tab"] = ("Tab", "탭"),
            ["choice.semicolon"] = ("Semicolon ( ; )", "세미콜론 ( ; )"),
            ["choice.colon"] = ("Colon ( : )", "콜론 ( : )"),
            ["choice.pipe"] = ("Pipe ( | )", "파이프 ( | )"),
            ["choice.space"] = ("Space", "공백"),
            ["choice.none"] = ("None", "없음"),
            ["choice.always"] = ("Always", "항상"),
            ["choice.minimal"] = ("Minimal", "최소"),
            ["choice.never"] = ("Never", "사용 안 함"),

            // 인코딩 이름
            ["encoding.cp949"] = ("Korean (CP949)", "한국어 (CP949)"),
            ["encoding.eucKr"] = ("Korean (EUC-KR)", "한국어 (EUC-KR)"),
            ["encoding.shiftJis"] = ("Japanese (Shift_JIS)", "일본어 (Shift_JIS)"),
            ["encoding.eucJp"] = ("Japanese (EUC-JP)", "일본어 (EUC-JP)"),
            ["encoding.gbk"] = ("Chinese Simplified (GBK)", "중국어 간체 (GBK)"),
            ["encoding.gb18030"] = ("Chinese Simplified (GB18030)", "중국어 간체 (GB18030)"),
            ["encoding.big5"] = ("Chinese Traditional (Big5)", "중국어 번체 (Big5)"),

            // 파일 형식 대화상자
            ["dialog.titleCsv"] = ("CSV File Format", "CSV 파일 형식"),
            ["dialog.titleTsv"] = ("TSV File Format", "TSV 파일 형식"),
            ["dialog.otherDelimiter"] = ("Single delimiter character", "구분 기호 문자 하나"),
            ["dialog.otherQuote"] = ("Single quote character", "따옴표 문자 하나"),
            ["dialog.currentEncoding"] = ("{0} (current)", "{0} (현재)"),
            ["dialog.cancelTip"] = ("Close without changes.", "바꾸지 않고 닫습니다."),
            ["dialog.reopen"] = ("Reopen", "다시 열기"),
            ["dialog.reopenTip"] = (
                "Re-read the file in this format. Unsaved edits are discarded.",
                "이 형식으로 파일을 다시 읽습니다. 저장하지 않은 편집은 버립니다."),
            ["dialog.apply"] = ("Apply", "적용"),
            ["dialog.applyTip"] = (
                "Change only the save format and keep cell values. Takes effect in the file when you save.",
                "셀 값은 그대로 두고 저장할 형식만 바꿉니다. 저장해야 파일에 반영됩니다."),

            // 상태 표시줄
            ["status.size"] = ("{0} × {1}", "{0} × {1}"),
            ["status.unsaved"] = ("Unsaved", "저장되지 않음"),
            ["status.deleteHint"] = ("{0} · Delete to remove", "{0} · Delete로 삭제"),
            ["status.delimiter"] = ("Delimiter=[{0}], Quote=[{1}]({2})", "구분 기호=[{0}], 따옴표=[{1}]({2})"),
            ["status.changeFormatTip"] = (
                "Click to change the file format (encoding, delimiter, quote, line ending).",
                "눌러서 파일 형식(인코딩·구분 기호·따옴표·줄 끝)을 바꿉니다."),
            ["status.encodingTip"] = ("File encoding. Kept as is when saving.", "파일 인코딩. 저장할 때 그대로 유지합니다."),
            ["status.newlineTip"] = ("Line ending. Kept as is when saving.", "개행 문자. 저장할 때 그대로 유지합니다."),
            ["status.delimiterTip"] = (
                "Delimiter and quoting rule. Quotes are added only to cells that contain the delimiter, a quote or a line break.",
                "구분 기호와 따옴표 규칙. 따옴표는 구분 기호·따옴표·줄바꿈이 든 셀에만 붙입니다."),
            ["count.row.one"] = ("{0} row", "{0} 행"),
            ["count.row.other"] = ("{0} rows", "{0} 행"),
            ["count.column.one"] = ("{0} column", "{0} 열"),
            ["count.column.other"] = ("{0} columns", "{0} 열"),
            ["count.selectedRow.one"] = ("{0} row", "행 {0}개"),
            ["count.selectedRow.other"] = ("{0} rows", "행 {0}개"),
            ["count.selectedColumn.one"] = ("{0} column", "열 {0}개"),
            ["count.selectedColumn.other"] = ("{0} columns", "열 {0}개"),
            ["count.cell.one"] = ("{0} cell", "{0} 셀"),
            ["count.cell.other"] = ("{0} cells", "{0} 셀"),
            ["count.char.one"] = ("{0} char", "{0}자"),
            ["count.char.other"] = ("{0} chars", "{0}자"),

            // 창
            ["window.newTable"] = ("New Table", "새 테이블"),
            ["window.loading"] = ("Loading…", "불러오는 중…"),
            ["window.loadFailed"] = ("Failed to load the file.\n{0}", "파일을 불러오지 못했습니다.\n{0}"),
            ["window.uxmlFailed"] = ("Failed to load UXML.\n{0}", "UXML을 불러오지 못했습니다.\n{0}"),
            ["window.saveTitle"] = ("Save Table", "테이블 저장"),
            ["window.noPath"] = (
                "There is no file path to save to. Open a CSV or TSV file from the Project window.",
                "저장할 파일 경로가 없습니다. 프로젝트 창에서 CSV 또는 TSV 파일을 열어 주세요."),
            ["window.reopenBody"] = (
                "Discard unsaved edits and re-read the file in this format.",
                "저장하지 않은 편집을 버리고 이 형식으로 파일을 다시 읽습니다."),
            ["window.overwriteBody"] = (
                "The file was changed outside the editor. Overwrite it with your current edits?",
                "파일이 에디터 외부에서 변경되었습니다. 현재 편집 내용으로 덮어쓰겠습니까?"),
            ["window.overwrite"] = ("Overwrite", "덮어쓰기"),
            ["window.unsavedChanges"] = (
                "There are unsaved changes. Do you want to save them?",
                "저장되지 않은 변경 사항이 있습니다. 저장하시겠습니까?"),
            ["window.addColumnTip"] = ("Add a column at the far right.", "맨 오른쪽에 열을 추가합니다."),
            ["window.addRowTip"] = ("Add a row at the bottom.", "맨 아래에 행을 추가합니다."),

            // 외부 변경 알림 바
            ["external.message"] = (
                "The file was changed outside the editor. You have unsaved edits.",
                "파일이 에디터 밖에서 변경되었습니다. 저장하지 않은 편집이 있습니다."),
            ["external.reload"] = ("Reload", "다시 불러오기"),
            ["external.reloadTip"] = (
                "Discard unsaved edits and read the changed file.",
                "저장하지 않은 편집을 버리고 바뀐 파일 내용을 읽습니다."),
            ["external.ignore"] = ("Ignore", "무시"),
            ["external.ignoreTip"] = (
                "Keep your edits. You will be asked whether to overwrite when you save.",
                "지금 편집을 유지합니다. 저장할 때 덮어쓸지 다시 묻습니다."),

            // 찾기·바꾸기
            ["search.buttonTip"] = ("Search cell contents (Ctrl+F)", "셀 내용을 검색합니다. (Ctrl+F)"),
            ["search.expandTip"] = ("Toggle replace", "바꾸기 열기/닫기"),
            ["search.caseTip"] = ("Match case", "대소문자 구분"),
            ["search.regexTip"] = ("Use regular expression", "정규식 사용"),
            ["search.wordTip"] = ("Match whole word", "단어 단위로"),
            ["search.nextTip"] = ("Next match (Enter, F3)", "다음 결과 (Enter, F3)"),
            ["search.previousTip"] = ("Previous match (Shift+Enter, Shift+F3)", "이전 결과 (Shift+Enter, Shift+F3)"),
            ["search.selectionTip"] = ("Find in selection", "선택 영역에서 찾기"),
            ["search.verticalTip"] = ("Search vertically (column by column, top to bottom)", "수직 방향으로 찾기 (왼쪽 열부터 위→아래)"),
            ["search.preserveCaseTip"] = ("Preserve case", "대소문자 유지"),
            ["search.replaceTip"] = ("Replace (Enter)", "바꾸기 (Enter)"),
            ["search.replaceAllTip"] = ("Replace all (Ctrl/Cmd+Enter)", "모두 바꾸기 (Ctrl/Cmd+Enter)"),
            ["search.find"] = ("Find", "찾기"),
            ["search.replace"] = ("Replace", "바꾸기"),
            ["search.noResults"] = ("No results", "결과 없음"),
            ["search.invalidRegex"] = ("Invalid regex", "잘못된 정규식"),
            ["count.match.one"] = ("{0} match", "{0}개"),
            ["count.match.other"] = ("{0} matches", "{0}개"),
            ["count.replaced.one"] = ("Replaced {0} cell", "{0}개 셀 바꿈"),
            ["count.replaced.other"] = ("Replaced {0} cells", "{0}개 셀 바꿈"),

            // 행으로 이동
            ["goto.placeholder"] = ("e.g. 12, C12", "예: 12, C12"),

            // 우클릭·열 메뉴
            ["menu.pingAsset"] = ("Show in Project", "Project 창에서 보기"),
            ["menu.sort"] = ("Sort", "정렬"),
            ["menu.sortAscending"] = ("Sort Ascending", "오름차순 정렬"),
            ["menu.sortDescending"] = ("Sort Descending", "내림차순 정렬"),
            ["menu.insertRowAbove"] = ("Insert Row Above", "위에 행 삽입"),
            ["menu.insertRowBelow"] = ("Insert Row Below", "아래에 행 삽입"),
            ["menu.duplicateRow"] = ("Duplicate Row", "행 복제"),
            ["menu.deleteRow"] = ("Delete Row", "행 삭제"),
            ["menu.deleteRows"] = ("Delete {0} Rows", "행 {0}개 삭제"),
            ["menu.insertColumnLeft"] = ("Insert Column Left", "왼쪽에 열 삽입"),
            ["menu.insertColumnRight"] = ("Insert Column Right", "오른쪽에 열 삽입"),
            ["menu.deleteColumn"] = ("Delete Column", "열 삭제"),
            ["menu.deleteColumns"] = ("Delete {0} Columns", "열 {0}개 삭제"),
            ["menu.copy"] = ("Copy", "복사"),
            ["menu.cut"] = ("Cut", "잘라내기"),
            ["menu.paste"] = ("Paste", "붙여넣기"),
            ["menu.clearContents"] = ("Clear Contents", "내용 지우기"),
            ["menu.setHeaderRows"] = ("Set Header Rows Up to Selection", "선택 항목까지 헤더 행 설정"),
            ["menu.clearHeaderRows"] = ("Clear Header Rows", "헤더 행 설정 해제"),

            // 실행 취소 작업 이름
            ["undo.editCell"] = ("Edit Cell", "셀 편집"),
            ["undo.fillRange"] = ("Fill Range", "범위 채우기"),
            ["undo.fill"] = ("Fill", "채우기"),
            ["undo.paste"] = ("Paste", "붙여넣기"),
            ["undo.clearRange"] = ("Clear Range", "범위 비우기"),
            ["undo.insertRows"] = ("Insert Rows", "행 삽입"),
            ["undo.insertColumns"] = ("Insert Columns", "열 삽입"),
            ["undo.deleteRows"] = ("Delete Rows", "행 삭제"),
            ["undo.deleteColumns"] = ("Delete Columns", "열 삭제"),
            ["undo.moveRows"] = ("Move Rows", "행 이동"),
            ["undo.moveColumns"] = ("Move Columns", "열 이동"),
            ["undo.changeFormat"] = ("Change File Format", "파일 형식 변경"),
            ["undo.replace"] = ("Replace", "바꾸기"),
            ["undo.replaceAll"] = ("Replace All", "모두 바꾸기"),
        };
    }
}
