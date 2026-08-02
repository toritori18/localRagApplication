using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using LocalRagApplication.Services.Ollama;

namespace LocalRagApplication.Controllers
{
    /// <summary>
    /// 質問文の入力と、同一セッション内における質問・回答の会話履歴の表示を行う <c>/Ask</c> 配下のコントローラー。
    /// classic ASP.NET MVC 5ではアクション名解決時に <c>Async</c> サフィックスは自動的には取り除かれないため、
    /// POSTの <c>IndexAsync</c> には <see cref="ActionNameAttribute"/> を付与し、GETの <c>Index</c> と
    /// ルーティング上同じ "Index" アクションとして扱われるようにしている。
    /// </summary>
    public class AskController : Controller
    {
        // TempData のキー。将来的に他のアクションから Index へメッセージを引き継ぐ場合に備えたもの
        // （DocumentsController と同様のキー・引き継ぎ方針に揃えている）。
        private const string MessageTempDataKey = "Message";

        private readonly IQueryService _queryService;
        private readonly IChatHistoryStore _chatHistoryStore;

        /// <summary>
        /// 既定の実装（<see cref="QueryService"/>・<see cref="SessionChatHistoryStore"/>）を組み立てて初期化する。
        /// </summary>
        public AskController() : this(new QueryService(), new SessionChatHistoryStore())
        {
        }

        /// <summary>
        /// クエリサービス・会話履歴ストアを注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="queryService">質問応答サービス。</param>
        /// <param name="chatHistoryStore">質問・回答の会話履歴の保存先。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="queryService"/> または <paramref name="chatHistoryStore"/> が null の場合。
        /// </exception>
        public AskController(IQueryService queryService, IChatHistoryStore chatHistoryStore)
        {
            if (queryService == null)
            {
                throw new ArgumentNullException(nameof(queryService));
            }

            if (chatHistoryStore == null)
            {
                throw new ArgumentNullException(nameof(chatHistoryStore));
            }

            _queryService = queryService;
            _chatHistoryStore = chatHistoryStore;
        }

        /// <summary>
        /// 同一セッション内の会話履歴と、質問入力フォームを表示する。
        /// </summary>
        /// <returns>会話履歴・質問入力フォームを表示するビュー。</returns>
        public ActionResult Index()
        {
            var message = TempData[MessageTempDataKey] as string;
            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.Message = message;
            }

            var viewModel = new AskViewModel
            {
                History = _chatHistoryStore.GetHistory()
            };

            return View(viewModel);
        }

        /// <summary>
        /// 入力された質問に対する回答を生成し、会話履歴へ1往復分追加した上で <see cref="Index"/> へリダイレクトする。
        /// Ollamaに接続できない場合も例外を画面に伝播させず、その旨のメッセージを回答として履歴に追加する。
        /// </summary>
        /// <param name="question">ユーザーが入力した質問文。</param>
        /// <returns>
        /// 質問が空の場合は質問入力フォームを表示するビュー。それ以外の場合は <see cref="Index"/> へのリダイレクト。
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                ViewBag.Message = "質問を入力してください。";
                var viewModel = new AskViewModel
                {
                    Question = question,
                    History = _chatHistoryStore.GetHistory()
                };
                return View("Index", viewModel);
            }

            try
            {
                var result = await _queryService.AskAsync(question);
                _chatHistoryStore.Append(new ChatTurn
                {
                    Question = result.Question,
                    Answer = result.Answer,
                    AskedAtUtc = DateTime.UtcNow
                });
            }
            catch (OllamaConnectionException)
            {
                _chatHistoryStore.Append(new ChatTurn
                {
                    Question = question,
                    Answer = "Ollamaが起動しているか確認してください。",
                    AskedAtUtc = DateTime.UtcNow
                });
            }

            // ブラウザの再読み込み（F5等）によって質問がそのまま再送信されてしまうのを防ぐため、
            // PRG（Post/Redirect/Get）パターンでリダイレクトし、会話履歴はIndexのGETで表示し直す。
            return RedirectToAction("Index");
        }

        /// <summary>
        /// 同一セッション内の会話履歴をすべて削除する。
        /// </summary>
        /// <returns><see cref="Index"/> へのリダイレクト。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear()
        {
            _chatHistoryStore.Clear();
            return RedirectToAction("Index");
        }
    }
}
