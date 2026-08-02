using System.Collections.Generic;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// 質問・回答の会話履歴（<see cref="ChatTurn"/>）の保存先を抽象化するインターフェース。
    /// 実装（<see cref="SessionChatHistoryStore"/>）は <see cref="System.Web.HttpContext"/> の
    /// セッションに依存するが、この抽象を介することで <c>AskController</c> のユニットテストが
    /// <c>HttpContext</c> なしに（手書きフェイクを注入して）実行できるようにしている。
    /// </summary>
    public interface IChatHistoryStore
    {
        /// <summary>
        /// 保存されている会話履歴を、質問された順（古い順）で取得する。
        /// </summary>
        /// <returns>会話履歴の一覧。保存されている履歴がない場合は要素数0の一覧。</returns>
        IReadOnlyList<ChatTurn> GetHistory();

        /// <summary>
        /// 会話履歴の末尾に1往復分（質問と回答）を追加する。
        /// </summary>
        /// <param name="turn">追加する質問・回答。</param>
        void Append(ChatTurn turn);

        /// <summary>
        /// 保存されている会話履歴をすべて削除する。
        /// </summary>
        void Clear();
    }
}
