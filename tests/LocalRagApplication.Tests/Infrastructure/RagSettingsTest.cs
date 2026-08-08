using System.Collections.Generic;
using System.Configuration;
using LocalRagApplication.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Infrastructure
{
    /// <summary>
    /// <see cref="RagSettings"/> の単体テスト。
    /// <see cref="ConfigurationManager.AppSettings"/> は実行時に書き換え可能であることを確認済みのため、
    /// [TestInitialize]/[TestCleanup] で対象キーの値を退避・復元しながら、
    /// 「未設定→既定値」「設定値あり→その値」「不正な整数値→既定値」のいずれも検証する。
    /// </summary>
    [TestClass]
    public class RagSettingsTest
    {
        private static readonly string[] TargetKeys =
        {
            "OllamaBaseUrl",
            "OllamaEmbeddingModel",
            "OllamaGenerationModel",
            "OllamaKeepAlive",
            "RagChunkSize",
            "RagChunkOverlap",
            "RagTopN",
            "RagMetricsLogRetentionDays"
        };

        private readonly Dictionary<string, string> _originalValues = new Dictionary<string, string>();

        [TestInitialize]
        public void Setup()
        {
            foreach (var key in TargetKeys)
            {
                _originalValues[key] = ConfigurationManager.AppSettings[key];
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var key in TargetKeys)
            {
                ConfigurationManager.AppSettings[key] = _originalValues[key];
            }
        }

        [TestMethod]
        public void OllamaBaseUrl_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["OllamaBaseUrl"] = null;
            Assert.AreEqual("http://127.0.0.1:11434", RagSettings.OllamaBaseUrl);
        }

        [TestMethod]
        public void OllamaBaseUrl_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["OllamaBaseUrl"] = "http://example.com:12345";
            Assert.AreEqual("http://example.com:12345", RagSettings.OllamaBaseUrl);
        }

        [TestMethod]
        public void OllamaEmbeddingModel_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["OllamaEmbeddingModel"] = null;
            Assert.AreEqual("nomic-embed-text", RagSettings.OllamaEmbeddingModel);
        }

        [TestMethod]
        public void OllamaEmbeddingModel_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["OllamaEmbeddingModel"] = "custom-embed-model";
            Assert.AreEqual("custom-embed-model", RagSettings.OllamaEmbeddingModel);
        }

        [TestMethod]
        public void OllamaGenerationModel_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["OllamaGenerationModel"] = null;
            Assert.AreEqual("llama3.1", RagSettings.OllamaGenerationModel);
        }

        [TestMethod]
        public void OllamaGenerationModel_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["OllamaGenerationModel"] = "custom-generation-model";
            Assert.AreEqual("custom-generation-model", RagSettings.OllamaGenerationModel);
        }

        [TestMethod]
        public void OllamaKeepAlive_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["OllamaKeepAlive"] = null;
            Assert.AreEqual("30m", RagSettings.OllamaKeepAlive);
        }

        [TestMethod]
        public void OllamaKeepAlive_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["OllamaKeepAlive"] = "10m";
            Assert.AreEqual("10m", RagSettings.OllamaKeepAlive);
        }

        [TestMethod]
        public void RagChunkSize_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkSize"] = null;
            Assert.AreEqual(500, RagSettings.RagChunkSize);
        }

        [TestMethod]
        public void RagChunkSize_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkSize"] = "300";
            Assert.AreEqual(300, RagSettings.RagChunkSize);
        }

        [TestMethod]
        public void RagChunkSize_不正な値の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkSize"] = "not-a-number";
            Assert.AreEqual(500, RagSettings.RagChunkSize);
        }

        [TestMethod]
        public void RagChunkOverlap_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkOverlap"] = null;
            Assert.AreEqual(100, RagSettings.RagChunkOverlap);
        }

        [TestMethod]
        public void RagChunkOverlap_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkOverlap"] = "50";
            Assert.AreEqual(50, RagSettings.RagChunkOverlap);
        }

        [TestMethod]
        public void RagChunkOverlap_不正な値の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagChunkOverlap"] = "not-a-number";
            Assert.AreEqual(100, RagSettings.RagChunkOverlap);
        }

        [TestMethod]
        public void RagTopN_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagTopN"] = null;
            Assert.AreEqual(5, RagSettings.RagTopN);
        }

        [TestMethod]
        public void RagTopN_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["RagTopN"] = "8";
            Assert.AreEqual(8, RagSettings.RagTopN);
        }

        [TestMethod]
        public void RagTopN_不正な値の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagTopN"] = "not-a-number";
            Assert.AreEqual(5, RagSettings.RagTopN);
        }

        [TestMethod]
        public void RagMetricsLogRetentionDays_未設定の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagMetricsLogRetentionDays"] = null;
            Assert.AreEqual(7, RagSettings.RagMetricsLogRetentionDays);
        }

        [TestMethod]
        public void RagMetricsLogRetentionDays_設定値がある場合はその値を返す()
        {
            ConfigurationManager.AppSettings["RagMetricsLogRetentionDays"] = "14";
            Assert.AreEqual(14, RagSettings.RagMetricsLogRetentionDays);
        }

        [TestMethod]
        public void RagMetricsLogRetentionDays_不正な値の場合は既定値を返す()
        {
            ConfigurationManager.AppSettings["RagMetricsLogRetentionDays"] = "not-a-number";
            Assert.AreEqual(7, RagSettings.RagMetricsLogRetentionDays);
        }
    }
}
