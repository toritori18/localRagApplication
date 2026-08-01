using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalRagApplication.Services.Ollama;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IOllamaClient"/> の手書きテストダブル。実際のOllamaへの通信は行わず、
    /// 呼び出し内容に応じた決定的な値を返す。呼び出し回数・引数を記録し、テストから検証できるようにする。
    /// </summary>
    public class FakeOllamaClient : IOllamaClient
    {
        private readonly Func<string, float[]> _embeddingFactory;
        private readonly string _generatedAnswer;
        private readonly List<IReadOnlyList<string>> _embedCallHistory = new List<IReadOnlyList<string>>();

        /// <summary>
        /// 既定の決定的な埋め込み生成関数と既定の回答文字列で初期化する。
        /// </summary>
        public FakeOllamaClient() : this(null, null)
        {
        }

        /// <summary>
        /// 埋め込み生成関数・回答文字列を指定して初期化する。
        /// </summary>
        /// <param name="embeddingFactory">
        /// テキストから埋め込みベクトルを生成する関数。<c>null</c> の場合は <see cref="DefaultEmbedding"/> を使う。
        /// </param>
        /// <param name="generatedAnswer">
        /// <see cref="GenerateAsync"/> が返す固定文字列。<c>null</c> の場合は既定文字列を使う。
        /// </param>
        public FakeOllamaClient(Func<string, float[]> embeddingFactory, string generatedAnswer)
        {
            _embeddingFactory = embeddingFactory ?? DefaultEmbedding;
            _generatedAnswer = generatedAnswer ?? "フェイク回答";
        }

        /// <summary>
        /// <see cref="EmbedAsync"/> が呼び出された回数。
        /// </summary>
        public int EmbedCallCount { get; private set; }

        /// <summary>
        /// <see cref="GenerateAsync"/> が呼び出された回数。
        /// </summary>
        public int GenerateCallCount { get; private set; }

        /// <summary>
        /// 直近の <see cref="GenerateAsync"/> 呼び出しで渡されたプロンプト。
        /// </summary>
        public string LastPrompt { get; private set; }

        /// <summary>
        /// <see cref="EmbedAsync"/> の呼び出しごとに渡された <c>texts</c> の履歴。
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> EmbedCallHistory
        {
            get { return _embedCallHistory; }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            EmbedCallCount++;
            _embedCallHistory.Add(texts);

            IReadOnlyList<float[]> result = texts.Select(_embeddingFactory).ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<string> GenerateAsync(string prompt)
        {
            GenerateCallCount++;
            LastPrompt = prompt;
            return Task.FromResult(_generatedAnswer);
        }

        /// <summary>
        /// 既定の埋め込み生成関数。テキストに含まれる文字コードの合計から決定的なベクトルを作る。
        /// 出典なし: 実際の埋め込みモデルの計算式とは無関係の、テスト専用の決定的なフェイク実装。
        /// </summary>
        /// <param name="text">埋め込み対象のテキスト。</param>
        /// <returns>決定的に生成された3次元の埋め込みベクトル。</returns>
        private static float[] DefaultEmbedding(string text)
        {
            long sum = 0;
            if (!string.IsNullOrEmpty(text))
            {
                foreach (var c in text)
                {
                    sum += c;
                }
            }

            var seed = (float)(sum % 997) / 997f;
            return new[] { seed, 1f - seed, 0.5f };
        }
    }
}
