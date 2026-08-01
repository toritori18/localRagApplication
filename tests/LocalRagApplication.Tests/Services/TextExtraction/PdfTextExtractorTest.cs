using System;
using System.IO;
using System.Threading.Tasks;
using LocalRagApplication.Services.TextExtraction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services.TextExtraction
{
    [TestClass]
    public class PdfTextExtractorTest
    {
        private static string FixturesDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures"); }
        }

        [TestMethod]
        public void CanHandle_Pdf拡張子でtrueを返す()
        {
            // Arrange
            var extractor = new PdfTextExtractor();

            // Act
            var result = extractor.CanHandle(".pdf");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanHandle_大文字小文字を区別しない()
        {
            // Arrange
            var extractor = new PdfTextExtractor();

            // Act
            var result = extractor.CanHandle(".PDF");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanHandle_対応外拡張子でfalseを返す()
        {
            // Arrange
            var extractor = new PdfTextExtractor();

            // Act
            var result = extractor.CanHandle(".txt");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanHandle_null空文字でfalseを返す()
        {
            // Arrange
            var extractor = new PdfTextExtractor();

            // Act & Assert
            Assert.IsFalse(extractor.CanHandle(null));
            Assert.IsFalse(extractor.CanHandle(string.Empty));
        }

        [TestMethod]
        public async Task ExtractTextAsync_2ページ分のテキストをページ順に抽出できる()
        {
            // Arrange
            // sample.pdf は2ページ構成で、各ページに1行ずつテキストが配置されている
            // （tests/LocalRagApplication.Tests/Fixtures/sample.pdf の生テキストストリームで確認済み）。
            var extractor = new PdfTextExtractor();
            var filePath = Path.Combine(FixturesDir, "sample.pdf");
            var expected =
                "This is page 1 of the sample PDF for RAG ingestion tests." + Environment.NewLine +
                "This is page 2 of the sample PDF for RAG ingestion tests.";

            // Act
            var actual = await extractor.ExtractTextAsync(filePath);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ExtractTextAsync_壊れたPdfの場合は例外がスローされる()
        {
            // Arrange
            var extractor = new PdfTextExtractor();
            var filePath = Path.Combine(FixturesDir, "invalid_broken.pdf");

            // Act & Assert
            var threw = false;
            try
            {
                await extractor.ExtractTextAsync(filePath);
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "壊れたPDFの読み込み時に例外がスローされるはずです。");
        }
    }
}
