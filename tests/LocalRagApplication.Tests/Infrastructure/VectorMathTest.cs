using System;
using LocalRagApplication.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Infrastructure
{
    [TestClass]
    public class VectorMathTest
    {
        private const double Tolerance = 1e-9;

        [TestMethod]
        public void CosineSimilarity_同一ベクトルの場合は1に近い値を返す()
        {
            // Arrange
            var a = new float[] { 1f, 2f, 3f };
            var b = new float[] { 1f, 2f, 3f };

            // Act
            var result = VectorMath.CosineSimilarity(a, b);

            // Assert
            Assert.AreEqual(1.0, result, Tolerance);
        }

        [TestMethod]
        public void CosineSimilarity_直交ベクトルの場合は0に近い値を返す()
        {
            // Arrange
            var a = new float[] { 1f, 0f };
            var b = new float[] { 0f, 1f };

            // Act
            var result = VectorMath.CosineSimilarity(a, b);

            // Assert
            Assert.AreEqual(0.0, result, Tolerance);
        }

        [TestMethod]
        public void CosineSimilarity_ゼロベクトルの場合は0を返す()
        {
            // Arrange
            var a = new float[] { 0f, 0f, 0f };
            var b = new float[] { 1f, 2f, 3f };

            // Act
            var result = VectorMath.CosineSimilarity(a, b);

            // Assert
            Assert.AreEqual(0.0, result, Tolerance);
        }

        [TestMethod]
        public void CosineSimilarity_次元数が異なる場合はArgumentExceptionをスローする()
        {
            // Arrange
            var a = new float[] { 1f, 2f };
            var b = new float[] { 1f, 2f, 3f };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => VectorMath.CosineSimilarity(a, b));
        }
    }
}
