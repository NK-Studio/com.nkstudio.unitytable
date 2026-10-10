using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 편집 중인 테이블의 행과 셀을 보관하는 모델입니다. UI에 의존하지 않습니다.
    /// 파일에서 읽은 셀은 문자열로 만들지 않고 원본 텍스트 안의 구간(<see cref="CellSlot"/>)으로만 들고 있다가,
    /// 화면에 보이는 셀처럼 실제로 읽을 때 문자열을 만든다. 편집한 셀만 별도 목록에 문자열로 둔다.
    /// </summary>
    public sealed class TableDocument
    {
        private readonly List<List<CellSlot>> _rows = new();

        // 편집으로 들어온 값들이다. 같은 셀을 다시 편집하면 그 자리를 덮어써 목록이 계속 자라지 않는다.
        // 삭제된 행/열이 쓰던 항목은 회수하지 않는다. 편집 횟수만큼만 남으므로 문서 수명 동안 무시할 만하다.
        private readonly List<string> _editedValues = new();

        private string _text = string.Empty;

        // 원본 텍스트를 읽을 때 쓴 따옴표 문자다. 저장 형식(FileOptions.Quote)을 바꿔도 아직 편집하지 않은 셀은
        // 원본 그대로 해석해야 하므로 따로 기억한다.
        private char _sourceQuote = '"';
        private int _columnCount;

        /// <summary>
        /// 프로젝트 상대 경로입니다. 새 문서면 비어 있습니다.
        /// </summary>
        public string AssetPath { get; set; } = string.Empty;

        /// <summary>
        /// 파일의 구분자 형식입니다.
        /// </summary>
        public TableFormat Format { get; set; } = TableFormat.Csv;

        /// <summary>
        /// 원본 파일의 인코딩과 개행 정보입니다.
        /// </summary>
        public TableFileOptions FileOptions { get; set; } = new();

        /// <summary>
        /// 행 개수입니다.
        /// </summary>
        public int RowCount => _rows.Count;

        /// <summary>
        /// 열 개수입니다. 모든 행이 이 개수만큼 셀을 가집니다.
        /// </summary>
        public int ColumnCount => _columnCount;

        /// <summary>
        /// 셀 값이 바뀌었을 때 행과 열 인덱스를 전달합니다.
        /// </summary>
        public event Action<int, int> CellChanged;

        /// <summary>
        /// 행 또는 열 개수가 바뀌었을 때 호출됩니다.
        /// </summary>
        public event Action StructureChanged;

        /// <summary>
        /// 파싱 결과로 문서 내용을 교체합니다. 행마다 셀 개수가 달라도 최대 열 수에 맞춰 패딩합니다.
        /// </summary>
        /// <param name="rows">교체할 행 목록입니다.</param>
        public void SetContent(List<List<string>> rows)
        {
            ResetStorage(string.Empty);

            if (rows != null)
            {
                foreach (List<string> row in rows)
                {
                    List<CellSlot> slots = new(row.Count);

                    foreach (string value in row)
                        slots.Add(CreateEditedSlot(value));

                    _rows.Add(slots);
                }
            }

            FinishContent();
        }

        /// <summary>
        /// 파서가 기록한 셀 구간으로 문서 내용을 교체합니다. 셀 문자열은 읽을 때 만든다.
        /// </summary>
        /// <param name="text">셀 구간이 가리키는 원본 텍스트입니다.</param>
        /// <param name="rows">행마다 셀 구간을 담은 목록입니다.</param>
        /// <param name="sourceQuote">파싱할 때 쓴 따옴표 문자입니다.</param>
        internal void SetParsedContent(string text, List<List<CellSlot>> rows, char sourceQuote = '"')
        {
            ResetStorage(text ?? string.Empty);
            _sourceQuote = sourceQuote;

            if (rows != null)
                _rows.AddRange(rows);

            FinishContent();
        }

        /// <summary>
        /// 셀 문자열을 만들지 않고 셀 값에 키워드가 들어 있는지 검사합니다. 검색처럼 모든 셀을 훑을 때 쓴다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        /// <param name="keyword">찾을 문자열입니다. 비어 있으면 false입니다.</param>
        /// <param name="comparison">비교 방식입니다.</param>
        /// <returns>셀 값에 키워드가 있으면 true입니다.</returns>
        public bool CellContains(int row, int column, string keyword, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;

            if (row < 0 || row >= _rows.Count || column < 0 || column >= _columnCount)
                return false;

            CellSlot slot = _rows[row][column];

            // 원본 구간을 그대로 쓰는 셀만 할당 없이 비교할 수 있다. 따옴표 해석이 필요한 셀과 편집한 셀은 값을 만들어 비교한다.
            if (!slot.IsEditedValue && !slot.IsQuoted)
            {
                if (slot.RawLength < keyword.Length)
                    return false;

                return _text.IndexOf(keyword, slot.TextStart, slot.RawLength, comparison) >= 0;
            }

            return Resolve(slot).IndexOf(keyword, comparison) >= 0;
        }

        /// <summary>
        /// 셀 문자열을 만들지 않고 셀 값의 대략적인 글자 수를 반환합니다. 열 폭 맞춤의 후보를 고를 때 쓴다.
        /// 원본 구간 셀은 원본 길이를 그대로 돌려주므로, 따옴표로 감싼 셀은 따옴표만큼 조금 길게 나온다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        /// <returns>글자 수입니다. 범위를 벗어나거나 빈 셀이면 0입니다.</returns>
        public int GetCellLengthHint(int row, int column)
        {
            if (row < 0 || row >= _rows.Count || column < 0 || column >= _columnCount)
                return 0;

            CellSlot slot = _rows[row][column];

            return slot.IsEditedValue ? _editedValues[slot.EditedValueIndex].Length : slot.RawLength;
        }

        /// <summary>
        /// 지정한 셀 값을 반환합니다. 범위를 벗어나면 빈 문자열을 반환합니다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        /// <returns>셀 값입니다.</returns>
        public string GetCell(int row, int column)
        {
            if (row < 0 || row >= _rows.Count)
                return string.Empty;

            if (column < 0 || column >= _columnCount)
                return string.Empty;

            return Resolve(_rows[row][column]);
        }

        /// <summary>
        /// 지정한 셀 값을 설정합니다. 범위를 벗어나면 아무 것도 하지 않습니다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        /// <param name="value">설정할 값입니다.</param>
        public void SetCell(int row, int column, string value)
        {
            if (row < 0 || row >= _rows.Count)
                return;

            if (column < 0 || column >= _columnCount)
                return;

            value ??= string.Empty;

            CellSlot current = _rows[row][column];

            if (string.Equals(Resolve(current), value, StringComparison.Ordinal))
                return;

            if (current.IsEditedValue && value.Length > 0)
                _editedValues[current.EditedValueIndex] = value;
            else
                _rows[row][column] = CreateEditedSlot(value);

            CellChanged?.Invoke(row, column);
        }

        /// <summary>
        /// 지정한 행의 셀 값을 복사한 배열을 반환합니다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <returns>복사된 셀 값 배열입니다.</returns>
        public string[] GetRowValues(int row)
        {
            if (row < 0 || row >= _rows.Count)
                return Array.Empty<string>();

            string[] values = new string[_columnCount];

            for (int columnIndex = 0; columnIndex < _columnCount; columnIndex++)
                values[columnIndex] = Resolve(_rows[row][columnIndex]);

            return values;
        }

        /// <summary>
        /// 지정한 열의 셀 값을 복사한 배열을 반환합니다.
        /// </summary>
        /// <param name="column">열 인덱스입니다.</param>
        /// <returns>복사된 셀 값 배열입니다.</returns>
        public string[] GetColumnValues(int column)
        {
            if (column < 0 || column >= _columnCount)
                return Array.Empty<string>();

            string[] values = new string[_rows.Count];

            for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
                values[rowIndex] = Resolve(_rows[rowIndex][column]);

            return values;
        }

        /// <summary>
        /// 지정한 위치에 행을 삽입합니다.
        /// </summary>
        /// <param name="index">삽입 위치입니다.</param>
        /// <param name="values">삽입할 행의 값 목록입니다. null이면 빈 행을 삽입합니다.</param>
        /// <param name="count">values가 null일 때 삽입할 빈 행 개수입니다.</param>
        public void InsertRows(int index, IReadOnlyList<string[]> values, int count)
        {
            index = Math.Clamp(index, 0, _rows.Count);

            int insertCount = values?.Count ?? count;

            if (insertCount <= 0)
                return;

            for (int offset = 0; offset < insertCount; offset++)
            {
                List<CellSlot> row = new(_columnCount);
                string[] source = values != null ? values[offset] : null;

                for (int columnIndex = 0; columnIndex < _columnCount; columnIndex++)
                {
                    bool hasSource = source != null && columnIndex < source.Length;
                    row.Add(hasSource ? CreateEditedSlot(source[columnIndex]) : CellSlot.Empty);
                }

                _rows.Insert(index + offset, row);
            }

            StructureChanged?.Invoke();
        }

        /// <summary>
        /// 지정한 위치부터 행을 제거하고 제거된 값을 반환합니다. 마지막 한 행은 남깁니다.
        /// </summary>
        /// <param name="index">제거 시작 위치입니다.</param>
        /// <param name="count">제거할 행 개수입니다.</param>
        /// <returns>제거된 행의 값 목록입니다.</returns>
        public List<string[]> RemoveRows(int index, int count)
        {
            List<string[]> removed = new();

            index = Math.Clamp(index, 0, Math.Max(0, _rows.Count - 1));
            count = Math.Min(count, _rows.Count - index);
            count = Math.Min(count, _rows.Count - 1);

            if (count <= 0)
                return removed;

            for (int offset = 0; offset < count; offset++)
                removed.Add(GetRowValues(index + offset));

            _rows.RemoveRange(index, count);
            StructureChanged?.Invoke();

            return removed;
        }

        /// <summary>
        /// firstRow부터 이어지는 행들의 순서를 바꿉니다. 행 참조만 옮기므로 셀 문자열을 만들지 않습니다.
        /// </summary>
        /// <param name="firstRow">순서를 바꿀 첫 행입니다. 그 앞의 행(헤더 등)은 그대로 둡니다.</param>
        /// <param name="order">order[i]는 firstRow 기준으로 몇 번째 행이 i번째 자리로 오는지입니다. 순열이어야 합니다.</param>
        public void ReorderRows(int firstRow, IReadOnlyList<int> order)
        {
            if (order == null || order.Count == 0)
                return;

            if (firstRow < 0 || firstRow + order.Count > _rows.Count)
                throw new ArgumentOutOfRangeException(nameof(order), "정렬할 범위가 문서 행 범위를 벗어났습니다.");

            // 순열이 아니면 행이 중복되거나 사라지므로, 문서를 건드리기 전에 막는다.
            bool[] used = new bool[order.Count];
            List<CellSlot>[] reordered = new List<CellSlot>[order.Count];

            for (int index = 0; index < order.Count; index++)
            {
                int source = order[index];

                if (source < 0 || source >= order.Count || used[source])
                    throw new ArgumentException("행 순서가 순열이 아닙니다.", nameof(order));

                used[source] = true;
                reordered[index] = _rows[firstRow + source];
            }

            for (int index = 0; index < reordered.Length; index++)
                _rows[firstRow + index] = reordered[index];

            StructureChanged?.Invoke();
        }

        /// <summary>
        /// 지정한 위치에 열을 삽입합니다.
        /// </summary>
        /// <param name="index">삽입 위치입니다.</param>
        /// <param name="values">삽입할 열의 값 목록입니다. null이면 빈 열을 삽입합니다.</param>
        /// <param name="count">values가 null일 때 삽입할 빈 열 개수입니다.</param>
        public void InsertColumns(int index, IReadOnlyList<string[]> values, int count)
        {
            index = Math.Clamp(index, 0, _columnCount);

            int insertCount = values?.Count ?? count;

            if (insertCount <= 0)
                return;

            for (int offset = 0; offset < insertCount; offset++)
            {
                string[] source = values != null ? values[offset] : null;

                for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
                {
                    bool hasSource = source != null && rowIndex < source.Length;
                    CellSlot slot = hasSource ? CreateEditedSlot(source[rowIndex]) : CellSlot.Empty;

                    _rows[rowIndex].Insert(index + offset, slot);
                }
            }

            _columnCount += insertCount;
            StructureChanged?.Invoke();
        }

        /// <summary>
        /// 지정한 위치부터 열을 제거하고 제거된 값을 반환합니다. 마지막 한 열은 남깁니다.
        /// </summary>
        /// <param name="index">제거 시작 위치입니다.</param>
        /// <param name="count">제거할 열 개수입니다.</param>
        /// <returns>제거된 열의 값 목록입니다.</returns>
        public List<string[]> RemoveColumns(int index, int count)
        {
            List<string[]> removed = new();

            index = Math.Clamp(index, 0, Math.Max(0, _columnCount - 1));
            count = Math.Min(count, _columnCount - index);
            count = Math.Min(count, _columnCount - 1);

            if (count <= 0)
                return removed;

            for (int offset = 0; offset < count; offset++)
                removed.Add(GetColumnValues(index + offset));

            foreach (List<CellSlot> row in _rows)
                row.RemoveRange(index, count);

            _columnCount -= count;
            StructureChanged?.Invoke();

            return removed;
        }

        /// <summary>
        /// 문서의 모든 셀을 문자열로 만들어 반환합니다. 셀마다 문자열을 새로 만들므로 큰 문서에서는 비싸다.
        /// 파일 저장은 이 메서드를 거치지 않는다.
        /// </summary>
        /// <returns>행 목록입니다.</returns>
        public IReadOnlyList<IReadOnlyList<string>> GetRows()
        {
            List<IReadOnlyList<string>> rows = new(_rows.Count);

            for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
                rows.Add(GetRowValues(rowIndex));

            return rows;
        }

        private string Resolve(CellSlot slot)
        {
            return slot.IsEditedValue
                ? _editedValues[slot.EditedValueIndex]
                : slot.ResolveFromText(_text, _sourceQuote);
        }

        // 빈 값은 목록에 넣지 않고 빈 셀로 둔다.
        private CellSlot CreateEditedSlot(string value)
        {
            if (string.IsNullOrEmpty(value))
                return CellSlot.Empty;

            _editedValues.Add(value);
            return CellSlot.FromEditedValue(_editedValues.Count - 1);
        }

        private void ResetStorage(string text)
        {
            _rows.Clear();
            _editedValues.Clear();
            _text = text;
        }

        // 최대 열 수에 맞춰 행을 패딩하고 구조 변경을 알린다. 행이 하나도 없으면 빈 행 하나를 둔다.
        private void FinishContent()
        {
            int columnCount = 0;

            foreach (List<CellSlot> row in _rows)
            {
                if (row.Count > columnCount)
                    columnCount = row.Count;
            }

            _columnCount = Math.Max(1, columnCount);

            if (_rows.Count == 0)
                _rows.Add(new List<CellSlot>());

            NormalizeRows();
            StructureChanged?.Invoke();
        }

        // 모든 행의 셀 개수를 열 개수에 맞춘다.
        private void NormalizeRows()
        {
            foreach (List<CellSlot> row in _rows)
            {
                while (row.Count < _columnCount)
                    row.Add(CellSlot.Empty);

                if (row.Count > _columnCount)
                    row.RemoveRange(_columnCount, row.Count - _columnCount);
            }
        }
    }
}
