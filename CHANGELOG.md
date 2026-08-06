# Changelog

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
