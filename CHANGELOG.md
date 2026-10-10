# Changelog

## [Unreleased]

### Added

- 한국어/영어 전환. 창 오른쪽 위 ⋮ 메뉴의 Language(언어) 또는 Preferences > Tabular Editor에서 고른다.
  사용자별 설정(EditorPrefs)이고, 처음에는 OS 언어를 따른다(한국어면 한국어, 그 밖에는 영어).
  바꾸는 즉시 열려 있는 창의 메뉴·툴팁·상태 표시줄·찾기 바·Preferences가 함께 바뀐다(예외·로그 메시지는 영어 그대로).

### Fixed

- 트랙패드 좌우 쓸기·가로(틸트) 휠·Shift+휠로 표가 가로 스크롤되지 않던 문제. 열 제목·고정 헤더 행 위에서도 움직인다.

## [1.1.0] - 2026-10-10

### Added

- 창 오른쪽 위 ⋮ 메뉴에서 테마(Unity / Android Studio)를 고른다. 사용자별 설정(EditorPrefs)이며,
  다크/라이트는 Unity 에디터 스킨을 따른다.
- 선택 범위 전체를 감싸는 테두리와 오른쪽 아래 동그란 핸들. 핸들 위에서는 십자 커서가 나온다.
- 행 번호·열 제목을 드래그해 여러 행/열을 선택한다.
- 행 번호(왼쪽 숫자 인덱스) 우클릭 'Set Header Rows Up to Selection / Clear Header Rows'. 지정한 행은 맨 위에 고정되어 굵게 보이고,
  선택·편집은 그대로 되며 정렬에서만 빠진다. 헤더 행 수는 파일의 `.meta`(`userData`)에 기록되어 VCS로 공유된다.
- 찾기·바꾸기 바: 입력칸 안 대소문자 구분·정규식·단어 단위 토글, 다음/이전, 선택 영역에서 찾기,
  수직 방향으로 찾기(왼쪽 열부터 위→아래), 펼치기로 여는 바꾸기 줄(대소문자 유지, 바꾸기, 모두 바꾸기).
  모두 바꾸기는 Undo 한 번으로 되돌아간다. 바꾸기 칸에서 Enter는 바꾸기, Ctrl/Cmd+Enter는 모두 바꾸기.
- 에디터 밖(Finder·다른 편집기)에서 열린 파일이 바뀌면 바로 반영한다. 저장하지 않은 편집이 없으면 선택·스크롤·열 폭을
  유지한 채 다시 읽고, 있으면 표 위 알림 바에서 [Reload] / [Ignore]를 고른다. 에디터 자신의 저장은 반영 대상이 아니다.
- 파일을 열면 값이 있는 열은 내용에 맞는 폭으로 시작한다(헤더 행은 굵은 글꼴 기준, 최대 너비는 Preferences).
  열 경계를 더블클릭하면 그 열만 다시 맞춘다. 폭은 어디에도 저장하지 않는다.
- 셀 편집 입력칸이 SmoothCSV처럼 셀 위에 두 줄 높이로 열리고, 글자가 길어지면 오른쪽으로 표 영역 끝까지 늘어난 뒤
  줄바꿈해서 아래로 늘어난다. F2·더블클릭으로 열 때는 전체 선택하지 않고 캐럿을 끝에 둔다.
