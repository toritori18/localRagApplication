using System;

namespace LocalRagApplication.Models
{
    /// <summary>
    /// 質問と回答の1往復分の会話を表すモデル。<c>/Ask</c> 画面での会話履歴の表示に使用する。
    /// </summary>
    public class ChatTurn
    {
        /// <summary>
        /// ユーザーが入力した質問文。
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// Ollamaが生成した回答文（またはOllamaに接続できない場合の案内メッセージ）。
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// この質問が行われた日時（UTC）。
        /// </summary>
        public DateTime AskedAtUtc { get; set; }
    }
}
