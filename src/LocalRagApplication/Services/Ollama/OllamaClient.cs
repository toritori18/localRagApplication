using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using Newtonsoft.Json;

namespace LocalRagApplication.Services.Ollama
{
    /// <summary>
    /// <see cref="IOllamaClient"/> の実装クラス。<see cref="HttpClient"/> を用いてOllamaのREST API
    /// （<c>/api/embed</c>・<c>/api/generate</c>）を呼び出す。
    /// </summary>
    public class OllamaClient : IOllamaClient
    {
        // 出典なし・暫定値。1回のリクエストが大きくなりすぎないよう分割するための件数であり、
        // 一次資料に基づく値ではない（プランの「例: 16件」という記載に沿った暫定値）。
        private const int EmbedBatchSize = 16;

        // Ollama公式APIドキュメントに "All durations are returned in nanoseconds." と明記されている。
        // ログにはミリ秒換算で出力するための除数。
        private const long NanosecondsPerMillisecond = 1000000;

        // ソケット枯渇を避けるため、HttpClientはアプリケーション全体で1インスタンスを共有する。
        private static readonly HttpClient HttpClientInstance = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private readonly HttpClient _httpClient;
        private readonly IQueryMetricsLogger _metricsLogger;

        /// <summary>
        /// 既定の内訳ログ出力先（<see cref="FileQueryMetricsLogger"/>）を使って初期化する。
        /// </summary>
        public OllamaClient() : this(new FileQueryMetricsLogger())
        {
        }

        /// <summary>
        /// 内訳ログの記録先を注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="metricsLogger">処理時間内訳の記録先。</param>
        /// <exception cref="ArgumentNullException"><paramref name="metricsLogger"/> が null の場合。</exception>
        public OllamaClient(IQueryMetricsLogger metricsLogger)
        {
            if (metricsLogger == null)
            {
                throw new ArgumentNullException(nameof(metricsLogger));
            }

            _httpClient = HttpClientInstance;
            _metricsLogger = metricsLogger;
        }

        /// <summary>
        /// HTTP通信を行う <see cref="HttpMessageHandler"/> と、既定の内訳ログ出力先
        /// （<see cref="FileQueryMetricsLogger"/>）を使って初期化する（テスト等で実通信を行わずレスポンスを
        /// 固定する場合を想定）。
        /// </summary>
        /// <param name="httpMessageHandler">HTTP送信を差し替えるハンドラー（テストダブル等）。</param>
        /// <exception cref="ArgumentNullException"><paramref name="httpMessageHandler"/> が null の場合。</exception>
        public OllamaClient(HttpMessageHandler httpMessageHandler) : this(httpMessageHandler, new FileQueryMetricsLogger())
        {
        }

        /// <summary>
        /// HTTP通信を行う <see cref="HttpMessageHandler"/> と内訳ログの記録先の両方を注入して初期化する
        /// （テスト等でHTTP通信・ロギングの両方をフェイクに差し替える場合を想定）。
        /// </summary>
        /// <remarks>
        /// 既定コンストラクタ（<see cref="OllamaClient()"/> ・ <see cref="OllamaClient(IQueryMetricsLogger)"/>）は
        /// ソケット枯渇を避けるためアプリケーション全体で共有される static な <see cref="HttpClient"/>
        /// （<see cref="HttpClientInstance"/>）をそのまま使い続ける。一方、このコンストラクタで生成した
        /// インスタンスは渡された <paramref name="httpMessageHandler"/> をラップした専用の <see cref="HttpClient"/>
        /// を新規に作成する。テストでは実通信を行わないため、共有インスタンスを使い回すメリット（ソケット枯渇の回避）が
        /// 意味を持たない一方、テストごとに異なるハンドラー（固定レスポンス・例外スロー等）を差し込む必要があるため。
        /// </remarks>
        /// <param name="httpMessageHandler">HTTP送信を差し替えるハンドラー（テストダブル等）。</param>
        /// <param name="metricsLogger">処理時間内訳の記録先。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="httpMessageHandler"/> または <paramref name="metricsLogger"/> が null の場合。
        /// </exception>
        public OllamaClient(HttpMessageHandler httpMessageHandler, IQueryMetricsLogger metricsLogger)
        {
            if (httpMessageHandler == null)
            {
                throw new ArgumentNullException(nameof(httpMessageHandler));
            }

            if (metricsLogger == null)
            {
                throw new ArgumentNullException(nameof(metricsLogger));
            }

            _httpClient = new HttpClient(httpMessageHandler);
            _metricsLogger = metricsLogger;
        }

