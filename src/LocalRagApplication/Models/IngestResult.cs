using System.Collections.Generic;

namespace LocalRagApplication.Models
{
    /// <summary>
    /// ファイル取り込み処理（<c>DocumentIngestionService.IngestAsync</c>）の結果。
    /// 複数ファイルを一括アップロードした際の集計と、対応外拡張子でスキップしたファイル名の一覧を保持する。
    /// </summary>
    public class IngestResult
    {
        /// <summary>
        /// 新規に追加されたドキュメントの件数。
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// 既存ドキュメントを再取り込みして更新した件数。
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 取り込み処理中にエラーが発生した件数。
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 対応していない拡張子のため、取り込み処理自体を行わずスキップしたファイル名の一覧。
        /// </summary>
        public IReadOnlyList<string> SkippedFileNames { get; set; }
    }
}
