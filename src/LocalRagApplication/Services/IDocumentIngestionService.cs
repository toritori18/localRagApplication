using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// アップロードされたファイルの取り込み（保存・テキスト抽出・チャンク分割・埋め込みベクトル化・索引登録）と
    /// 取り込み済みドキュメントの削除を行うサービスのインターフェース。
    /// </summary>
    public interface IDocumentIngestionService
    {
        /// <summary>
        /// アップロードされたファイル群を取り込む。対応外拡張子はスキップし、1ファイルの失敗は他ファイルの処理を
        /// 妨げない（<c>Status = Error</c> として記録したうえで処理を継続する）。
        /// </summary>
        /// <param name="files">アップロードされたファイルの一覧。</param>
        /// <returns>追加・更新・エラーの件数とスキップしたファイル名を含む取り込み結果。</returns>
        Task<IngestResult> IngestAsync(IEnumerable<HttpPostedFileBase> files);

        /// <summary>
        /// 指定したドキュメントを削除する。保存済みの元ファイル・抽出済みテキスト・索引（チャンク）・
        /// メタデータをまとめて削除する。
        /// </summary>
        /// <param name="id">削除対象ドキュメントのId。</param>
        Task DeleteAsync(string id);
    }
}
