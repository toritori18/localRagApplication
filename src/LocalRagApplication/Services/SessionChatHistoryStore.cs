using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <see cref="IChatHistoryStore"/> の実装。<see cref="HttpContext.Current"/> のセッション
    /// （InProcセッション）に会話履歴を保持する。
    /// </summary>
    public class SessionChatHistoryStore : IChatHistoryStore
    {
        // セッションに会話履歴（List&lt;ChatTurn&gt;）を保持する際のキー。
        private const string HistorySessionKey = "Ask.ChatHistory";

        // セッションが肥大化しないよう保持する会話履歴の上限件数。超えた分は古いものから削除する。
        private const int MaxHistoryCount = 50;

        /// <inheritdoc />
        public IReadOnlyList<ChatTurn> GetHistory()
        {
            var history = GetSessionHistory();
            return history != null ? new List<ChatTurn>(history) : new List<ChatTurn>();
        }

        /// <inheritdoc />
        public void Append(ChatTurn turn)
        {
            if (turn == null)
            {
                return;
            }

            // セッションが利用できない環境（HttpContext.Currentがnull等）では、
            // 例外を投げずに追加を無視する（画面表示のみに使う履歴のため、失敗しても質問応答自体には影響させない）。
            var session = GetSession();
            if (session == null)
            {
                return;
            }

            var history = GetSessionHistory() ?? new List<ChatTurn>();
            history.Add(turn);

            while (history.Count > MaxHistoryCount)
            {
                history.RemoveAt(0);
            }

            session[HistorySessionKey] = history;
        }

        /// <inheritdoc />
        public void Clear()
        {
            var session = GetSession();
            if (session == null)
            {
                return;
            }

            session.Remove(HistorySessionKey);
        }

        /// <summary>
        /// 現在のリクエストのセッションを取得する。
        /// </summary>
        /// <returns><see cref="HttpContext.Current"/> または <see cref="HttpSessionState"/> が
        /// 利用できない場合は null。</returns>
        private static HttpSessionState GetSession()
        {
            return HttpContext.Current != null ? HttpContext.Current.Session : null;
        }

        /// <summary>
        /// セッションに保存されている会話履歴を取得する。
        /// </summary>
        /// <returns>セッションに保存されている <see cref="List{ChatTurn}"/>。
        /// セッションが利用できない、または未保存の場合は null。</returns>
        private static List<ChatTurn> GetSessionHistory()
        {
            var session = GetSession();
            return session != null ? session[HistorySessionKey] as List<ChatTurn> : null;
        }
    }
}
