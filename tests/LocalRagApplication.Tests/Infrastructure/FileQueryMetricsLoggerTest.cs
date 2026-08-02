using System;
using System.IO;
using LocalRagApplication.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Infrastructure
{
    [TestClass]
    public class FileQueryMetricsLoggerTest
    {
        private string _logsDir;

        [TestInitialize]
        public void Setup()
        {
            // テストごとに専用の一時ディレクトリを使い、他テストと状態を共有しないようにする。
            _logsDir = Path.Combine(Path.GetTempPath(), "FileQueryMetricsLoggerTest_" + Guid.NewGuid());
            Directory.CreateDirectory(_logsDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_logsDir))
                {
                    Directory.Delete(_logsDir, true);
                }
            }
            catch (IOException)
            {
                // 一時ディレクトリの削除失敗はテスト結果に影響しないため無視する。
            }
            catch (UnauthorizedAccessException)
            {
                // 同上。
            }
        }

        [TestMethod]
        public void LogMetrics_当日日付のファイル名で追記される()
        {
            // Arrange
            var logger = new FileQueryMetricsLogger(_logsDir);
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var expectedFilePath = Path.Combine(_logsDir, "query-metrics-" + today + ".log");

            // Act
            logger.LogMetrics("op=ask chunks=101 dims=768 index_load=1ms similarity=0ms total=2ms");

            // Assert
            Assert.IsTrue(File.Exists(expectedFilePath));
            var content = File.ReadAllText(expectedFilePath);
            StringAssert.Contains(content, "[METRICS]");
            StringAssert.Contains(content, "op=ask chunks=101 dims=768 index_load=1ms similarity=0ms total=2ms");
        }

        [TestMethod]
        public void LogMetrics_保持日数より古いファイルは削除され期間内のファイルは残る()
        {
            // Arrange: 保持日数（既定7日）より明らかに古い日付のファイルと、期間内の日付のファイルを事前に用意する。
            var oldFilePath = Path.Combine(_logsDir, "query-metrics-2000-01-01.log");
            File.WriteAllText(oldFilePath, "dummy");

            var recentDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
            var recentFilePath = Path.Combine(_logsDir, "query-metrics-" + recentDate + ".log");
            File.WriteAllText(recentFilePath, "dummy");

            // 日付としてパースできない不正なファイル名は削除対象外であることも併せて確認する。
            var unparsableFilePath = Path.Combine(_logsDir, "query-metrics-invalid.log");
            File.WriteAllText(unparsableFilePath, "dummy");

            var logger = new FileQueryMetricsLogger(_logsDir);

            // Act: 明示ディレクトリ指定コンストラクタは書き込みのたびに必ずパージを実行するため、
            // 他テストの実行順序に関わらずこの1回の呼び出しでパージ結果を検証できる。
            logger.LogMetrics("op=embed model=nomic-embed-text texts=1 wall=1ms total=1ms load=0ms prompt_eval=1tok");

            // Assert
            Assert.IsFalse(File.Exists(oldFilePath), "保持日数より古いファイルは削除されているはず");
            Assert.IsTrue(File.Exists(recentFilePath), "保持期間内のファイルは残っているはず");
            Assert.IsTrue(File.Exists(unparsableFilePath), "日付をパースできないファイルは削除対象外のはず");
        }

        [TestMethod]
        public void LogMetrics_出力先ディレクトリが存在しない場合でも例外を投げない()
        {
            // Arrange: 実際には存在しない（かつ自動作成もされない）ディレクトリを指定する。
            var nonExistentDir = Path.Combine(_logsDir, "not-exists", "nested");
            var logger = new FileQueryMetricsLogger(nonExistentDir);

            // Act & Assert: 例外が外に漏れないことを確認する（例外が発生すればテストが失敗する）。
            logger.LogMetrics("op=ask chunks=101 dims=768 index_load=1ms similarity=0ms total=2ms");
        }
    }
}
