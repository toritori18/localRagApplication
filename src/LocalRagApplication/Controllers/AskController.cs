using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using LocalRagApplication.Services.Ollama;

namespace LocalRagApplication.Controllers
{
    /// <summary>
    /// 質問文の入力と、回答・参照ソースの表示を行う <c>/Ask</c> 配下のコントローラー。
    /// classic ASP.NET MVC 5ではアクション名解決時に <c>Async</c> サフィックスは自動的には取り除かれないため、
    /// POSTの <c>IndexAsync</c> には <see cref="ActionNameAttribute"/> を付与し、GETの <c>Index</c> と
    /// ルーティング上同じ "Index" アクションとして扱われるようにしている。
    /// </summary>
    public class AskController : Controller
    {
        private readonly IQueryService _queryService;

        /// <summary>
        /// 既定の実装（<see cref="QueryService"/>）を組み立てて初期化する。
        /// </summary>
        public AskController() : this(new QueryService())
        {
        }

        /// <summary>
        /// クエリサービスを注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="queryService">質問応答サービス。</param>
        /// <exception cref="ArgumentNullException"><paramref name="queryService"/> が null の場合。</exception>
        public AskController(IQueryService queryService)
        {
            if (queryService == null)
            {
                throw new ArgumentNullException(nameof(queryService));
            }

            _queryService = queryService;
        }

        /// <summary>
        /// 質問入力フォームのみを表示する。
        /// </summary>
        /// <returns>質問入力フォームを表示するビュー。</returns>
        public ActionResult Index()
        {
            return View(new AnswerResult { Sources = new List<SearchHit>() });
        }

        /// <summary>
        /// 入力された質問に対する回答を生成し、質問入力フォームと合わせて表示する。
        /// Ollamaに接続できない場合は例外を画面に伝播させず、その旨のメッセージを回答として表示する。
        /// </summary>
        /// <param name="question">ユーザーが入力した質問文。</param>
        /// <returns>回答と参照ソースを表示するビュー。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                ViewBag.Message = "質問を入力してください。";
                return View("Index", new AnswerResult { Question = question, Sources = new List<SearchHit>() });
            }

            try
            {
                var result = await _queryService.AskAsync(question);
                return View("Index", result);
            }
            catch (OllamaConnectionException)
            {
                var result = new AnswerResult
                {
                    Question = question,
                    Answer = "Ollamaが起動しているか確認してください。",
                    Sources = new List<SearchHit>()
                };
                return View("Index", result);
            }
        }
    }
}
