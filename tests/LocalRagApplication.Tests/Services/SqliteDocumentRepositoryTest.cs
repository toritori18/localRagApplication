using System;
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
    /// <see cref="SqliteDocumentRepository"/> の単体テスト。実DBを使い、
    /// <c>Path.GetTempFileName()</c> の一時ファイルに接続して検証する。
    /// </summary>
    [TestClass]
    public class SqliteDocumentRepositoryTest
    {
        private string _dbFilePath;
        private string _connectionString;
        private SqliteDocumentRepository _repository;

        [TestInitialize]
        public void Setup()
        {
            // テストごとに専用の一時DBファイルを使い、他テストと状態を共有しないようにする。
            _dbFilePath = Path.GetTempFileName();
            _connectionString = "Data Source=" + _dbFilePath + ";";
            _repository = new SqliteDocumentRepository(_connectionString);
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

        private static DocumentMetadata CreateSampleDocument(string id, string fileName)
        {
            return new DocumentMetadata
            {
                Id = id,
                FileName = fileName,
                FileType = ".txt",
                FileSizeBytes = 123,
                UploadedAtUtc = DateTime.UtcNow,
                Status = DocumentStatus.Indexed,
                IndexedAtUtc = DateTime.UtcNow,
                ChunkCount = 3,
                ErrorMessage = null
            };
        }

        [TestMethod]
        public async Task UpsertAsync_新規ドキュメントが挿入される()
        {
            // Arrange
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "sample.txt");

            // Act
            await _repository.UpsertAsync(document);

            // Assert
            var stored = await _repository.FindByFileNameAsync("sample.txt");
            Assert.IsNotNull(stored);
            Assert.AreEqual(document.Id, stored.Id);
            Assert.AreEqual(document.FileType, stored.FileType);
            Assert.AreEqual(document.FileSizeBytes, stored.FileSizeBytes);
            Assert.AreEqual(document.ChunkCount, stored.ChunkCount);
        }

        [TestMethod]
        public async Task UpsertAsync_同一Idで再度呼ぶと行が増えず内容が置き換わる()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var original = CreateSampleDocument(id, "original.txt");
            await _repository.UpsertAsync(original);

            var updated = CreateSampleDocument(id, "updated.txt");
            updated.Status = DocumentStatus.Error;
            updated.ChunkCount = 0;
            updated.ErrorMessage = "更新後のエラー";

            // Act
            await _repository.UpsertAsync(updated);

            // Assert
            var all = await _repository.GetAllAsync();
            Assert.AreEqual(1, all.Count(d => d.Id == id));

            var stored = await _repository.FindByFileNameAsync("updated.txt");
            Assert.IsNotNull(stored);
            Assert.AreEqual(DocumentStatus.Error, stored.Status);
            Assert.AreEqual(0, stored.ChunkCount);
            Assert.AreEqual("更新後のエラー", stored.ErrorMessage);

            var originalNameLookup = await _repository.FindByFileNameAsync("original.txt");
            Assert.IsNull(originalNameLookup);
        }

        [TestMethod]
        public async Task FindByFileNameAsync_一致するファイル名がある場合はドキュメントを返す()
        {
            // Arrange
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "target.txt");
            await _repository.UpsertAsync(document);

            // Act
            var result = await _repository.FindByFileNameAsync("target.txt");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(document.Id, result.Id);
        }

        [TestMethod]
        public async Task FindByFileNameAsync_一致するファイル名がない場合はnullを返す()
        {
            // Act
            var result = await _repository.FindByFileNameAsync("not-exists.txt");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAllAsync_複数件のドキュメントを返す()
        {
            // Arrange
            var first = CreateSampleDocument(Guid.NewGuid().ToString(), "first.txt");
            var second = CreateSampleDocument(Guid.NewGuid().ToString(), "second.txt");
            var third = CreateSampleDocument(Guid.NewGuid().ToString(), "third.txt");
            await _repository.UpsertAsync(first);
            await _repository.UpsertAsync(second);
            await _repository.UpsertAsync(third);

            // Act
            var all = await _repository.GetAllAsync();

            // Assert
            Assert.AreEqual(3, all.Count);
            CollectionAssert.AreEquivalent(
                new[] { first.Id, second.Id, third.Id }, all.Select(d => d.Id).ToList());
        }

        [TestMethod]
        public async Task DeleteAsync_該当行が削除される()
        {
            // Arrange
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "delete-target.txt");
            await _repository.UpsertAsync(document);

            // Act
            await _repository.DeleteAsync(document.Id);

            // Assert
            var stored = await _repository.FindByFileNameAsync("delete-target.txt");
            Assert.IsNull(stored);

            var all = await _repository.GetAllAsync();
            Assert.AreEqual(0, all.Count);
        }

        [TestMethod]
        public async Task UpsertAsync_IndexedAtUtcがnullの場合はラウンドトリップする()
        {
            // Arrange
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "no-index.txt");
            document.IndexedAtUtc = null;

            // Act
            await _repository.UpsertAsync(document);

            // Assert
            var stored = await _repository.FindByFileNameAsync("no-index.txt");
            Assert.IsNotNull(stored);
            Assert.IsNull(stored.IndexedAtUtc);
        }

        [TestMethod]
        public async Task UpsertAsync_IndexedAtUtcに値がある場合はUtcとしてラウンドトリップする()
        {
            // Arrange
            var indexedAt = DateTime.UtcNow;
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "indexed.txt");
            document.IndexedAtUtc = indexedAt;

            // Act
            await _repository.UpsertAsync(document);

            // Assert
            var stored = await _repository.FindByFileNameAsync("indexed.txt");
            Assert.IsNotNull(stored);
            Assert.IsTrue(stored.IndexedAtUtc.HasValue);
            Assert.AreEqual(indexedAt, stored.IndexedAtUtc.Value);
            Assert.AreEqual(DateTimeKind.Utc, stored.IndexedAtUtc.Value.Kind);
        }

        [TestMethod]
        public async Task UpsertAsync_ErrorMessageがnullの場合はDBNullとして保存され読み出しでnullに戻る()
        {
            // Arrange
            var document = CreateSampleDocument(Guid.NewGuid().ToString(), "no-error.txt");
            document.ErrorMessage = null;

            // Act
            await _repository.UpsertAsync(document);

            // Assert
            var stored = await _repository.FindByFileNameAsync("no-error.txt");
            Assert.IsNotNull(stored);
            Assert.IsNull(stored.ErrorMessage);
        }

        [TestMethod]
        public async Task UpsertAsync_Statusがラウンドトリップする()
        {
            // Arrange
            var indexed = CreateSampleDocument(Guid.NewGuid().ToString(), "status-indexed.txt");
            indexed.Status = DocumentStatus.Indexed;
            var error = CreateSampleDocument(Guid.NewGuid().ToString(), "status-error.txt");
            error.Status = DocumentStatus.Error;
            var unsupported = CreateSampleDocument(Guid.NewGuid().ToString(), "status-unsupported.txt");
            unsupported.Status = DocumentStatus.Unsupported;

            // Act
            await _repository.UpsertAsync(indexed);
            await _repository.UpsertAsync(error);
            await _repository.UpsertAsync(unsupported);

            // Assert
            Assert.AreEqual(DocumentStatus.Indexed, (await _repository.FindByFileNameAsync("status-indexed.txt")).Status);
            Assert.AreEqual(DocumentStatus.Error, (await _repository.FindByFileNameAsync("status-error.txt")).Status);
            Assert.AreEqual(DocumentStatus.Unsupported, (await _repository.FindByFileNameAsync("status-unsupported.txt")).Status);
        }

        [TestMethod]
        public void Constructor_connectionStringがnullの場合はArgumentNullExceptionをスローする()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SqliteDocumentRepository(null));
        }

        [TestMethod]
        public async Task FindByFileNameAsync_fileNameがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _repository.FindByFileNameAsync(null));
        }

        [TestMethod]
        public async Task UpsertAsync_documentがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _repository.UpsertAsync(null));
        }

        [TestMethod]
        public async Task DeleteAsync_idがnullの場合はArgumentNullExceptionをスローする()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _repository.DeleteAsync(null));
        }
    }
}
