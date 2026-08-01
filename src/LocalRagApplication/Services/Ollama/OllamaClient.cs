using System;
using System.Collections.Generic;
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

        // ソケット枯渇を避けるため、HttpClientはアプリケーション全体で1インスタンスを共有する。
        private static readonly HttpClient HttpClientInstance = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

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
                Stream = false
            };

            var url = BuildUrl("/api/generate");
            var responseBody = await PostJsonAsync(url, JsonConvert.SerializeObject(request)).ConfigureAwait(false);
            var generateResponse = JsonConvert.DeserializeObject<GenerateResponse>(responseBody);
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
                Input = texts.ToArray()
            };

            var url = BuildUrl("/api/embed");
            var responseBody = await PostJsonAsync(url, JsonConvert.SerializeObject(request)).ConfigureAwait(false);
            var embedResponse = JsonConvert.DeserializeObject<EmbedResponse>(responseBody);
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
        private static async Task<string> PostJsonAsync(string url, string json)
        {
            try
            {
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await HttpClientInstance.PostAsync(url, content).ConfigureAwait(false))
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
        /// <c>/api/embed</c> のリクエストボディ。
        /// </summary>
        private class EmbedRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("input")]
            public string[] Input { get; set; }
        }

        /// <summary>
        /// <c>/api/embed</c> のレスポンスボディ。
        /// </summary>
        private class EmbedResponse
        {
            [JsonProperty("embeddings")]
            public float[][] Embeddings { get; set; }
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
        }

        /// <summary>
        /// <c>/api/generate</c> のレスポンスボディ。
        /// </summary>
        private class GenerateResponse
        {
            [JsonProperty("response")]
            public string Response { get; set; }
        }
    }
}
