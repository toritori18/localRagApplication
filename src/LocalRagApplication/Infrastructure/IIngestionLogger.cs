using System;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// ドキュメント取り込み処理の警告・エラーを記録するロガーのインターフェース。
    /// </summary>
    public interface IIngestionLogger
    {
        /// <summary>
        /// 警告を記録する。
        /// </summary>
        /// <param name="message">警告メッセージ。</param>
        void LogWarning(string message);

        /// <summary>
        /// エラーを記録する。
        /// </summary>
        /// <param name="message">エラーメッセージ。</param>
        /// <param name="exception">発生した例外。</param>
        void LogError(string message, Exception exception);
    }
}
