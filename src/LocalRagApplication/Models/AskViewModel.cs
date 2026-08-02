using System.Collections.Generic;

namespace LocalRagApplication.Models
{
    /// <summary>
    /// <c>/Ask</c> 画面（会話履歴・質問入力フォーム）の表示に使うビューモデル。
    /// </summary>
    public class AskViewModel
    {
        /// <summary>
        /// 同一セッション内で行われた質問・回答の会話履歴（古い順）。
        /// </summary>
        public IReadOnlyList<ChatTurn> History { get; set; }

        /// <summary>
        /// 質問入力欄（textarea）に表示する値。質問が未入力だった場合など、入力エラー時に
        /// ユーザーの入力内容を画面へ差し戻すために使用する（通常は空文字列またはnull）。
        /// </summary>
        public string Question { get; set; }
    }
}
