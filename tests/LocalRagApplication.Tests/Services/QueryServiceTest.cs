using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using LocalRagApplication.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services
{
    [TestClass]
    public class QueryServiceTest
    {
        private string _dbFilePath;
        private SqliteDocumentRepository _documentRepository;
        private SqliteVectorIndexRepository _vectorIndexRepository;

        [TestInitialize]
        public void Setup()
        {
            _dbFilePath = Path.GetTempFileName();
            var connectionString = "Data Source=" + _dbFilePath + ";";
            _documentRepository = new SqliteDocumentRepository(connectionString);
            _vectorIndexRepository = new SqliteVectorIndexRepository(connectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();

            try
            {
                if (File.Exists(_dbFilePath))
                {
                    File.Delete(_dbFilePath);
                }
            }
            catch (IOException)
            {
                // 一時ファイルの削除失敗はテスト結果に影響しないため無視する。
            }
        }

        [TestMethod]
        public async Task AskAsync_コサイン類似度が高い順にTopN件のSourcesが返る()
        {
            // Arrange: 質問ベクトルに近い順で「一致度高」「一致度中」「一致度低」の3チャンクを用意する。
            var documentId = Guid.NewGuid().ToString();
            await _documentRepository.UpsertAsync(new DocumentMetadata
            {
                Id = documentId,
                FileName = "sample.txt",
                FileType = ".txt",
                FileSizeBytes = 100,
                UploadedAtUtc = DateTime.UtcNow,
                Status = DocumentStatus.Indexed,
                IndexedAtUtc = DateTime.UtcNow,
                ChunkCount = 3
            });

            var questionVector = new float[] { 1f, 0f, 0f };
            var chunkHigh = new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                ChunkIndex = 0,
                Text = "一致度高チャンク",
                Embedding = new float[] { 1f, 0f, 0f }
            };
            var chunkMid = new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                ChunkIndex = 1,
                Text = "一致度中チャンク",
                Embedding = new float[] { 1f, 1f, 0f }
            };
            var chunkLow = new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                ChunkIndex = 2,
                Text = "一致度低チャンク",
                Embedding = new float[] { 0f, 1f, 0f }
            };

            // 登録順をわざとバラバラにして、ソート結果が本当に類似度に基づくことを確認する。
            await _vectorIndexRepository.ReplaceChunksAsync(
                documentId, new[] { chunkLow, chunkHigh, chunkMid });

            var embeddingByText = new Dictionary<string, float[]> { { "質問文", questionVector } };
            var ollamaClient = new FakeOllamaClient(text => embeddingByText[text], "フェイク回答");
            var queryService = new QueryService(
                _vectorIndexRepository, _documentRepository, ollamaClient, new FakeQueryMetricsLogger());

            // Act
            var result = await queryService.AskAsync("質問文");

            // Assert
            Assert.AreEqual(3, result.Sources.Count);
            Assert.AreEqual(chunkHigh.Id, result.Sources[0].Chunk.Id);
            Assert.AreEqual(chunkMid.Id, result.Sources[1].Chunk.Id);
            Assert.AreEqual(chunkLow.Id, result.Sources[2].Chunk.Id);
            Assert.IsTrue(result.Sources[0].Score > result.Sources[1].Score);
            Assert.IsTrue(result.Sources[1].Score > result.Sources[2].Score);
            Assert.AreEqual("sample.txt", result.Sources[0].DocumentFileName);
            Assert.AreEqual(1, ollamaClient.EmbedCallCount);
            Assert.AreEqual(1, ollamaClient.GenerateCallCount);
        }

        [TestMethod]
        public async Task AskAsync_TopNを超えるチャンクがある場合は上位RagTopN件に絞られる()
        {
            // Arrange: 既定のRagTopN（5件）を超える数のチャンクを登録し、上位5件のみが返ることを確認する。
            var documentId = Guid.NewGuid().ToString();
            await _documentRepository.UpsertAsync(new DocumentMetadata
            {
                Id = documentId,
                FileName = "sample.txt",
                FileType = ".txt",
                FileSizeBytes = 100,
                UploadedAtUtc = DateTime.UtcNow,
                Status = DocumentStatus.Indexed,
                IndexedAtUtc = DateTime.UtcNow,
                ChunkCount = 7
            });

            var questionVector = new float[] { 1f, 0f };
            var chunks = new List<DocumentChunk>();
            for (var i = 0; i < 7; i++)
            {
                // 角度を少しずつ変え、スコアに明確な差をつける。
                var angle = i * 0.1;
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentId = documentId,
                    ChunkIndex = i,
                    Text = "チャンク" + i,
                    Embedding = new[] { (float)Math.Cos(angle), (float)Math.Sin(angle) }
                });
            }

            await _vectorIndexRepository.ReplaceChunksAsync(documentId, chunks);

            var embeddingByText = new Dictionary<string, float[]> { { "質問文", questionVector } };
            var ollamaClient = new FakeOllamaClient(text => embeddingByText[text], "フェイク回答");
            var queryService = new QueryService(
                _vectorIndexRepository, _documentRepository, ollamaClient, new FakeQueryMetricsLogger());

            // Act
            var result = await queryService.AskAsync("質問文");

            // Assert
            Assert.AreEqual(RagSettings.RagTopN, result.Sources.Count);
        }

        [TestMethod]
        public async Task AskAsync_索引が空の場合はOllamaを呼び出さず案内メッセージを返す()
        {
            // Arrange
            var ollamaClient = new FakeOllamaClient();
            var queryService = new QueryService(
                _vectorIndexRepository, _documentRepository, ollamaClient, new FakeQueryMetricsLogger());

            // Act
            var result = await queryService.AskAsync("質問文");

            // Assert
            Assert.AreEqual(0, ollamaClient.EmbedCallCount);
            Assert.AreEqual(0, ollamaClient.GenerateCallCount);
            Assert.AreEqual(0, result.Sources.Count);
            Assert.IsFalse(string.IsNullOrEmpty(result.Answer));
        }
    }
}
