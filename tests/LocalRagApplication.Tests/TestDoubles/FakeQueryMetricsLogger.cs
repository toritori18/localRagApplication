using System.Collections.Generic;
using LocalRagApplication.Infrastructure;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IQueryMetricsLogger"/> の手書きテストダブル。ファイルには書き込まず、
    /// 記録されたメッセージをメモリ上に保持するのみ。
    /// </summary>
    public class FakeQueryMetricsLogger : IQueryMetricsLogger
    {
        private readonly List<string> _messages = new List<string>();

        /// <summary>
        /// 記録されたメッセージの一覧。
        /// </summary>
        public IReadOnlyList<string> Messages
        {
            get { return _messages; }
        }

        /// <inheritdoc />
        public void LogMetrics(string message)
        {
            _messages.Add(message);
        }
    }
}
