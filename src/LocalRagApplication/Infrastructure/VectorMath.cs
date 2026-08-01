using System;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// 埋め込みベクトルに対する数値計算を行う静的ヘルパー。
    /// </summary>
    public static class VectorMath
    {
        /// <summary>
        /// 2つのベクトルのコサイン類似度を計算する。<c>dot(a,b) / (|a| * |b|)</c> の標準的な定義に基づく。
        /// </summary>
        /// <param name="a">比較対象のベクトル1。</param>
        /// <param name="b">比較対象のベクトル2。</param>
        /// <returns>
        /// -1〜1の類似度スコア。<paramref name="a"/>・<paramref name="b"/> のいずれかがゼロベクトル（ノルムが0）の場合は
        /// ゼロ除算を避けるため 0 を返す。
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="a"/> と <paramref name="b"/> の長さが異なる場合。</exception>
        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentException("ベクトルの次元数が一致しません。", nameof(b));
            }

            double dot = 0;
            double normA = 0;
            double normB = 0;
            for (var i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}