        /// <summary>
        /// 複数のテキストをまとめて埋め込みベクトル化する。1回のリクエストが大きくなりすぎないよう、
        /// <see cref="EmbedBatchSize"/> 件ごとに分割して <c>/api/embed</c> を複数回呼び出す。
        /// </summary>
        /// <param name="texts">埋め込み対象のテキスト一覧。</param>
        /// <returns><paramref name="texts"/> と同じ順序で並んだ埋め込みベクトルの一覧。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="texts"/> が null の場合。</exception>
        /// <exception cref="OllamaConnectionException">Ollamaサーバーに接続できない、またはタイムアウトした場合。</exception>
        public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            var results = new List<float[]>(texts.Count);
            for (var offset = 0; offset < texts.Count; offset += EmbedBatchSize)
            {
                var batch = texts.Skip(offset).Take(EmbedBatchSize).ToArray();
                var batchResults = await EmbedBatchAsync(batch).ConfigureAwait(false);
                results.AddRange(batchResults);
            }

            return results;
        }

        /// <summary>
        /// プロンプトから回答テキストを生成する。<c>/api/generate</c> の <c>stream</c> は既定値が
        /// <c>true</c> のため、一括で応答を受け取れるよう明示的に <c>false</c> を指定する。
        /// </summary>
        /// <param name="prompt">Ollamaに送信するプロンプト文字列。</param>
        /// <returns>生成された回答テキスト。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prompt"/> が null の場合。</exception>
        /// <exception cref="OllamaConnectionException">Ollamaサーバーに接続できない、またはタイムアウトした場合。</exception>
        public async Task<string> GenerateAsync(string prompt)
        {
            if (prompt == null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            var request = new GenerateRequest
            {
                Model = RagSettings.OllamaGenerationModel,
                Prompt = prompt,
                Stream = false,
                KeepAlive = RagSettings.OllamaKeepAlive
            };

            var url = BuildUrl("/api/generate");
            var stopwatch = Stopwatch.StartNew();
            var responseBody = await PostJsonAsync(url, JsonConvert.SerializeObject(request)).ConfigureAwait(false);
            stopwatch.Stop();

            var generateResponse = JsonConvert.DeserializeObject<GenerateResponse>(responseBody);
            LogGenerateMetrics(generateResponse, stopwatch.ElapsedMilliseconds);
            return generateResponse.Response;
        }

        /// <summary>
        /// <c>/api/embed</c> を1回呼び出し、渡されたテキスト群の埋め込みベクトルを取得する。
        /// （リクエストは <c>{"model": ..., "input": [...]}</c>、レスポンスは <c>{"embeddings": [[...], ...]}</c>）
        /// </summary>
        /// <param name="texts">埋め込み対象のテキスト一覧（1バッチ分）。</param>
        /// <returns><paramref name="texts"/> と同じ順序で並んだ埋め込みベクトルの一覧。</returns>
        private async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts)
        {
            var request = new EmbedRequest
            {
                Model = RagSettings.OllamaEmbeddingModel,
                Input = texts.ToArray(),
                KeepAlive = RagSettings.OllamaKeepAlive
            };

            var url = BuildUrl("/api/embed");
            var stopwatch = Stopwatch.StartNew();
            var responseBody = await PostJsonAsync(url, JsonConvert.SerializeObject(request)).ConfigureAwait(false);
            stopwatch.Stop();

            var embedResponse = JsonConvert.DeserializeObject<EmbedResponse>(responseBody);
            LogEmbedMetrics(embedResponse, texts.Count, stopwatch.ElapsedMilliseconds);
            return embedResponse.Embeddings;
        }

        /// <summary>
        /// <see cref="RagSettings.OllamaBaseUrl"/> と指定したパスからリクエストURLを組み立てる。
        /// </summary>
        /// <param name="path">先頭に <c>/</c> を含むAPIパス（例: <c>/api/embed</c>）。</param>
        /// <returns>組み立てられたURL文字列。</returns>
        private static string BuildUrl(string path)
        {
            return RagSettings.OllamaBaseUrl.TrimEnd('/') + path;
        }

        /// <summary>
        /// 指定したURLにJSONをPOSTし、レスポンス本文を文字列として返す。
        /// Ollamaに接続できない場合・タイムアウトした場合は、呼び出し元が
        /// HTTP通信の詳細に依存せず対処できるよう <see cref="OllamaConnectionException"/> にラップして再スローする。
        /// </summary>
        /// <param name="url">送信先URL。</param>
        /// <param name="json">送信するJSON文字列。</param>
        /// <returns>レスポンス本文の文字列。</returns>
        /// <exception cref="OllamaConnectionException">Ollamaサーバーに接続できない、またはタイムアウトした場合。</exception>
        private async Task<string> PostJsonAsync(string url, string json)
        {
            try
            {
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new OllamaConnectionException(
                    "Ollamaサーバーに接続できませんでした。Ollamaが起動しているか確認してください。",
                    ex);
            }
            catch (TaskCanceledException ex)
            {
                // HttpClient.Timeout超過時は HttpRequestException ではなく TaskCanceledException がスローされる。
                throw new OllamaConnectionException(
                    "Ollamaサーバーへの応答がタイムアウトしました。Ollamaが起動しているか確認してください。",
                    ex);
            }
        }

