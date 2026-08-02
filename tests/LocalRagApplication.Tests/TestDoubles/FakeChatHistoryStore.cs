using System.Collections.Generic;
using LocalRagApplication.Models;
using LocalRagApplication.Services;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IChatHistoryStore"/> の手書きテストダブル。メモリ上のリストで会話履歴を保持し、
    /// <see cref="Clear"/> の呼び出し回数を検証できるようにする。
    /// </summary>
    public class FakeChatHistoryStore : IChatHistoryStore
    {
        private readonly List<ChatTurn> _history;

        /// <summary>
        /// 初期状態の会話履歴を指定して初期化する。
        /// </summary>
        /// <param name="initialHistory">初期状態として保持する会話履歴。<c>null</c> の場合は空で初期化する。</param>
        public FakeChatHistoryStore(IEnumerable<ChatTurn> initialHistory)
        {
            _history = initialHistory != null ? new List<ChatTurn>(initialHistory) : new List<ChatTurn>();
        }

        /// <summary>
        /// <see cref="Clear"/> が呼び出された回数。
        /// </summary>
        public int ClearCallCount { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ChatTurn> GetHistory()
        {
            return new List<ChatTurn>(_history);
        }

        /// <inheritdoc />
        public void Append(ChatTurn turn)
        {
            _history.Add(turn);
        }

        /// <inheritdoc />
        public void Clear()
        {
            ClearCallCount++;
            _history.Clear();
        }
    }
}
