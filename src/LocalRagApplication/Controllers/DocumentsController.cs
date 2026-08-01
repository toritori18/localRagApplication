using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LocalRagApplication.Models;
using LocalRagApplication.Services;

namespace LocalRagApplication.Controllers
{
    /// <summary>
    /// ドキュメントの取り込み（アップロード）・一覧表示・削除を行う <c>/Documents</c> 配下のコントローラー。
    /// 各アクションは <c>docs/csharp-contributing.md</c> の規約に従い <c>Async</c> サフィックス付きで命名しているが、
    /// classic ASP.NET MVC 5ではアクション名解決時に <c>Async</c> サフィックスは自動的には取り除かれないため、
    /// <see cref="ActionNameAttribute"/> を用いてルーティング上のアクション名を "Index"/"Upload"/"Delete" に明示している。
    /// </summary>
    public class DocumentsController : Controller
    {
        // TempData のキー。Upload後のリダイレクト先である Index で参照する。
        private const string IngestResultTempDataKey = "IngestResult";
        private const string MessageTempDataKey = "Message";

        private readonly IDocumentIngestionService _documentIngestionService;
        private readonly IDocumentRepository _documentRepository;

        /// <summary>
        /// 既定の実装（<see cref="DocumentIngestionService"/>・<see cref="SqliteDocumentRepository"/>）を
        /// 組み立てて初期化する。
        /// </summary>
        public DocumentsController() : this(new DocumentIngestionService(), new SqliteDocumentRepository())
        {
        }

        /// <summary>
        /// 各依存コンポーネントを注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="documentIngestionService">ドキュメント取り込みサービス。</param>
        /// <param name="documentRepository">ドキュメントメタデータのリポジトリ。</param>
        /// <exception cref="ArgumentNullException">いずれかの引数が null の場合。</exception>
        public DocumentsController(
            IDocumentIngestionService documentIngestionService, IDocumentRepository documentRepository)
        {
            if (documentIngestionService == null)
            {
                throw new ArgumentNullException(nameof(documentIngestionService));
            }

            if (documentRepository == null)
            {
                throw new ArgumentNullException(nameof(documentRepository));
            }

            _documentIngestionService = documentIngestionService;
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// 取り込み済みドキュメントの一覧を表示する。直前に <see cref="UploadAsync"/> を実行していた場合は、
        /// その取り込み結果も合わせて表示する。
        /// </summary>
        /// <returns>ドキュメント一覧を表示するビュー。</returns>
        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            // ASP.NET MVC（クラシック）にはASP.NET同期コンテキストが存在し、TempData/ViewBag等の
            // HttpContext依存メンバーへawait後もアクセスするため、ここでは既定のawait（コンテキスト復帰あり）を用いる。
            var documents = await _documentRepository.GetAllAsync();

            ViewBag.IngestResult = TempData[IngestResultTempDataKey] as IngestResult;
            ViewBag.Message = TempData[MessageTempDataKey] as string;

            return View(documents);
        }

        /// <summary>
        /// アップロードされたファイル群を取り込む。取り込み結果は <see cref="TempData"/> 経由で
        /// <see cref="IndexAsync"/> へ引き継ぐ。
        /// </summary>
        /// <param name="files">アップロードされたファイルの一覧。</param>
        /// <returns><see cref="IndexAsync"/> へのリダイレクト。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Upload")]
        public async Task<ActionResult> UploadAsync(IEnumerable<HttpPostedFileBase> files)
        {
            var hasFile = files != null && files.Any(file => file != null && file.ContentLength > 0);
            if (!hasFile)
            {
                TempData[MessageTempDataKey] = "アップロードするファイルを選択してください。";
                return RedirectToAction("Index");
            }

            var ingestResult = await _documentIngestionService.IngestAsync(files);
            TempData[IngestResultTempDataKey] = ingestResult;

            return RedirectToAction("Index");
        }

        /// <summary>
        /// 指定したドキュメントを削除する。
        /// </summary>
        /// <param name="id">削除対象ドキュメントのId。</param>
        /// <returns><see cref="IndexAsync"/> へのリダイレクト。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            await _documentIngestionService.DeleteAsync(id);

            return RedirectToAction("Index");
        }
    }
}
