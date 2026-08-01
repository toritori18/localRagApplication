using System;

namespace LocalRagApplication.Models
{
    /// <summary>
    /// 取り込んだ1ファイル分のメタデータ。<c>data/rag.db</c> の Documents テーブルに対応する。
    /// </summary>
    public class DocumentMetadata
    {
        /// <summary>
        /// GUID文字列。<c>data/sources/</c>・<c>data/extracted/</c> の保存ファイル名にも使う。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// アップロード時の元ファイル名（表示専用。保存パスには使わない）。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// ファイルの拡張子（".pdf" / ".md" / ".txt"）。
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// ファイルサイズ（バイト数）。
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// アップロード日時（UTC）。
        /// </summary>
        public DateTime UploadedAtUtc { get; set; }

        /// <summary>
        /// ドキュメントの処理状態。
        /// </summary>
        public DocumentStatus Status { get; set; }

        /// <summary>
        /// 索引化（チャンク分割・埋め込みベクトル化）が完了した日時（UTC）。未索引の場合は null。
        /// </summary>
        public DateTime? IndexedAtUtc { get; set; }

        /// <summary>
        /// 生成されたチャンクの件数。
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// 取り込み処理中にエラーが発生した場合のエラーメッセージ。正常時は null。
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
