using System;
using LocalRagApplication.Services.Chunking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services.Chunking
{
    [TestClass]
    public class FixedLengthTextChunkerTest
    {
        [TestMethod]
        public void Split_nullの場合は空リストを返す()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();

            // Act
            var result = chunker.Split(null, 10, 2);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Split_空文字列の場合は空リストを返す()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();

            // Act
            var result = chunker.Split(string.Empty, 10, 2);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Split_チャンクサイズ未満のテキストは1件になる()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();
            var text = "12345";

            // Act
            var result = chunker.Split(text, 10, 2);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(text, result[0]);
        }

        [TestMethod]
        public void Split_ちょうど割り切れる長さでオーバーラップなしの場合は境界通りに分割される()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();
            var text = "01234567890123456789"; // 20文字

            // Act
            var result = chunker.Split(text, 10, 0);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("0123456789", result[0]);
            Assert.AreEqual("0123456789", result[1]);
        }

        [TestMethod]
        public void Split_オーバーラップを指定すると隣接チャンクの一部が重複する()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();
            // 25文字のテキスト。chunkSize=10, overlap=3 のため step=7 となり、
            // 開始位置は 0, 7, 14, 21 の4チャンクに分割される想定。
            var text = "0123456789012345678901234";

            // Act
            var result = chunker.Split(text, 10, 3);

            // Assert
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(text.Substring(0, 10), result[0]);
            Assert.AreEqual(text.Substring(7, 10), result[1]);
            Assert.AreEqual(text.Substring(14, 10), result[2]);
            Assert.AreEqual(text.Substring(21, 4), result[3]);

            // 隣接チャンク間で overlap 文字分が一致することを確認する。
            var chunk0Tail = result[0].Substring(result[0].Length - 3);
            var chunk1Head = result[1].Substring(0, 3);
            Assert.AreEqual(chunk0Tail, chunk1Head);
        }

        [TestMethod]
        public void Split_chunkSizeが0以下の場合はArgumentExceptionをスローする()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => chunker.Split("text", 0, 0));
            Assert.ThrowsException<ArgumentException>(() => chunker.Split("text", -1, 0));
        }

        [TestMethod]
        public void Split_chunkOverlapがchunkSize以上の場合はArgumentExceptionをスローする()
        {
            // Arrange
            var chunker = new FixedLengthTextChunker();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => chunker.Split("text", 5, 5));
            Assert.ThrowsException<ArgumentException>(() => chunker.Split("text", 5, 6));
        }
    }
}
