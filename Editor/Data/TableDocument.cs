using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 편집 중인 테이블의 행과 셀을 보관하는 모델입니다. UI에 의존하지 않습니다.
    /// </summary>
    public sealed class TableDocument
    {
        private readonly List<List<string>> _rows = new();

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
            _rows.Clear();

            int columnCount = 0;

            if (rows != null)
            {
                foreach (List<string> row in rows)
                {
                    if (row.Count > columnCount)
                        columnCount = row.Count;
                }

                foreach (List<string> row in rows)
                    _rows.Add(new List<string>(row));
            }

            _columnCount = Math.Max(1, columnCount);

            if (_rows.Count == 0)
                _rows.Add(new List<string>());

            NormalizeRows();
            StructureChanged?.Invoke();
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

            return _rows[row][column] ?? string.Empty;
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

            if (string.Equals(_rows[row][column], value, StringComparison.Ordinal))
                return;

            _rows[row][column] = value;
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

            return _rows[row].ToArray();
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
                values[rowIndex] = _rows[rowIndex][column];

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
                List<string> row = new(_columnCount);
                string[] source = values != null ? values[offset] : null;

                for (int columnIndex = 0; columnIndex < _columnCount; columnIndex++)
                {
                    bool hasSource = source != null && columnIndex < source.Length;
                    row.Add(hasSource ? source[columnIndex] : string.Empty);
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
                removed.Add(_rows[index + offset].ToArray());

            _rows.RemoveRange(index, count);
            StructureChanged?.Invoke();

            return removed;
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
                    string value = hasSource ? source[rowIndex] : string.Empty;

                    _rows[rowIndex].Insert(index + offset, value);
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

            foreach (List<string> row in _rows)
                row.RemoveRange(index, count);

            _columnCount -= count;
            StructureChanged?.Invoke();

            return removed;
        }

        /// <summary>
        /// 문서의 모든 행을 읽기 전용 목록으로 반환합니다. 직렬화에 사용합니다.
        /// </summary>
        /// <returns>행 목록입니다.</returns>
        public IReadOnlyList<IReadOnlyList<string>> GetRows()
        {
            return _rows;
        }

        // 모든 행의 셀 개수를 열 개수에 맞춘다.
        private void NormalizeRows()
        {
            foreach (List<string> row in _rows)
            {
                while (row.Count < _columnCount)
                    row.Add(string.Empty);

                if (row.Count > _columnCount)
                    row.RemoveRange(_columnCount, row.Count - _columnCount);
            }
        }
    }
}
