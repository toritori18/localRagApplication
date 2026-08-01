using System;
using System.IO;
using System.Text;
using LocalRagApplication.Services.TextExtraction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services.TextExtraction
{
    [TestClass]
    public class PlainTextExtractorTest
    {
        private static string FixturesDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures"); }
        }

        [TestMethod]
        public void CanHandle_Txt拡張子でtrueを返す()
        {
            // Arrange
            var extractor = new PlainTextExtractor();

            // Act
            var result = extractor.CanHandle(".txt");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanHandle_Md拡張子でtrueを返す()
        {
            // Arrange
            var extractor = new PlainTextExtractor();

            // Act
            var result = extractor.CanHandle(".md");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanHandle_大文字小文字を区別しない()
        {
            // Arrange
            var extractor = new PlainTextExtractor();

            // Act
            var result = extractor.CanHandle(".TXT");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanHandle_対応外拡張子でfalseを返す()
        {
            // Arrange
            var extractor = new PlainTextExtractor();

            // Act
            var result = extractor.CanHandle(".pdf");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanHandle_null空文字でfalseを返す()
        {
            // Arrange
            var extractor = new PlainTextExtractor();

            // Act & Assert
            Assert.IsFalse(extractor.CanHandle(null));
            Assert.IsFalse(extractor.CanHandle(string.Empty));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ExtractTextAsync_txtファイルの内容をそのまま取得できる()
        {
            // Arrange
            var extractor = new PlainTextExtractor();
            var filePath = Path.Combine(FixturesDir, "sample.txt");
            var expected = File.ReadAllText(filePath, Encoding.UTF8);

            // Act
            var actual = await extractor.ExtractTextAsync(filePath);

            // Assert
            Assert.AreEqual(expected, actual);
            StringAssert.Contains(actual, "Retrieval-Augmented Generation");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ExtractTextAsync_mdファイルの内容をそのまま取得できる()
        {
            // Arrange
            var extractor = new PlainTextExtractor();
            var filePath = Path.Combine(FixturesDir, "sample.md");
            var expected = File.ReadAllText(filePath, Encoding.UTF8);

            // Act
            var actual = await extractor.ExtractTextAsync(filePath);

            // Assert
            Assert.AreEqual(expected, actual);
            StringAssert.Contains(actual, "# ローカルRAGアプリケーションについて");
        }
    }
}
