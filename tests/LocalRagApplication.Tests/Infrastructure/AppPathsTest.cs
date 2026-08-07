using System;
using System.Configuration;
using System.IO;
using LocalRagApplication.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Infrastructure
{
    /// <summary>
    /// <see cref="AppPaths"/> の単体テスト。
    /// <c>ConfigurationManager.AppSettings</c> は実行時に書き換え可能（<see cref="RagSettingsTest"/> で確認済み）
    /// であることを利用し、<c>[TestInitialize]</c> で <c>RagDataRoot</c> を <c>Path.GetTempPath()</c> 配下の
    /// GUID付き専用ディレクトリへ差し替える。これにより <c>FileIngestionLoggerTest</c> や
    /// <c>DocumentIngestionServiceTest</c> が使う <c>TestData</c> ディレクトリを共有せず、実行順序に関わらず
    /// 安全に検証できる。<c>[TestCleanup]</c> で元の値へ復元し、差し替え用ディレクトリを削除する。
    /// </summary>
    [TestClass]
    public class AppPathsTest
    {
        private const string RagDataRootKey = "RagDataRoot";

        private string _originalRagDataRoot;
        private string _tempDataRoot;

        [TestInitialize]
        public void Setup()
        {
            _originalRagDataRoot = ConfigurationManager.AppSettings[RagDataRootKey];
            _tempDataRoot = Path.Combine(Path.GetTempPath(), "AppPathsTest_" + Guid.NewGuid());
            ConfigurationManager.AppSettings[RagDataRootKey] = _tempDataRoot;
        }

        [TestCleanup]
        public void Cleanup()
        {
            ConfigurationManager.AppSettings[RagDataRootKey] = _originalRagDataRoot;

            if (Directory.Exists(_tempDataRoot))
            {
                Directory.Delete(_tempDataRoot, true);
            }
        }

        [TestMethod]
        public void DataRoot_AppConfigのRagDataRoot値を返す()
        {
            // Arrange: このテストはApp.configのRagDataRoot既定値（TestData）自体を検証したいため、
            // Setup()で差し替えた値を元へ戻してから検証する。
            ConfigurationManager.AppSettings[RagDataRootKey] = _originalRagDataRoot;

            Assert.AreEqual("TestData", AppPaths.DataRoot);
        }

        [TestMethod]
        public void SourcesDir_DataRoot配下のsourcesディレクトリに解決され自動作成される()
        {
            var expectedPath = Path.Combine(AppPaths.DataRoot, "sources");

            var result = AppPaths.SourcesDir;

            Assert.AreEqual(expectedPath, result);
            Assert.IsTrue(Directory.Exists(result));
        }

        [TestMethod]
        public void ExtractedDir_DataRoot配下のextractedディレクトリに解決され自動作成される()
        {
            var expectedPath = Path.Combine(AppPaths.DataRoot, "extracted");

            var result = AppPaths.ExtractedDir;

            Assert.AreEqual(expectedPath, result);
            Assert.IsTrue(Directory.Exists(result));
        }

        [TestMethod]
        public void LogsDir_DataRoot配下のlogsディレクトリに解決され自動作成される()
        {
            var expectedPath = Path.Combine(AppPaths.DataRoot, "logs");

            var result = AppPaths.LogsDir;

            Assert.AreEqual(expectedPath, result);
            Assert.IsTrue(Directory.Exists(result));
        }

        [TestMethod]
        public void RagDbPath_DataRoot配下のragdbになる()
        {
            var expectedPath = Path.Combine(AppPaths.DataRoot, "rag.db");
            Assert.AreEqual(expectedPath, AppPaths.RagDbPath);
        }
    }
}
