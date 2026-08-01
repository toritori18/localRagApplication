using System.Threading.Tasks;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// 質問文をもとに関連チャンクを検索し、Ollamaで回答を生成するクエリサービスのインターフェース。
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// 質問に対する回答を生成する。索引が空の場合はOllamaを呼び出さず、取り込みを促すメッセージを返す。
        /// </summary>
        /// <param name="question">ユーザーが入力した質問文。</param>
        /// <returns>回答と参照ソース一覧を含む結果。</returns>
        /// <exception cref="Services.Ollama.OllamaConnectionException">
        /// Ollamaサーバーに接続できない、またはタイムアウトした場合。呼び出し元で「Ollamaが起動しているか確認してください」
        /// という主旨のメッセージ表示を行うため、ここでは捕捉せずそのまま伝播させる。
        /// </exception>
        Task<AnswerResult> AskAsync(string question);
    }
}
