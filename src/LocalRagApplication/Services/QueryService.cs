using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Models;
using LocalRagApplication.Services.Ollama;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <see cref="IQueryService"/> の実装クラス。質問文の埋め込みベクトル化、コサイン類似度による関連チャンク検索、
    /// プロンプト組み立て、Ollamaによる回答生成までのクエリパイプライン全体を行う。
    /// </summary>
    public class QueryService : IQueryService
    {
        private const string NoDocumentsMessage = "先にファイルを取り込んでください。";

        private readonly IVectorIndexRepository _vectorIndexRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IOllamaClient _ollamaClient;

        /// <summary>
        /// 既定の実装（SQLiteリポジトリ・<see cref="OllamaClient"/>）を組み立てて初期化する。
        /// </summary>
        public QueryService()
            : this(new SqliteVectorIndexRepository(), new SqliteDocumentRepository(), new OllamaClient())
        {
        }

        /// <summary>
        /// 各依存コンポーネントを注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="vectorIndexRepository">チャンク・埋め込みベクトルのリポジトリ。</param>
        /// <param name="documentRepository">ドキュメントメタデータのリポジトリ。</param>
        /// <param name="ollamaClient">Ollamaクライアント。</param>
        /// <exception cref="ArgumentNullException">いずれかの引数が null の場合。</exception>
        public QueryService(
            IVectorIndexRepository vectorIndexRepository,
            IDocumentRepository documentRepository,
            IOllamaClient ollamaClient)
        {
            if (vectorIndexRepository == null)
            {
                throw new ArgumentNullException(nameof(vectorIndexRepository));
            }

            if (documentRepository == null)
            {
                throw new ArgumentNullException(nameof(documentRepository));
            }

            if (ollamaClient == null)
            {
                throw new ArgumentNullException(nameof(ollamaClient));
            }

            _vectorIndexRepository = vectorIndexRepository;
            _documentRepository = documentRepository;
            _ollamaClient = ollamaClient;
        }

        /// <inheritdoc />
        public async Task<AnswerResult> AskAsync(string question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var allChunks = await _vectorIndexRepository.GetAllAsync().ConfigureAwait(false);
            if (allChunks.Count == 0)
            {
                // 索引が空の場合は、無駄なOllama通信を避けるためEmbedAsync/GenerateAsyncを一切呼び出さない。
                return new AnswerResult
                {
                    Question = question,
                    Answer = NoDocumentsMessage,
                    Sources = new List<SearchHit>()
                };
            }

            var questionEmbeddings = await _ollamaClient.EmbedAsync(new[] { question }).ConfigureAwait(false);
            var questionEmbedding = questionEmbeddings[0];

            var topChunks = allChunks
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Score = VectorMath.CosineSimilarity(questionEmbedding, chunk.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(RagSettings.RagTopN)
                .ToList();

            var fileNameById = await GetFileNameByDocumentIdAsync().ConfigureAwait(false);

            var sources = topChunks
                .Select(x => new SearchHit
                {
                    Chunk = x.Chunk,
                    Score = x.Score,
                    DocumentFileName = fileNameById.ContainsKey(x.Chunk.DocumentId)
                        ? fileNameById[x.Chunk.DocumentId]
                        : null
                })
                .ToList();

            var prompt = BuildPrompt(question, sources);
            var answer = await _ollamaClient.GenerateAsync(prompt).ConfigureAwait(false);

            return new AnswerResult
            {
                Question = question,
                Answer = answer,
                Sources = sources
            };
        }

        /// <summary>
        /// <see cref="IDocumentRepository.GetAllAsync"/> の結果から、ドキュメントIdをキーとした
        /// ファイル名の対応表を組み立てる。
        /// </summary>
        /// <returns>ドキュメントId → ファイル名の対応表。</returns>
        private async Task<IDictionary<string, string>> GetFileNameByDocumentIdAsync()
        {
            var documents = await _documentRepository.GetAllAsync().ConfigureAwait(false);
            return documents.ToDictionary(d => d.Id, d => d.FileName);
        }

        /// <summary>
        /// 上位チャンクの本文と質問文から、Ollamaに送信するプロンプトを組み立てる。
        /// 文脈にない内容を生成しないよう、「与えられた文脈のみに基づいて回答すること」を明示的に指示する。
        /// </summary>
        /// <param name="question">ユーザーが入力した質問文。</param>
        /// <param name="sources">上位検索ヒットの一覧。</param>
        /// <returns>組み立てられたプロンプト文字列。</returns>
        private static string BuildPrompt(string question, IReadOnlyList<SearchHit> sources)
        {
            var contextBuilder = new StringBuilder();
            for (var i = 0; i < sources.Count; i++)
            {
                contextBuilder.AppendLine(string.Format("[文脈{0}]", i + 1));
                contextBuilder.AppendLine(sources[i].Chunk.Text);
                contextBuilder.AppendLine();
            }

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("あなたはユーザーの質問に答えるアシスタントです。");
            promptBuilder.AppendLine("以下の「文脈」に書かれている情報のみに基づいて、日本語で質問に回答してください。");
            promptBuilder.AppendLine("回答に必要な情報が文脈に含まれていない場合は、推測で答えず「文脈からは分かりません」という主旨を回答してください。");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 文脈");
            promptBuilder.Append(contextBuilder);
            promptBuilder.AppendLine("### 質問");
            promptBuilder.AppendLine(question);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 回答");

            return promptBuilder.ToString();
        }
    }
}
