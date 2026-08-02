using System;
using System.Globalization;
using System.IO;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// <see cref="IQueryMetricsLogger"/> の実装クラス。<c>data/logs/query-metrics-yyyy-MM-dd.log</c>（UTC日付）に1行ずつ追記する。
    /// <c>ingestion.log</c> とは異なり質問1回ごとに複数行出力され増加ペースが速いため、日付別ファイルに分割した上で
    /// <see cref="RagSettings.RagMetricsLogRetentionDays"/> より古いファイルを自動削除する。
    /// </summary>
    public class FileQueryMetricsLogger : IQueryMetricsLogger
    {
        private const string LogFileNamePrefix = "query-metrics-";
        private const string LogFileNameExtension = ".log";
        private const string LogFileDateFormat = "yyyy-MM-dd";
        private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private const string LogLevel = "METRICS";

        private static readonly object WriteLock = new object();

        // プロセス内で1日1回だけパージ処理を実行するためのガード（UTC日付）。static のため、
        // 既定コンストラクタ（本番用）で生成したすべてのインスタンスで共有される。
        private static DateTime? _lastPurgedUtcDate;

        private readonly string _logsDir;
        private readonly bool _bypassPurgeGuard;

        /// <summary>
        /// 既定の出力先（<see cref="AppPaths.LogsDir"/>）を使って初期化する。
        /// </summary>
        /// <remarks>
        /// コンストラクタ内では <see cref="AppPaths"/> のパス解決を行わない。<see cref="AppPaths.LogsDir"/> の解決は
        /// <c>HostingEnvironment</c> に依存しており、単体テストなどホスティング環境が無い状況でコンストラクタが
        /// 例外を投げてしまうのを避けるため、解決は実際に書き込みを行う <see cref="LogMetrics"/> 呼び出し時まで遅延させる。
        /// </remarks>
        public FileQueryMetricsLogger()
        {
            _logsDir = null;
            _bypassPurgeGuard = false;
        }

        /// <summary>
        /// ログ出力先ディレクトリを明示指定して初期化する（テスト等で一時ディレクトリを使う場合を想定）。
        /// </summary>
        /// <param name="logsDir">ログファイルの出力先ディレクトリの絶対パス。</param>
        /// <remarks>
        /// このコンストラクタで生成したインスタンスは、書き込みのたびに必ずパージ処理を実行する
        /// （プロセス全体で共有される <see cref="_lastPurgedUtcDate"/> ガードの対象外とする）。本番コードでは常に
        /// パラメータなしコンストラクタが使われるためこの挙動に影響はなく、テストがプロセス内の実行順序
        /// （他のテストで既にその日のパージが実行済みかどうか）に依存しないようにするための設計判断である
        /// （出典なし・テスト容易性確保のためのプロジェクト独自の方針）。
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="logsDir"/> が null の場合。</exception>
        public FileQueryMetricsLogger(string logsDir)
        {
            if (logsDir == null)
            {
                throw new ArgumentNullException(nameof(logsDir));
            }

            _logsDir = logsDir;
            _bypassPurgeGuard = true;
        }

        /// <summary>
        /// 処理時間の内訳メッセージを <c>data/logs/query-metrics-yyyy-MM-dd.log</c>（UTC日付）に追記する。
        /// </summary>
        /// <param name="message">記録するメッセージ。</param>
        /// <remarks>
        /// 計測ログの書き込み失敗によって回答生成そのものが失敗扱いになるのを避けるため、ファイルI/O関連の例外
        /// （<see cref="IOException"/> / <see cref="UnauthorizedAccessException"/>）と、ホスティング環境外
        /// （単体テスト等）で <see cref="AppPaths"/> がパスを解決できない場合に投げる
        /// <see cref="InvalidOperationException"/> はここで捕捉し、無視する。
        /// </remarks>
        public void LogMetrics(string message)
        {
            try
            {
                var logsDir = _logsDir ?? AppPaths.LogsDir;
                var now = DateTime.UtcNow;
                var logFilePath = Path.Combine(
                    logsDir,
                    LogFileNamePrefix + now.ToString(LogFileDateFormat, CultureInfo.InvariantCulture) + LogFileNameExtension);
                var timestamp = now.ToString(TimestampFormat, CultureInfo.InvariantCulture);
                var line = string.Format(CultureInfo.InvariantCulture, "{0} [{1}] {2}", timestamp, LogLevel, message);

                lock (WriteLock)
                {
                    PurgeOldLogFilesIfNeeded(logsDir);
                    File.AppendAllText(logFilePath, line + Environment.NewLine);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// 保持日数（<see cref="RagSettings.RagMetricsLogRetentionDays"/>）より古い <c>query-metrics-*.log</c> を削除する。
        /// プロセス内で1日1回だけ実行されるよう <see cref="_lastPurgedUtcDate"/> でガードする
        /// （呼び出し元で <see cref="WriteLock"/> を保持していること）。
        /// </summary>
        /// <param name="logsDir">ログファイルの出力先ディレクトリ。</param>
        private void PurgeOldLogFilesIfNeeded(string logsDir)
        {
            var today = DateTime.UtcNow.Date;
            if (!_bypassPurgeGuard && _lastPurgedUtcDate == today)
            {
                return;
            }

            var cutoffDate = today.AddDays(-RagSettings.RagMetricsLogRetentionDays);
            var searchPattern = LogFileNamePrefix + "*" + LogFileNameExtension;

            foreach (var filePath in Directory.GetFiles(logsDir, searchPattern))
            {
                var fileDate = ExtractDate(filePath);
                if (fileDate == null || fileDate.Value >= cutoffDate)
                {
                    // 日付をパースできないファイルは対象外。保持期間内のファイルもそのまま残す。
                    continue;
                }

                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            _lastPurgedUtcDate = today;
        }

        /// <summary>
        /// ログファイル名（<c>query-metrics-yyyy-MM-dd.log</c>）から日付部分を厳密にパースする。
        /// </summary>
        /// <param name="filePath">ログファイルのパス。</param>
        /// <returns>パースできた場合は日付。書式が一致しない場合は null。</returns>
        private static DateTime? ExtractDate(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName == null || !fileName.StartsWith(LogFileNamePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var datePart = fileName.Substring(LogFileNamePrefix.Length);
            DateTime parsed;
            if (DateTime.TryParseExact(
                datePart, LogFileDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
