using System;
using System.Collections.Generic;

namespace LocalRagApplication.Services.Chunking
{
    /// <summary>
    /// テキストを固定文字数（<c>chunkSize</c>）ごとに、隣接チャンク間を <c>chunkOverlap</c> 文字だけ
    /// 重複させながら分割するチャンカー。オーバーラップを設けるのは、チャンクの境界で文の途中が
    /// 切れてしまい前後の文脈が失われるのを緩和するためである。
    /// </summary>
    public class FixedLengthTextChunker : ITextChunker
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// <paramref name="chunkSize"/> が0以下、または <paramref name="chunkOverlap"/> が
        /// <paramref name="chunkSize"/> 以上の場合。この条件では分割位置が前進せず無限ループになり得るため。
        /// </exception>
        public IReadOnlyList<string> Split(string text, int chunkSize, int chunkOverlap)
        {
            var chunks = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                return chunks;
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSizeは1以上である必要があります。", nameof(chunkSize));
            }

            if (chunkOverlap >= chunkSize)
            {
                throw new ArgumentException("chunkOverlapはchunkSize未満である必要があります。", nameof(chunkOverlap));
            }

            if (text.Length <= chunkSize)
            {
                chunks.Add(text);
                return chunks;
            }

            var step = chunkSize - chunkOverlap;
            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(chunkSize, text.Length - start);
                chunks.Add(text.Substring(start, length));

                if (start + length >= text.Length)
                {
                    break;
                }

                start += step;
            }

            return chunks;
        }
    }
}
