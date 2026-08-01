using System;
using System.Collections.Generic;
using LocalRagApplication.Infrastructure;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IIngestionLogger"/> の手書きテストダブル。ファイルには書き込まず、
    /// 呼び出された内容をメモリ上に記録するのみ。
    /// </summary>
    public class FakeIngestionLogger : IIngestionLogger
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// 記録された警告メッセージの一覧。
        /// </summary>
        public IReadOnlyList<string> Warnings
        {
            get { return _warnings; }
        }

        /// <summary>
        /// 記録されたエラーメッセージの一覧。
        /// </summary>
        public IReadOnlyList<string> Errors
        {
            get { return _errors; }
        }

        /// <inheritdoc />
        public void LogWarning(string message)
        {
            _warnings.Add(message);
        }

        /// <inheritdoc />
        public void LogError(string message, Exception exception)
        {
            _errors.Add(message);
        }
    }
}
