using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;

namespace LocalRagApplication.Infrastructure
{
    /// <summary>
    /// <c>data/</c> 配下の各ディレクトリ・ファイルのパスを解決する静的ヘルパー。
    /// </summary>
    /// <remarks>
    /// パス解決の方針は <c>docs/development-setup.md</c> で正式には未確定であるため、
    /// ここでの「<see cref="HostingEnvironment.MapPath(string)"/> の結果から2階層上をリポジトリルートとみなし、
    /// その直下の <c>data/</c> を既定値とする」実装は暫定的なものである。デプロイ構成が変わる場合は
    /// Web.config の <c>RagDataRoot</c> で明示的に絶対パスを指定して上書きすることを想定する。
    /// </remarks>
    public static class AppPaths
    {
        private const string RagDataRootAppSettingKey = "RagDataRoot";

        /// <summary>
        /// <c>data/</c> ディレクトリのルートパス。存在しない場合は自動的に作成する。
        /// </summary>
        public static string DataRoot
        {
            get
            {
                var root = ResolveDataRoot();
                return EnsureDirectoryExists(root);
            }
        }

        /// <summary>
        /// アップロードされた元ファイルを保存する <c>data/sources/</c> ディレクトリ。存在しない場合は自動的に作成する。
        /// </summary>
        public static string SourcesDir
        {
            get { return EnsureDirectoryExists(Path.Combine(DataRoot, "sources")); }
        }

        /// <summary>
        /// 抽出済みプレーンテキストを保存する <c>data/extracted/</c> ディレクトリ。存在しない場合は自動的に作成する。
        /// </summary>
        public static string ExtractedDir
        {
            get { return EnsureDirectoryExists(Path.Combine(DataRoot, "extracted")); }
        }

        /// <summary>
        /// 処理ログを保存する <c>data/logs/</c> ディレクトリ。存在しない場合は自動的に作成する。
        /// </summary>
        public static string LogsDir
        {
            get { return EnsureDirectoryExists(Path.Combine(DataRoot, "logs")); }
        }

        /// <summary>
        /// ドキュメントメタデータ・チャンク・埋め込みベクトルを保存するSQLiteデータベースファイル（<c>data/rag.db</c>）のフルパス。
        /// </summary>
        public static string RagDbPath
        {
            get { return Path.Combine(DataRoot, "rag.db"); }
        }

        /// <summary>
        /// Web.config の <c>RagDataRoot</c> 設定、または既定のパス解決ロジックにより <c>data/</c> のルートパスを決定する。
        /// </summary>
        /// <returns><c>data/</c> ディレクトリの絶対パス。</returns>
        /// <exception cref="InvalidOperationException">
        /// <c>RagDataRoot</c> が未設定で、かつホスティング環境からアプリケーションルートを解決できない場合。
        /// </exception>
        private static string ResolveDataRoot()
        {
            var configuredRoot = ConfigurationManager.AppSettings[RagDataRootAppSettingKey];
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                return configuredRoot;
            }

            var appRoot = HostingEnvironment.MapPath("~/");
            if (string.IsNullOrEmpty(appRoot))
            {
                throw new InvalidOperationException(
                    "data/ のルートパスを解決できませんでした。Web.config の RagDataRoot に絶対パスを設定してください。");
            }

            // appRoot は src/LocalRagApplication/ を指すため、2階層上（リポジトリルート）に data/ があるとみなす。
            var repositoryRoot = Path.GetFullPath(Path.Combine(appRoot, "..", ".."));
            return Path.Combine(repositoryRoot, "data");
        }

        /// <summary>
        /// 指定したディレクトリが存在しない場合は作成する。
        /// </summary>
        /// <param name="path">ディレクトリの絶対パス。</param>
        /// <returns>引数と同じパス。</returns>
        private static string EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
