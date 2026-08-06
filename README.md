# Tabular Editor

Unity 에디터에서 CSV/TSV 파일을 스프레드시트처럼 편집하는 UI Toolkit 기반 테이블 에디터입니다.

## 설치

Unity의 `Window > Package Manager > + > Install package from git URL`에 아래 주소를 넣습니다.

```text
https://github.com/NK-Studio/com.nkstudio.unitytable.git
```

또는 프로젝트의 `Packages/manifest.json`에 직접 추가합니다.

```json
"com.nkstudio.unitytable": "https://github.com/NK-Studio/com.nkstudio.unitytable.git"
```

특정 버전을 고정하려면 뒤에 `#1.0.0`처럼 태그를 붙입니다.

## 사용법

프로젝트 창에서 `.csv` 또는 `.tsv` 파일을 더블클릭하면 열립니다.
파일 없이 열려면 `Window > NKStudio > Tabular Editor`를 사용합니다.

## 단축키

| 키 | 동작 |
| --- | --- |
| 방향키 | 활성 셀 이동 |
| Shift + 방향키 | 선택 범위 확장 |
| Tab / Shift+Tab | 오른쪽/왼쪽 이동 (행 끝에서 줄바꿈) |
| Enter / Shift+Enter | 아래/위 이동. 편집 중이면 확정 후 이동 |
| Home / End | 행의 처음/끝 |
| Ctrl+Home / Ctrl+End | 표의 처음/끝 |
| PageUp / PageDown | 화면 단위 이동 |
| F2 또는 문자 입력 | 셀 편집 시작 |
| 더블클릭 | 셀 편집 시작 |
| 방향키 (타이핑 중) | 입력을 확정하고 그 방향으로 이동 |
| 방향키 (F2 편집 중) | 캐럿 이동 |
| Esc | 편집 취소 |
| Delete / Backspace | 선택 종류에 따라 행·열 삭제 또는 내용 비우기 |
| Ctrl+S | 저장 |
| Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y | 되돌리기 / 다시 실행 |
| Ctrl+C / Ctrl+X / Ctrl+V | 복사 / 잘라내기 / 붙여넣기 |
| Ctrl+A | 전체 선택 |
| Ctrl+F | 검색 |
| F3 / Shift+F3 | 다음 / 이전 일치 항목 |

macOS에서는 Ctrl 대신 Cmd를 사용합니다.

## 행과 열 다루기

툴바에는 행/열 버튼이 없습니다. 세 가지 방법을 씁니다.

### 끝에 추가 — `+` 버튼

그리드 오른쪽 끝의 `+`는 열을, 아래쪽 끝의 `+`는 행을 하나 추가합니다.

### 중간에 삽입 — 우클릭 메뉴

| 우클릭 위치 | 메뉴 |
| --- | --- |
| 셀 | 행 삽입/삭제, 열 삽입/삭제, 복사·잘라내기·붙여넣기·내용 지우기 |
| 행 번호 (왼쪽 거터) | 행 삽입/삭제, 클립보드 |
| 열 제목 (A/B/C) | 열 삽입/삭제, 클립보드 |

- 여러 행이나 열을 선택한 상태면 그 개수만큼 한 번에 삽입·삭제합니다. 3개 행을 선택하고 "위에 행 삽입"을 누르면 3개 행이 생깁니다.
- 선택 범위 **밖**을 우클릭하면 활성 셀이 그쪽으로 옮겨가고, 범위 **안**을 우클릭하면 선택이 유지됩니다.
- 마지막 남은 한 행이나 한 열은 삭제할 수 없어 메뉴 항목이 비활성으로 표시됩니다.
- macOS에서는 Ctrl+클릭으로도 메뉴가 열립니다.

### 삭제 — 선택 후 Delete

- 셀 위에서 **마우스를 끌면** 범위가 선택됩니다. Shift+클릭이나 Shift+방향키로도 넓힐 수 있습니다.
- **행 번호**를 클릭하면 그 행 전체가, **열 제목**을 클릭하면 그 열 전체가 선택됩니다. Shift+클릭으로 여러 개를 이어서 선택합니다.
- 이 상태에서 **Delete**를 누르면 행 또는 열이 삭제됩니다.
- 셀을 직접 선택한 상태에서 Delete를 누르면 **내용만** 지워집니다.
- 지금 Delete가 무엇을 지울지는 상태 표시줄에 나옵니다 (`행 3개 선택 · Delete로 삭제`).
- 마지막 남은 한 행/열이면 삭제 대신 내용만 지워집니다.
- 열 제목을 드래그하면 삭제가 아니라 열 폭 조절로 동작합니다.

## 동작 규칙

- **파일 보존**: 인코딩(BOM 유무), 개행 스타일(`\n` / `\r\n`), 최종 개행 유무를 원본 그대로 유지합니다.
  구분자, 따옴표, 개행이 들어 있는 셀만 인용하므로 불필요한 diff가 생기지 않습니다.
- **클립보드**: 편집 중인 파일이 CSV여도 클립보드는 항상 TSV입니다.
  Excel, 구글 시트, Numbers와 그대로 주고받을 수 있습니다.
- **Undo**: 윈도우 내부 스택을 사용하며 Unity 전역 Undo(씬 편집)와 완전히 분리되어 있습니다.
- **외부 변경 감지**: 파일을 연 뒤 외부에서 내용이 바뀌었으면 저장 시 덮어쓸지 확인합니다.

## 구조

```text
Editor/
├─ Data/        RFC 4180 파서/라이터, 문서 모델, 파일 입출력 (UI 의존 없음)
├─ Commands/    Undo 가능한 문서 변경 작업
├─ Selection/   셀 좌표와 선택 범위
├─ Import/      ScriptedImporter, 더블클릭 열기 핸들러
└─ Window/      EditorWindow, 그리드 View, 입력 라우터, 검색
```

문서 변경은 반드시 `ITableCommand`를 거칩니다. 이 규칙 덕분에 모든 변경이 Undo 가능합니다.

## 테스트

`Tests/Editor`의 EditMode 테스트를 Test Runner에서 실행합니다.
테스트가 보이지 않으면 프로젝트의 `Packages/manifest.json`에 다음을 추가합니다.

```json
"testables": [ "com.nkstudio.unitytable" ]
```
