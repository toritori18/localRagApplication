namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// Ollama呼び出し（埋め込み・回答生成）および質問応答パイプラインの処理時間内訳を記録するロガーのインターフェース。
    /// </summary>
    public interface IQueryMetricsLogger
    {
        /// <summary>
        /// 処理時間の内訳メッセージを記録する。
        /// </summary>
        /// <param name="message">記録するメッセージ。</param>
        void LogMetrics(string message);
    }
}
