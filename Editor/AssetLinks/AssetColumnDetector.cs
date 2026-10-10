using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.AssetLinks
{
    /// <summary>
    /// 열이 에셋 경로를 담은 열인지 자동으로 가립니다. 값이 있는 칸을 위에서부터 몇 개만 보고,
    /// 그중 절반 이상이 에셋으로 찾아지면 에셋 경로 열로 본다. 이름처럼 우연히 에셋 이름과 같은 값이 섞인 열이
    /// 아이콘으로 뒤덮이지 않게 하는 기준이다.
    /// </summary>
    public static class AssetColumnDetector
    {
        /// <summary>
        /// 살펴볼 값 개수입니다. 열 전체를 훑지 않아도 열의 성격은 위쪽 몇십 칸에서 드러난다.
        /// </summary>
        public const int SampleCount = 50;

        public static bool IsAssetColumn(IEnumerable<string> values, Func<string, bool> resolves)
        {
            int sampled = 0;
            int resolved = 0;

            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                sampled++;

                if (resolves(value))
                    resolved++;

                if (sampled >= SampleCount)
                    break;
            }

            return sampled > 0 && resolved * 2 >= sampled;
        }
    }
}
