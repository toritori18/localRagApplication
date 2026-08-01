using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using LocalRagApplication.Services.Chunking;
using LocalRagApplication.Services.TextExtraction;
using LocalRagApplication.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services
{
    [TestClass]
    public class DocumentIngestionServiceTest
    {
        private static string FixturesDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures"); }
        }

        private string _dbFilePath;
        private string _connectionString;
        private SqliteDocumentRepository _documentRepository;
        private SqliteVectorIndexRepository _vectorIndexRepository;
        private FakeOllamaClient _ollamaClient;
        private FakeIngestionLogger _logger;
        private DocumentIngestionService _service;

        [TestInitialize]
        public void Setup()
        {
            // テストごとに専用の一時DBファイルを使い、他テストと状態を共有しないようにする。
            _dbFilePath = Path.GetTempFileName();
            _connectionString = "Data Source=" + _dbFilePath + ";";
            _documentRepository = new SqliteDocumentRepository(_connectionString);
            _vectorIndexRepository = new SqliteVectorIndexRepository(_connectionString);
            _ollamaClient = new FakeOllamaClient();
            _logger = new FakeIngestionLogger();

            _service = new DocumentIngestionService(
                _documentRepository,
                _vectorIndexRepository,
                new List<ITextExtractor> { new PlainTextExtractor(), new PdfTextExtractor() },
                new FixedLengthTextChunker(),
                _ollamaClient,
                _logger);
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

        [TestMethod]
        public async Task IngestAsync_新規ファイルはAddedCountが増えIndexedとして登録される()
        {
            // Arrange
            var file = FakeHttpPostedFile.FromFile("sample.txt", Path.Combine(FixturesDir, "sample.txt"));

            // Act
            var result = await _service.IngestAsync(new[] { file });

            // Assert
            Assert.AreEqual(1, result.AddedCount);
            Assert.AreEqual(0, result.UpdatedCount);
            Assert.AreEqual(0, result.ErrorCount);

            var stored = await _documentRepository.FindByFileNameAsync("sample.txt");
            Assert.IsNotNull(stored);
            Assert.AreEqual(DocumentStatus.Indexed, stored.Status);
            Assert.IsTrue(stored.ChunkCount > 0);
        }

        [TestMethod]
        public async Task IngestAsync_同名ファイルを再取り込みするとUpdatedCountが増えIdは変わらない()
        {
            // Arrange
            var firstFile = FakeHttpPostedFile.FromFile("sample.txt", Path.Combine(FixturesDir, "sample.txt"));
            var firstResult = await _service.IngestAsync(new[] { firstFile });
            var firstStored = await _documentRepository.FindByFileNameAsync("sample.txt");

            var secondFile = FakeHttpPostedFile.FromFile("sample.txt", Path.Combine(FixturesDir, "sample.txt"));

            // Act
            var secondResult = await _service.IngestAsync(new[] { secondFile });

            // Assert
            Assert.AreEqual(1, firstResult.AddedCount);
            Assert.AreEqual(0, secondResult.AddedCount);
            Assert.AreEqual(1, secondResult.UpdatedCount);

            var secondStored = await _documentRepository.FindByFileNameAsync("sample.txt");
            Assert.AreEqual(firstStored.Id, secondStored.Id);
        }

        [TestMethod]
        public async Task IngestAsync_非対応拡張子はSkippedFileNamesに入り保存も抽出も行われない()
        {
            // Arrange
            var file = FakeHttpPostedFile.FromFile(
                "invalid_unsupported.docx", Path.Combine(FixturesDir, "invalid_unsupported.docx"));

            // Act
            var result = await _service.IngestAsync(new[] { file });

            // Assert
            CollectionAssert.Contains(result.SkippedFileNames.ToList(), "invalid_unsupported.docx");
            Assert.AreEqual(0, result.AddedCount);
            Assert.AreEqual(0, result.ErrorCount);
            Assert.AreEqual(0, _ollamaClient.EmbedCallCount);

            var stored = await _documentRepository.FindByFileNameAsync("invalid_unsupported.docx");
            Assert.IsNull(stored);
        }

        [TestMethod]
        public async Task IngestAsync_壊れたファイルはErrorとして記録され他ファイルの取り込みは継続する()
        {
            // Arrange
            var brokenFile = FakeHttpPostedFile.FromFile(
                "invalid_broken.pdf", Path.Combine(FixturesDir, "invalid_broken.pdf"));
            var validFile = FakeHttpPostedFile.FromFile("sample.txt", Path.Combine(FixturesDir, "sample.txt"));

            // Act
            var result = await _service.IngestAsync(new[] { brokenFile, validFile });

            // Assert
            Assert.AreEqual(1, result.ErrorCount);
            Assert.AreEqual(1, result.AddedCount);

            var brokenStored = await _documentRepository.FindByFileNameAsync("invalid_broken.pdf");
            Assert.IsNotNull(brokenStored);
            Assert.AreEqual(DocumentStatus.Error, brokenStored.Status);
            Assert.IsNotNull(brokenStored.ErrorMessage);

            var validStored = await _documentRepository.FindByFileNameAsync("sample.txt");
            Assert.IsNotNull(validStored);
            Assert.AreEqual(DocumentStatus.Indexed, validStored.Status);
        }

        [TestMethod]
        public async Task DeleteAsync_該当ドキュメントのメタデータとチャンクが削除される()
        {
            // Arrange
            var file = FakeHttpPostedFile.FromFile("sample.md", Path.Combine(FixturesDir, "sample.md"));
            await _service.IngestAsync(new[] { file });
            var stored = await _documentRepository.FindByFileNameAsync("sample.md");
            Assert.IsNotNull(stored);

            // Act
            await _service.DeleteAsync(stored.Id);

            // Assert
            var afterDelete = await _documentRepository.FindByFileNameAsync("sample.md");
            Assert.IsNull(afterDelete);

            var allChunks = await _vectorIndexRepository.GetAllAsync();
            Assert.IsFalse(allChunks.Any(c => c.DocumentId == stored.Id));
        }
    }
}
