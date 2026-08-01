using System;
using System.IO;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// <see cref="IIngestionLogger"/> の実装クラス。<c>data/logs/ingestion.log</c> に1行ずつ追記する。
    /// </summary>
    public class FileIngestionLogger : IIngestionLogger
    {
        private const string LogFileName = "ingestion.log";
        private static readonly object WriteLock = new object();

        /// <summary>
        /// 警告を <c>data/logs/ingestion.log</c> に追記する。
        /// </summary>
        /// <param name="message">警告メッセージ。</param>
        public void LogWarning(string message)
        {
            WriteLine("WARN", message, null);
        }

        /// <summary>
        /// エラーを <c>data/logs/ingestion.log</c> に追記する。
        /// </summary>
        /// <param name="message">エラーメッセージ。</param>
        /// <param name="exception">発生した例外。</param>
        public void LogError(string message, Exception exception)
        {
            WriteLine("ERROR", message, exception);
        }

        /// <summary>
        /// ログファイルへの追記を行う。複数スレッドからの同時書き込みで内容が壊れないよう <c>lock</c> で排他制御する。
        /// </summary>
        /// <param name="level">ログレベル（WARN / ERROR）。</param>
        /// <param name="message">ログメッセージ。</param>
        /// <param name="exception">発生した例外。無い場合は null。</param>
        private void WriteLine(string level, string message, Exception exception)
        {
            var logFilePath = Path.Combine(AppPaths.LogsDir, LogFileName);

            // ISO 8601形式（UTC）の日時を先頭に付与する。
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var line = string.Format("{0} [{1}] {2}", timestamp, level, message);
            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            lock (WriteLock)
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
    }
}
