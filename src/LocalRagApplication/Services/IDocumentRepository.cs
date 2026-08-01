using System.Collections.Generic;
using System.Threading.Tasks;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <c>data/rag.db</c>（SQLite）の Documents テーブルに対する読み書きを行うリポジトリのインターフェース。
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// 取り込み済みの全ドキュメントのメタデータを取得する。
        /// </summary>
        /// <returns>Documents テーブルの全行を表すメタデータの一覧。</returns>
        Task<IReadOnlyList<DocumentMetadata>> GetAllAsync();

        /// <summary>
        /// 指定したファイル名（アップロード時の元ファイル名）に一致するドキュメントを検索する。
        /// </summary>
        /// <param name="fileName">検索対象のファイル名。</param>
        /// <returns>一致するドキュメントのメタデータ。見つからない場合は <c>null</c>。</returns>
        Task<DocumentMetadata> FindByFileNameAsync(string fileName);

        /// <summary>
        /// ドキュメントのメタデータを挿入または更新する。<see cref="DocumentMetadata.Id"/> が既存行と一致する場合は
        /// 上書き更新し、一致しない場合は新規挿入する。
        /// </summary>
        /// <param name="document">保存するドキュメントのメタデータ。</param>
        Task UpsertAsync(DocumentMetadata document);

        /// <summary>
        /// 指定したIdのドキュメントメタデータを削除する。
        /// </summary>
        /// <param name="id">削除対象ドキュメントのId。</param>
        Task DeleteAsync(string id);
    }
}