- 파일 형식 대화상자: 상태 표시줄의 인코딩·줄 끝·구분 기호를 누르면 연다. 인코딩(UTF-8/BOM, UTF-16, CP949·EUC-KR 등
  레거시 코드 페이지), 구분 기호(Comma·Tab·Semicolon·Colon·Pipe·Space·Other), 따옴표(None·"·'·Other), 따옴표 모드(Always·Minimal·Never),
  줄 끝(LF·CRLF), 마지막 줄 바꿈을 고른다. 'Reopen'은 그 형식으로 파일을 다시 읽고, 'Apply'는 셀 값은 둔 채 저장할 형식만 바꾼다(Undo 가능).
  파일에서 알아낼 수 없는 부분(레거시 인코딩·구분 기호·따옴표·따옴표 모드)은 .meta(`userData`)에 기록되어 다음에도·팀원도 같은 형식으로 읽는다.
- Preferences > Tabular Editor 페이지(사용자별 EditorPrefs).
  - 표시: 테마, 글꼴 크기(8~32, 행 높이·행 번호 폭·열 제목 높이·열 너비가 함께 비례), 마우스 휠 줌(Ctrl/Cmd+휠로 글꼴 크기 변경).
  - 열 너비 자동 맞춤: 파일을 열 때, 셀을 편집할 때(넓히기만 한다), 스캔할 행 수(기본 3000), 최대 너비(창 너비의 %, 기본 70).
  - 새 CSV 파일: 행·열 수(기본 5x5), 인코딩, 구분 기호, 따옴표, 따옴표 모드, 줄 끝, 마지막 줄 바꿈.
    Unity 템플릿 생성 경로는 UTF-8과 프로젝트 개행으로만 쓰므로, 임포트 직후 고른 형식으로 다시 쓰고 .meta에 형식을 남긴다.
- 아이콘은 Material Symbols(Apache 2.0)를 쓴다. 라이선스는 `Editor/Window/Icons/MaterialSymbols-LICENSE.txt`.
- 단축키 (macOS는 Ctrl → Cmd, Alt → Option). 모두 Undo로 되돌아간다.
  - Ctrl+Alt+방향키: 그 방향에 행·열 추가(선택한 개수만큼). 편집 중이면 입력을 확정한 뒤 추가한다.
  - Ctrl+방향키: Excel처럼 값이 이어진 구간의 끝·다음 값·표 끝으로 이동. Shift를 더하면 선택 확장.
  - Ctrl+-: 선택한 행 삭제(열 선택이면 열 삭제). Ctrl+D: 선택한 행을 바로 아래에 복제(행 번호 메뉴에도 'Duplicate Row' 추가).
  - Alt+↑/↓: 선택한 행을 한 칸씩 이동(헤더 행 구간을 넘지 않는다). Alt+←/→: 선택한 열을 한 칸씩 이동.
  - Ctrl+Enter(편집 중): 입력한 값을 선택 범위 전체에 채운다.
- 마지막 열 오른쪽·마지막 행 아래 빈 공간을 누르면 가장 가까운 셀을 선택한다. 드래그로 빈 공간까지 나가도 범위가 이어진다.

### Changed

- 열 제목은 항상 A, B, C … 로 표시한다. 툴바의 '첫 행을 헤더로'(first row as header) 토글은 헤더 행 설정으로 대체되어 제거했다.
- 툴바의 '검색'은 아이콘 버튼으로 바꿨다.
- 상태 표시줄: 왼쪽에 'N rows × M columns'와 활성 셀 '행:열 (N chars 또는 N cells)', 오른쪽에 인코딩·개행·구분 기호를 표시한다.
- 툴바의 '저장'·'다시 불러오기' 버튼을 제거했다. 저장은 Ctrl/Cmd+S와 창을 닫을 때의 저장 확인 팝업으로 한다.
- 활성 셀은 범위를 시작한 셀(anchor)이다. 드래그·Shift로 범위를 넓혀도 강조·편집 대상·상태 표시줄 위치가
  시작 셀에 남는다.
- 에디터 글꼴을 IBM Plex Sans KR(Regular/Bold, SIL OFL 1.1)로 바꿨다. 라이선스는 `Editor/Window/Fonts/IBMPlex-LICENSE.txt`.
- 범위에 든 셀의 배경을 회색에서 옅은 파란색으로 바꿔 선택이 또렷하게 보인다.
- 화면 문구를 모두 영어로 바꿨다: Preferences, 파일 형식 선택지·인코딩 이름, 상태 표시줄, 우클릭·열 메뉴, 파일 형식 대화상자,
  찾기·바꾸기 바, 알림 바, 확인 창, Undo 작업 이름, 툴팁, 예외·로그 메시지. Preferences 검색은 한국어 키워드로도 된다.
- 바꾸기 버튼 아이콘을 Material Symbols `check`로 바꿔 모두 바꾸기(`done_all`)와 짝을 맞췄다.
- `package.json`: 작성자 표기를 "NK Studio"로, 설명을 영어로 바꿨다.

## [1.0.1]

### Changed

- 지원 버전을 Unity 6.0까지 낮췄다. 6.3 이상은 `EntityId` 기반, 6.0~6.2는 기존 instance ID(`int`)
  기반 `OnOpenAsset` 핸들러를 사용해 `.csv`/`.tsv` 더블클릭 열기가 모든 버전에서 동작한다.

## [1.0.0]

첫 릴리스.

### Added

**파일 처리**

- RFC 4180 파서 및 라이터. 인용 필드, 필드 내 구분자/개행, `""` 이스케이프를 지원한다.
- CSV와 TSV를 같은 파이프라인에서 구분자만 바꿔 처리한다.
- 인코딩(BOM 유무), 개행 스타일(`\n` / `\r\n`), 최종 개행 유무를 원본 그대로 보존한다.
  구분자·따옴표·개행이 든 셀만 인용하므로 불필요한 diff가 생기지 않는다.
- `.tsv` ScriptedImporter와 `.csv`/`.tsv` 더블클릭 열기 핸들러.
- 파일을 연 뒤 외부에서 내용이 바뀌었으면 저장 시 덮어쓸지 확인한다.

**편집**

- 방향키/Tab/Enter/Home/End/PageUp/PageDown 셀 이동.
- Shift 조합과 마우스 드래그로 범위 선택.
- 문자를 치면 바로 편집이 시작된다. 편집 필드가 항상 활성 셀 위에서 포커스를 유지하므로
  한글 IME 조합이 첫 자모부터 정상 동작한다.
- F2 또는 더블클릭으로 기존 값을 전체 선택한 채 편집.
- Ctrl+S 저장, 저장되지 않은 변경 사항 추적 및 창 닫기 확인.
- Undo/Redo. 윈도우 내부 커맨드 스택을 쓰며 Unity 전역 Undo(씬 편집)와 완전히 분리된다.
- 복사/잘라내기/붙여넣기. 클립보드는 항상 TSV라 Excel, 구글 시트와 그대로 주고받는다.
- Ctrl+F 검색과 일치 항목 이동.

**행과 열**

- 그리드 오른쪽 끝과 아래쪽 끝의 `+` 버튼으로 열과 행을 추가한다.
- 셀, 행 번호, 열 제목 우클릭 메뉴로 원하는 위치에 삽입·삭제한다.
  선택한 행/열 개수만큼 한 번에 처리한다.
- 행 번호나 열 제목을 클릭해 행/열 전체를 선택한 뒤 Delete로 삭제한다.
  셀 선택 상태의 Delete는 내용만 지운다. 상태 표시줄이 Delete의 결과를 미리 알려준다.

### Note

이 패키지 이전에 다른 TSV 임포터로 임포트된 `.tsv` 파일은 `.meta`에 `ScriptedImporter:` 블록이
없어, 저장할 때마다 다음 경고가 나올 수 있다.

```text
Serialized file "....tsv.meta" contains a <unknown> object at version 1,
below the supported minimum (2). Open and re-save the file to upgrade.
```

해당 `.meta`에 아래 블록을 추가하면 없어진다. `guid`는 기존 값을 그대로 두어야 에셋 참조가
끊기지 않는다. 이 패키지로 새로 임포트되는 `.tsv`는 처음부터 올바른 `.meta`를 갖는다.

```yaml
ScriptedImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 2
  userData:
  assetBundleName:
  assetBundleVariant:
  script: {fileID: 11500000, guid: b93946026bd2d4c2399fc1df83f752d9, type: 3}
```
