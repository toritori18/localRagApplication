using System.Collections.Generic;

namespace LocalRagApplication.Models
{
    /// <summary>
    /// 質問への回答と、回答の根拠として参照したソース一覧。
    /// </summary>
    public class AnswerResult
    {
        /// <summary>
        /// ユーザーが入力した質問文。
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// Ollamaが生成した回答文。
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// 回答の根拠として参照した検索ヒットの一覧。
        /// </summary>
        public IReadOnlyList<SearchHit> Sources { get; set; }
    }
}
