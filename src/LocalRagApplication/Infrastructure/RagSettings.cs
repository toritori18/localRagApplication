using System.Configuration;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// Web.config の <c>appSettings</c> からRAG関連の設定値を読み取る静的ヘルパー。
    /// 値が未設定または不正な場合は既定値にフォールバックする。
    /// </summary>
    public static class RagSettings
    {
        private const string OllamaBaseUrlDefault = "http://localhost:11434";
        private const string OllamaEmbeddingModelDefault = "nomic-embed-text";
        private const string OllamaGenerationModelDefault = "llama3.1";

        // 出典なし・暫定値。「数百文字ずつチャンク分割」という方針に沿った暫定的な設定であり、
        // 論文・仕様書等の一次資料に基づくものではない。実際の回答品質を見ながら調整すること。
        private const int RagChunkSizeDefault = 500;
        private const int RagChunkOverlapDefault = 100;
        private const int RagTopNDefault = 5;

        /// <summary>
        /// OllamaサーバーのベースURL（既定: <c>http://localhost:11434</c>）。
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
