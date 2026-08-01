namespace LocalRagApplication.Models
{
    /// <summary>
    /// 取り込んだドキュメントの処理状態を表す。
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// テキスト抽出・チャンク分割・埋め込みベクトル化が完了し、検索可能な状態。
        /// </summary>
        Indexed,

        /// <summary>
        /// 取り込み処理中に例外が発生し、索引化できなかった状態。
        /// </summary>
        Error,

        /// <summary>
        /// 対応していない拡張子のため、取り込み処理自体を行わなかった状態。
        /// </summary>
        Unsupported
    }
}