        /// <summary>
        /// <c>/api/embed</c> の処理時間内訳をログに出力する。取り込み処理（<c>DocumentIngestionService</c>）からも
        /// 呼び出されるため、同じログファイルに出力される他の行と区別できるよう <c>op=embed</c> を付与する。
        /// </summary>
        /// <param name="response">デシリアライズ済みのレスポンス。</param>
        /// <param name="textCount">今回のリクエストで送信したテキスト件数。</param>
        /// <param name="wallMilliseconds">HTTP往復にかかった実測時間（ミリ秒）。</param>
        private void LogEmbedMetrics(EmbedResponse response, int textCount, long wallMilliseconds)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "op=embed model={0} texts={1} wall={2}ms total={3}ms load={4}ms prompt_eval={5}tok",
                RagSettings.OllamaEmbeddingModel,
                textCount,
                wallMilliseconds,
                response.TotalDuration / NanosecondsPerMillisecond,
                response.LoadDuration / NanosecondsPerMillisecond,
                response.PromptEvalCount);

            _metricsLogger.LogMetrics(message);
        }

        /// <summary>
        /// <c>/api/generate</c> の処理時間内訳をログに出力する。<c>op=generate</c> を付与し、
        /// <c>eval_count / eval_duration</c> から生成スループット（tok/s）も併せて記録する。
        /// </summary>
        /// <param name="response">デシリアライズ済みのレスポンス。</param>
        /// <param name="wallMilliseconds">HTTP往復にかかった実測時間（ミリ秒）。</param>
        private void LogGenerateMetrics(GenerateResponse response, long wallMilliseconds)
        {
            var promptEvalMilliseconds = response.PromptEvalDuration / NanosecondsPerMillisecond;
            var evalMilliseconds = response.EvalDuration / NanosecondsPerMillisecond;

            // 古いOllamaやフィールド欠損時は eval_duration が 0 のままデシリアライズされるため、
            // ゼロ除算を避けるためにガードする。
            var throughput = 0d;
            if (response.EvalDuration > 0)
            {
                var evalDurationSeconds = response.EvalDuration / (double)(NanosecondsPerMillisecond * 1000);
                throughput = response.EvalCount / evalDurationSeconds;
            }

            var message = string.Format(
                CultureInfo.InvariantCulture,
                "op=generate model={0} wall={1}ms total={2}ms load={3}ms prompt_eval={4}ms/{5}tok eval={6}ms/{7}tok throughput={8}tok/s",
                RagSettings.OllamaGenerationModel,
                wallMilliseconds,
                response.TotalDuration / NanosecondsPerMillisecond,
                response.LoadDuration / NanosecondsPerMillisecond,
                promptEvalMilliseconds,
                response.PromptEvalCount,
                evalMilliseconds,
                response.EvalCount,
                throughput.ToString("F1", CultureInfo.InvariantCulture));

            _metricsLogger.LogMetrics(message);
        }

        /// <summary>
        /// <c>/api/embed</c> のリクエストボディ。
        /// </summary>
        private class EmbedRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("input")]
            public string[] Input { get; set; }

            [JsonProperty("keep_alive")]
            public string KeepAlive { get; set; }
        }

        /// <summary>
        /// <c>/api/embed</c> のレスポンスボディ。
        /// </summary>
        private class EmbedResponse
        {
            [JsonProperty("embeddings")]
            public float[][] Embeddings { get; set; }

            [JsonProperty("total_duration")]
            public long TotalDuration { get; set; }

            [JsonProperty("load_duration")]
            public long LoadDuration { get; set; }

            [JsonProperty("prompt_eval_count")]
            public int PromptEvalCount { get; set; }
        }

        /// <summary>
        /// <c>/api/generate</c> のリクエストボディ。
        /// </summary>
        private class GenerateRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("prompt")]
            public string Prompt { get; set; }

            [JsonProperty("stream")]
            public bool Stream { get; set; }

            [JsonProperty("keep_alive")]
            public string KeepAlive { get; set; }
        }

        /// <summary>
        /// <c>/api/generate</c> のレスポンスボディ。
        /// </summary>
        private class GenerateResponse
        {
            [JsonProperty("response")]
            public string Response { get; set; }

            [JsonProperty("total_duration")]
            public long TotalDuration { get; set; }

            [JsonProperty("load_duration")]
            public long LoadDuration { get; set; }

            [JsonProperty("prompt_eval_count")]
            public int PromptEvalCount { get; set; }

            [JsonProperty("prompt_eval_duration")]
            public long PromptEvalDuration { get; set; }

            [JsonProperty("eval_count")]
            public int EvalCount { get; set; }

            [JsonProperty("eval_duration")]
            public long EvalDuration { get; set; }
        }
    }
}
