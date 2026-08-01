namespace LocalRagApplication.Models
{
    /// <summary>
    /// ドキュメントを分割した1チャンク分の本文と埋め込みベクトル。<c>data/rag.db</c> の Chunks テーブルに対応する。
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// チャンクのGUID。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 親ドキュメント（<see cref="DocumentMetadata.Id"/>）のId。
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// ドキュメント内での出現順序（0始まり）。
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// チャンクの本文。
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// チャンク本文を埋め込みモデルでベクトル化した結果。
        /// </summary>
        public float[] Embedding { get; set; }
    }
}
