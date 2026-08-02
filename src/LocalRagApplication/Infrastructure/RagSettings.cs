using System.Configuration;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// Web.config の <c>appSettings</c> からRAG関連の設定値を読み取る静的ヘルパー。
    /// 値が未設定または不正な場合は既定値にフォールバックする。
    /// </summary>
    public static class RagSettings
    {
        // localhost ではなく 127.0.0.1 を既定とする。Ollama は 127.0.0.1（IPv4）でのみ待ち受けるが、
        // Windows の localhost は ::1（IPv6）を先に解決するため、IPv4 へフォールバックするまで
        // 新規接続ごとに約2秒を要する（実測: localhost 2073ms / 127.0.0.1 10ms）。
        private const string OllamaBaseUrlDefault = "http://127.0.0.1:11434";
        private const string OllamaEmbeddingModelDefault = "nomic-embed-text";
        private const string OllamaGenerationModelDefault = "llama3.1";

        // Ollama側の既定は5m（出典: https://raw.githubusercontent.com/ollama/ollama/main/docs/api.md ）。
        // 30mという値自体は、作業セッション中モデルをメモリに保持し続けることを狙った出典なし・暫定値。
        private const string OllamaKeepAliveDefault = "30m";

        // 出典なし・暫定値。「数百文字ずつチャンク分割」という方針に沿った暫定的な設定であり、
        // 論文・仕様書等の一次資料に基づくものではない。実際の回答品質を見ながら調整すること。
        private const int RagChunkSizeDefault = 500;
        private const int RagChunkOverlapDefault = 100;
        private const int RagTopNDefault = 5;

        // 出典なし・暫定値。内訳ログ（query-metrics-*.log）は質問1回ごとに出力され増加ペースが速いため、
        // 無期限に増加させないための保持日数として設定した暫定値であり、一次資料に基づくものではない。
        private const int RagMetricsLogRetentionDaysDefault = 7;

        /// <summary>
        /// OllamaサーバーのベースURL（既定: <c>http://127.0.0.1:11434</c>）。
        /// </summary>
        public static string OllamaBaseUrl
        {
            get { return GetString("OllamaBaseUrl", OllamaBaseUrlDefault); }
        }

        /// <summary>
        /// 埋め込み（ベクトル化）に使用するOllamaモデル名（既定: <c>nomic-embed-text</c>）。
        /// </summary>
        public static string OllamaEmbeddingModel
        {
            get { return GetString("OllamaEmbeddingModel", OllamaEmbeddingModelDefault); }
        }

        /// <summary>
        /// 回答生成に使用するOllamaモデル名（既定: <c>llama3.1</c>）。
        /// </summary>
        public static string OllamaGenerationModel
        {
            get { return GetString("OllamaGenerationModel", OllamaGenerationModelDefault); }
        }

        /// <summary>
        /// Ollamaにモデルをメモリ上に保持させる時間（<c>keep_alive</c>、既定: <c>30m</c>）。
        /// <c>/api/generate</c>・<c>/api/embed</c> の両方に付与する。
        /// </summary>
        public static string OllamaKeepAlive
        {
            get { return GetString("OllamaKeepAlive", OllamaKeepAliveDefault); }
        }

        /// <summary>
        /// テキストチャンク分割時の1チャンクあたりの文字数（既定: 500）。
        /// </summary>
        public static int RagChunkSize
        {
            get { return GetInt("RagChunkSize", RagChunkSizeDefault); }
        }

        /// <summary>
        /// テキストチャンク分割時のチャンク間オーバーラップ文字数（既定: 100）。
        /// </summary>
        public static int RagChunkOverlap
        {
            get { return GetInt("RagChunkOverlap", RagChunkOverlapDefault); }
        }

        /// <summary>
        /// 類似度検索で取得する上位チャンク数（既定: 5）。
        /// </summary>
        public static int RagTopN
        {
            get { return GetInt("RagTopN", RagTopNDefault); }
        }

        /// <summary>
        /// 内訳ログ（<c>data/logs/query-metrics-*.log</c>）の保持日数（既定: 7）。これより古い日付のファイルは自動削除される。
        /// </summary>
        public static int RagMetricsLogRetentionDays
        {
            get { return GetInt("RagMetricsLogRetentionDays", RagMetricsLogRetentionDaysDefault); }
        }

        /// <summary>
        /// <c>appSettings</c> から文字列値を取得する。未設定または空文字の場合は既定値を返す。
        /// </summary>
        /// <param name="key">appSettings のキー。</param>
        /// <param name="defaultValue">未設定時の既定値。</param>
        /// <returns>設定値、または既定値。</returns>
        private static string GetString(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// <c>appSettings</c> から整数値を取得する。未設定または数値に変換できない場合は既定値を返す。
        /// </summary>
        /// <param name="key">appSettings のキー。</param>
        /// <param name="defaultValue">未設定・不正値時の既定値。</param>
        /// <returns>設定値、または既定値。</returns>
        private static int GetInt(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            int parsed;
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out parsed))
            {
                return defaultValue;
            }

            return parsed;
        }
    }
}
