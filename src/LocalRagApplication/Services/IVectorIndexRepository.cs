using System.Collections.Generic;
using System.Threading.Tasks;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <c>data/rag.db</c>（SQLite）の Chunks テーブル（チャンク本文＋埋め込みベクトル）に対する
    /// 読み書きを行うリポジトリのインターフェース。
    /// </summary>
    public interface IVectorIndexRepository
    {
        /// <summary>
        /// 全ドキュメントの全チャンクを取得する。クエリパイプラインでの全件総当たりの類似度計算に使用する。
        /// </summary>
        /// <returns>Chunks テーブルの全行を表すチャンクの一覧。</returns>
        Task<IReadOnlyList<DocumentChunk>> GetAllAsync();

        /// <summary>
        /// 指定したドキュメントの既存チャンクをすべて削除したうえで、新しいチャンク一覧を挿入する。
        /// 1トランザクション内で行うため、途中で失敗した場合は削除・挿入とも行われない。
        /// </summary>
        /// <param name="documentId">対象ドキュメントのId。</param>
        /// <param name="chunks">置き換え後のチャンク一覧。</param>
        Task ReplaceChunksAsync(string documentId, IReadOnlyList<DocumentChunk> chunks);

        /// <summary>
        /// 指定したドキュメントに属するチャンクをすべて削除する。
        /// </summary>
        /// <param name="documentId">対象ドキュメントのId。</param>
        Task DeleteByDocumentIdAsync(string documentId);
    }
}
