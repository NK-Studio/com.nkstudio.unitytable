using System;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 글꼴 크기에 비례하는 표의 크기들입니다. 기준 글꼴 12px일 때 행 높이 20, 행 번호 열 46, 열 제목 22, 기본 열 160입니다.
    /// 글꼴 크기 설정과 Ctrl/Cmd+휠 줌이 모두 이 값을 바꿉니다.
    /// </summary>
    public readonly struct TableGridMetrics
    {
        public const int BaseFontSize = 12;

        private const float BaseRowHeight = 20f;
        private const float BaseRowNumberColumnWidth = 46f;
        private const float BaseHeaderHeight = 22f;
        private const float BaseDefaultColumnWidth = 160f;
        private const float BaseRowNumberFontSize = 10f;

        private TableGridMetrics(int fontSize)
        {
            FontSize = fontSize;
            Scale = fontSize / (float)BaseFontSize;
            RowHeight = MathF.Round(BaseRowHeight * Scale);
            RowNumberColumnWidth = MathF.Round(BaseRowNumberColumnWidth * Scale);
            HeaderHeight = MathF.Round(BaseHeaderHeight * Scale);
            DefaultColumnWidth = MathF.Round(BaseDefaultColumnWidth * Scale);

            // 행 번호는 본문보다 살짝 작게 쓴다. 너무 작아지면 읽을 수 없어 8px 아래로는 내리지 않는다.
            RowNumberFontSize = Math.Max(8f, MathF.Round(BaseRowNumberFontSize * Scale));
        }

        public int FontSize { get; }

        /// <summary>
        /// 기준 글꼴(12px) 대비 배율입니다.
        /// </summary>
        public float Scale { get; }

        public float RowHeight { get; }

        public float RowNumberColumnWidth { get; }

        public float HeaderHeight { get; }

        public float DefaultColumnWidth { get; }

        public float RowNumberFontSize { get; }

        public static TableGridMetrics FromFontSize(int fontSize)
        {
            return new TableGridMetrics(Math.Max(1, fontSize));
        }

        /// <summary>
        /// 자동 맞춤 열의 최대 너비입니다. 데이터 영역 너비의 percent%이고, 최소 열 너비보다 작아지지 않습니다.
        /// </summary>
        /// <param name="dataAreaWidth">행 번호 열을 뺀 표 영역 너비입니다.</param>
        /// <param name="percent">1~100입니다.</param>
        /// <param name="minColumnWidth">최소 열 너비입니다.</param>
        public static float MaxAutoFitWidth(float dataAreaWidth, int percent, float minColumnWidth)
        {
            float width = dataAreaWidth * Math.Clamp(percent, 1, 100) / 100f;
            return Math.Max(minColumnWidth, width);
        }
    }
}
