using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services
{
    /// <summary>
    /// <see cref="SqliteVectorIndexRepository"/> の単体テスト。実DBを使い、
    /// <c>Path.GetTempFileName()</c> の一時ファイルに接続して検証する。
    /// </summary>
    [TestClass]
    public class SqliteVectorIndexRepositoryTest
    {
        private string _dbFilePath;
        private string _connectionString;
        private SqliteVectorIndexRepository _repository;

        [TestInitialize]
        public void Setup()
        {
            // テストごとに専用の一時DBファイルを使い、他テストと状態を共有しないようにする。
            _dbFilePath = Path.GetTempFileName();
            _connectionString = "Data Source=" + _dbFilePath + ";";
            _repository = new SqliteVectorIndexRepository(_connectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // System.Data.SQLite の接続プールがファイルを掴んだままだと削除に失敗することがあるため、
            // 明示的にプールをクリアしてから一時ファイルを削除する。
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

        private static DocumentChunk CreateChunk(string documentId, int chunkIndex, float[] embedding)
        {
            return new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                ChunkIndex = chunkIndex,
                Text = "チャンク" + chunkIndex,
                Embedding = embedding
            };
        }

        [TestMethod]
        public async Task ReplaceChunksAsync_Embeddingが複数要素負値小数を含む場合もBLOB経由でラウンドトリップする()
        {
            // Arrange: Buffer.BlockCopyによるバイト列変換が壊れやすい箇所のため、
            // 正の整数値・負値・小数・0を混在させたベクトルで検証する。
            var documentId = Guid.NewGuid().ToString();
            var embedding = new float[] { 1.5f, -2.25f, 0f, 100.125f, -0.001f, 3.4028235E38f };
            var chunk = CreateChunk(documentId, 0, embedding);

            // Act
            await _repository.ReplaceChunksAsync(documentId, new[] { chunk });

            // Assert
            var all = await _repository.GetAllAsync();
            var stored = all.Single(c => c.Id == chunk.Id);
            CollectionAssert.AreEqual(embedding, stored.Embedding);
        }

        [TestMethod]
        public async Task ReplaceChunksAsync_同一DocumentIdの既存チャンクだけが削除され別のDocumentIdのチャンクは残る()
        {
            // Arrange
            var documentIdA = Guid.NewGuid().ToString();
            var documentIdB = Guid.NewGuid().ToString();
            var oldChunkA = CreateChunk(documentIdA, 0, new float[] { 1f, 0f });
            var chunkB = CreateChunk(documentIdB, 0, new float[] { 0f, 1f });
            await _repository.ReplaceChunksAsync(documentIdA, new[] { oldChunkA });
            await _repository.ReplaceChunksAsync(documentIdB, new[] { chunkB });

            var newChunkA = CreateChunk(documentIdA, 0, new float[] { 1f, 1f });

            // Act
            await _repository.ReplaceChunksAsync(documentIdA, new[] { newChunkA });

            // Assert
            var all = await _repository.GetAllAsync();
            Assert.IsFalse(all.Any(c => c.Id == oldChunkA.Id));
            Assert.IsTrue(all.Any(c => c.Id == newChunkA.Id));
            Assert.IsTrue(all.Any(c => c.Id == chunkB.Id));
        }

        [TestMethod]
        public async Task ReplaceChunksAsync_空リストを渡すと該当DocumentIdのチャンクが全削除される()
        {
            // Arrange
            var documentId = Guid.NewGuid().ToString();
            var chunk1 = CreateChunk(documentId, 0, new float[] { 1f, 0f });
            var chunk2 = CreateChunk(documentId, 1, new float[] { 0f, 1f });
            await _repository.ReplaceChunksAsync(documentId, new[] { chunk1, chunk2 });

            // Act
            await _repository.ReplaceChunksAsync(documentId, new DocumentChunk[0]);

            // Assert
            var all = await _repository.GetAllAsync();
            Assert.IsFalse(all.Any(c => c.DocumentId == documentId));
        }

        [TestMethod]
        public async Task DeleteByDocumentIdAsync_該当DocumentIdのチャンクのみ削除される()
        {
            // Arrange
            var documentIdA = Guid.NewGuid().ToString();
            var documentIdB = Guid.NewGuid().ToString();
            var chunkA = CreateChunk(documentIdA, 0, new float[] { 1f, 0f });
            var chunkB = CreateChunk(documentIdB, 0, new float[] { 0f, 1f });
            await _repository.ReplaceChunksAsync(documentIdA, new[] { chunkA });
            await _repository.ReplaceChunksAsync(documentIdB, new[] { chunkB });

            // Act
            await _repository.DeleteByDocumentIdAsync(documentIdA);

            // Assert
            var all = await _repository.GetAllAsync();
            Assert.IsFalse(all.Any(c => c.DocumentId == documentIdA));
            Assert.IsTrue(all.Any(c => c.Id == chunkB.Id));
        }

        [TestMethod]
        public void Constructor_connectionStringがnullの場合はArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SqliteVectorIndexRepository(null));
        }

        [TestMethod]
        public async Task ReplaceChunksAsync_documentIdがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _repository.ReplaceChunksAsync(null, new List<DocumentChunk>()));
        }

        [TestMethod]
        public async Task ReplaceChunksAsync_chunksがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _repository.ReplaceChunksAsync(Guid.NewGuid().ToString(), null));
        }

        [TestMethod]
        public async Task DeleteByDocumentIdAsync_documentIdがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _repository.DeleteByDocumentIdAsync(null));
        }
    }
}
