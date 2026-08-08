using System;
using System.IO;
using LocalRagApplication.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Infrastructure
{
    /// <summary>
    /// <see cref="FileIngestionLogger"/> の単体テスト。
    /// 出力先（<see cref="AppPaths.LogsDir"/> の <c>ingestion.log</c>）を差し替える口が無いため、
    /// <c>App.config</c> の <c>RagDataRoot</c>（<c>TestData</c>）配下の同ファイルを直接読んで検証する。
    /// <c>AppPathsTest</c> は自テスト用に <c>RagDataRoot</c> を一時ディレクトリへ差し替えるため、
    /// このディレクトリを共有しない。ディレクトリごとの削除は他テストの前提を壊すため行わず、
    /// <c>ingestion.log</c> ファイルのみ削除する。
    /// </summary>
    [TestClass]
    public class FileIngestionLoggerTest
    {
        private string _logFilePath;

        [TestInitialize]
        public void Setup()
        {
            _logFilePath = Path.Combine(AppPaths.LogsDir, "ingestion.log");
            DeleteLogFileIfExists();
        }

        [TestCleanup]
        public void Cleanup()
        {
            DeleteLogFileIfExists();
        }

        private void DeleteLogFileIfExists()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
            catch (IOException)
            {
                // 一時ファイルの削除失敗はテスト結果に影響しないため無視する。
            }
        }

        [TestMethod]
        public void LogWarning_WARNを含む行が追記される()
        {
            // Arrange
            var logger = new FileIngestionLogger();

            // Act
            logger.LogWarning("警告メッセージ");

            // Assert
            var content = File.ReadAllText(_logFilePath);
            StringAssert.Contains(content, "[WARN]");
            StringAssert.Contains(content, "警告メッセージ");
        }

        [TestMethod]
        public void LogError_ERRORを含む行と続けて例外の文字列が出力される()
        {
            // Arrange
            var logger = new FileIngestionLogger();
            var exception = new InvalidOperationException("テスト用の例外");

            // Act
            logger.LogError("エラーメッセージ", exception);

            // Assert
            var content = File.ReadAllText(_logFilePath);
            StringAssert.Contains(content, "[ERROR]");
            StringAssert.Contains(content, "エラーメッセージ");
            StringAssert.Contains(content, exception.ToString());
        }

        [TestMethod]
        public void LogWarning_複数回呼ぶと追記され上書きされない()
        {
            // Arrange
            var logger = new FileIngestionLogger();

            // Act
            logger.LogWarning("1回目の警告");
            logger.LogWarning("2回目の警告");

            // Assert
            var lines = File.ReadAllLines(_logFilePath);
            Assert.AreEqual(2, lines.Length);
            StringAssert.Contains(lines[0], "1回目の警告");
            StringAssert.Contains(lines[1], "2回目の警告");
        }
    }
}
