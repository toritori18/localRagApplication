using System.Collections.Generic;

namespace LocalRagApplication.Services.Chunking
{
    /// <summary>
    /// テキストを検索・埋め込み用の小さな単位（チャンク）に分割するインターフェース。
    /// </summary>
    public interface ITextChunker
    {
        /// <summary>
        /// テキストを固定条件でチャンクに分割する。
        /// </summary>
        /// <param name="text">分割対象のテキスト。</param>
        /// <param name="chunkSize">1チャンクあたりの文字数。</param>
        /// <param name="chunkOverlap">隣接するチャンク間でオーバーラップさせる文字数。</param>
        /// <returns>分割されたチャンク本文の一覧。</returns>
        IReadOnlyList<string> Split(string text, int chunkSize, int chunkOverlap);
    }
}
