namespace LocalRagApplication.Models
{
    /// <summary>
    /// 質問文とのコサイン類似度検索でヒットしたチャンク1件分の情報。
    /// </summary>
    public class SearchHit
    {
        /// <summary>
        /// ヒットしたチャンク。
        /// </summary>
        public DocumentChunk Chunk { get; set; }

        /// <summary>
        /// 質問文ベクトルとのコサイン類似度。
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// チャンクの元となったドキュメントのファイル名（表示用）。
        /// </summary>
        public string DocumentFileName { get; set; }
    }
}
