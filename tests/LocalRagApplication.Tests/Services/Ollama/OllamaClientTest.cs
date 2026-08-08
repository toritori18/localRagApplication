using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Services.Ollama;
using LocalRagApplication.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalRagApplication.Tests.Services.Ollama
{
    /// <summary>
    /// <see cref="OllamaClient"/> の単体テスト。実通信は行わず、
    /// <see cref="FakeHttpMessageHandler"/> を注入する専用コンストラクタ
    /// （<see cref="OllamaClient(HttpMessageHandler, IQueryMetricsLogger)"/>）経由でHTTP通信をフェイクに差し替える。
    /// </summary>
    [TestClass]
    public class OllamaClientTest
    {
        [TestMethod]
        public async Task EmbedAsync_レスポンスJSONをfloat配列の一覧にマッピングする()
        {
            // Arrange
            var expected = new[] { new float[] { 0.5f, -1f }, new float[] { 2f, 3.5f } };
            var handler = new FakeHttpMessageHandler(BuildEmbedResponseJson(expected));
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var result = await client.EmbedAsync(new[] { "テキストA", "テキストB" });

            // Assert
            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEqual(expected[0], result[0]);
            CollectionAssert.AreEqual(expected[1], result[1]);
        }

        [TestMethod]
        public async Task EmbedAsync_17件渡すと16件と1件の2リクエストに分割され入力と同じ順序で返る()
        {
            // Arrange: EmbedBatchSize（16）の境界を確認するため17件を渡す。
            var texts = Enumerable.Range(0, 17).Select(i => "text" + i).ToArray();
            var firstBatchEmbeddings = Enumerable.Range(0, 16).Select(i => new float[] { i }).ToArray();
            var secondBatchEmbeddings = new[] { new float[] { 16f } };
            var handler = new FakeHttpMessageHandler(new[]
            {
                BuildEmbedResponseJson(firstBatchEmbeddings),
                BuildEmbedResponseJson(secondBatchEmbeddings)
            });
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var result = await client.EmbedAsync(texts);

            // Assert: 2回のリクエストに分割され、1回目は16件・2回目は1件のテキストが送信される。
            Assert.AreEqual(2, handler.Requests.Count);
            var firstRequestInput = (JArray)JObject.Parse(handler.Requests[0].Body)["input"];
            var secondRequestInput = (JArray)JObject.Parse(handler.Requests[1].Body)["input"];
            Assert.AreEqual(16, firstRequestInput.Count);
            Assert.AreEqual(1, secondRequestInput.Count);

            // 結果は入力と同じ順序（0..16）で17件返る。
            Assert.AreEqual(17, result.Count);
            for (var i = 0; i < 17; i++)
            {
                Assert.AreEqual((float)i, result[i][0]);
            }
        }

        [TestMethod]
        public async Task EmbedAsync_リクエスト本文のmodelがOllamaEmbeddingModelになる()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(BuildEmbedResponseJson(new[] { new float[] { 1f } }));
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            await client.EmbedAsync(new[] { "テキスト" });

            // Assert
            var requestBody = JObject.Parse(handler.Requests[0].Body);
            Assert.AreEqual(RagSettings.OllamaEmbeddingModel, (string)requestBody["model"]);
        }

        [TestMethod]
        public async Task GenerateAsync_レスポンスのresponseフィールドを返す()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(BuildGenerateResponseJson("生成された回答テキスト"));
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var result = await client.GenerateAsync("質問プロンプト");

            // Assert
            Assert.AreEqual("生成された回答テキスト", result);
        }

        [TestMethod]
        public async Task GenerateAsync_リクエスト本文にstreamがfalseで指定される()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(BuildGenerateResponseJson("回答"));
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            await client.GenerateAsync("プロンプト");

            // Assert
            var requestBody = JObject.Parse(handler.Requests[0].Body);
            Assert.AreEqual(false, (bool)requestBody["stream"]);
        }

        [TestMethod]
        public async Task EmbedAsync_HttpRequestExceptionはOllamaConnectionExceptionにラップされる()
        {
            // Arrange
            var originalException = new HttpRequestException("接続失敗");
            var handler = new FakeHttpMessageHandler(originalException);
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var thrown = await Assert.ThrowsExceptionAsync<OllamaConnectionException>(
                () => client.EmbedAsync(new[] { "テキスト" }));

            // Assert: InnerExceptionとして元の例外がそのまま保持されている
            Assert.AreSame(originalException, thrown.InnerException);
        }

        [TestMethod]
        public async Task EmbedAsync_非2xx応答はOllamaConnectionExceptionにラップされHttpRequestExceptionを保持する()
        {
            // Arrange: EnsureSuccessStatusCode() が非2xx応答でHttpRequestExceptionをスローする本番の挙動を
            // 変えていないことを確認する。
            var handler = new FakeHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var thrown = await Assert.ThrowsExceptionAsync<OllamaConnectionException>(
                () => client.EmbedAsync(new[] { "テキスト" }));

            // Assert
            Assert.IsInstanceOfType(thrown.InnerException, typeof(HttpRequestException));
        }

        [TestMethod]
        public async Task GenerateAsync_TaskCanceledExceptionはOllamaConnectionExceptionにラップされる()
        {
            // Arrange: HttpClient.Timeout超過時はHttpRequestExceptionではなくTaskCanceledExceptionがスローされる。
            // なお TaskCanceledException（OperationCanceledExceptionのサブクラス）は、非同期メソッド内でスローされると
            // Taskが（Faultedではなく）Canceled状態になり、呼び出し元がawaitした時点で新しいTaskCanceledException
            // インスタンスが生成される（元の例外インスタンスへの参照は失われる）というTPLの既定動作があるため、
            // HttpRequestExceptionのケースとは異なり、InnerExceptionの参照一致ではなく型で検証する
            // （出典: .NET Framework 4.8 実機での動作確認。System.Net.Http.dllを参照した最小限のHttpClient経由の
            // 再現コードで、FakeHttpMessageHandler相当の非同期ハンドラーがTaskCanceledExceptionをスローした場合に
            // 呼び出し元が受け取る例外が別インスタンスになることを確認済み）。
            var originalException = new TaskCanceledException("タイムアウト");
            var handler = new FakeHttpMessageHandler(originalException);
            var client = new OllamaClient(handler, new FakeQueryMetricsLogger());

            // Act
            var thrown = await Assert.ThrowsExceptionAsync<OllamaConnectionException>(
                () => client.GenerateAsync("プロンプト"));

            // Assert
            Assert.IsInstanceOfType(thrown.InnerException, typeof(TaskCanceledException));
        }

        [TestMethod]
        public async Task GenerateAsync_evalDurationが0のレスポンスでもゼロ除算せず例外なく完了する()
        {
            // Arrange: eval_durationが0（フィールド欠損時のデシリアライズ結果と同じ状況）のレスポンスを返す。
            var responseJson = JsonConvert.SerializeObject(new
            {
                response = "回答",
                total_duration = 200000000L,
                load_duration = 50000000L,
                prompt_eval_count = 3,
                prompt_eval_duration = 100000000L,
                eval_count = 5,
                eval_duration = 0L
            });
            var handler = new FakeHttpMessageHandler(responseJson);
            var metricsLogger = new FakeQueryMetricsLogger();
            var client = new OllamaClient(handler, metricsLogger);

            // Act
            var result = await client.GenerateAsync("プロンプト");

            // Assert: 例外なく完了し、スループットは0として記録される
            Assert.AreEqual("回答", result);
            Assert.AreEqual(1, metricsLogger.Messages.Count);
            StringAssert.Contains(metricsLogger.Messages[0], "throughput=0.0tok/s");
        }

        [TestMethod]
        public async Task EmbedAsync_textsがnullの場合はArgumentNullExceptionをスローする()
        {
            var client = new OllamaClient(new FakeHttpMessageHandler("{}"), new FakeQueryMetricsLogger());

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.EmbedAsync(null));
        }

        [TestMethod]
        public async Task GenerateAsync_promptがnullの場合はArgumentNullExceptionをスローする()
        {
            var client = new OllamaClient(new FakeHttpMessageHandler("{}"), new FakeQueryMetricsLogger());

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.GenerateAsync(null));
        }

        [TestMethod]
        public void Constructor_metricsLoggerがnullの場合はArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new OllamaClient((IQueryMetricsLogger)null));
        }

        [TestMethod]
        public void Constructor_httpMessageHandlerのみを渡す場合にhttpMessageHandlerがnullだとArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new OllamaClient((HttpMessageHandler)null));
        }

        [TestMethod]
        public void Constructor_httpMessageHandlerとmetricsLoggerを渡す場合にhttpMessageHandlerがnullだとArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new OllamaClient((HttpMessageHandler)null, new FakeQueryMetricsLogger()));
        }

        [TestMethod]
        public void Constructor_httpMessageHandlerとmetricsLoggerを渡す場合にmetricsLoggerがnullだとArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new OllamaClient(new FakeHttpMessageHandler("{}"), null));
        }

        /// <summary>
        /// <c>/api/embed</c> のレスポンスJSON（<c>{"embeddings": [[...], ...]}</c>）を組み立てる。
        /// </summary>
        /// <param name="embeddings">返す埋め込みベクトルの一覧。</param>
        /// <returns>シリアライズ済みのレスポンスJSON文字列。</returns>
        private static string BuildEmbedResponseJson(IEnumerable<float[]> embeddings)
        {
            return JsonConvert.SerializeObject(new { embeddings = embeddings.ToArray() });
        }

        /// <summary>
        /// <c>/api/generate</c> のレスポンスJSON（<c>{"response": "..."}</c>）を組み立てる。
        /// </summary>
        /// <param name="responseText">返す回答テキスト。</param>
        /// <returns>シリアライズ済みのレスポンスJSON文字列。</returns>
        private static string BuildGenerateResponseJson(string responseText)
        {
            return JsonConvert.SerializeObject(new { response = responseText });
        }
    }
}
