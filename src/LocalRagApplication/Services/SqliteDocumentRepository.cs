using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <see cref="IDocumentRepository"/> の実装クラス。<c>data/rag.db</c>（SQLite）の Documents テーブルに対して
    /// <c>System.Data.SQLite</c>（ADO.NETプロバイダ）を用いて読み書きを行う。
    /// </summary>
    public class SqliteDocumentRepository : IDocumentRepository
    {
        // docs/sql/001_create_tables.sql と同一内容のDDL。マイグレーションツールは導入していないため、
        // スキーマを変更する場合はこの文字列と docs/sql/ 配下のファイルを手動で同期させること。
        private const string CreateTableSql =
            "CREATE TABLE IF NOT EXISTS Documents (" +
            "Id TEXT PRIMARY KEY," +
            "FileName TEXT NOT NULL," +
            "FileType TEXT NOT NULL," +
            "FileSizeBytes INTEGER NOT NULL," +
            "UploadedAtUtc TEXT NOT NULL," +
            "Status TEXT NOT NULL," +
            "IndexedAtUtc TEXT," +
            "ChunkCount INTEGER NOT NULL DEFAULT 0," +
            "ErrorMessage TEXT" +
            ")";

        private readonly string _connectionString;

        /// <summary>
        /// <c>data/rag.db</c>（<see cref="AppPaths.RagDbPath"/>）を接続先として初期化する。
        /// </summary>
        public SqliteDocumentRepository() : this(BuildConnectionString(AppPaths.RagDbPath))
        {
        }

        /// <summary>
        /// 接続文字列を指定して初期化する（テスト等で一時ファイルを使う場合を想定）。
        /// </summary>
        /// <param name="connectionString">SQLiteの接続文字列。<c>Data Source</c> パラメータが必須。</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> が null の場合。</exception>
        public SqliteDocumentRepository(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _connectionString = connectionString;

            // コンストラクタ起動時にテーブルが無ければ自動作成する。マイグレーションツールは導入していないため、
            // 起動のたびに CREATE TABLE IF NOT EXISTS を実行することで簡易的にスキーマの存在を保証する。
            EnsureSchema();
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<DocumentMetadata>> GetAllAsync()
        {
            return Task.Run<IReadOnlyList<DocumentMetadata>>(() =>
            {
                var results = new List<DocumentMetadata>();
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT Id, FileName, FileType, FileSizeBytes, UploadedAtUtc, Status, IndexedAtUtc, ChunkCount, ErrorMessage " +
                        "FROM Documents";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadDocument(reader));
                        }
                    }
                }

                return results;
            });
        }

        /// <inheritdoc />
        public Task<DocumentMetadata> FindByFileNameAsync(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT Id, FileName, FileType, FileSizeBytes, UploadedAtUtc, Status, IndexedAtUtc, ChunkCount, ErrorMessage " +
                        "FROM Documents WHERE FileName = @fileName";
                    command.Parameters.Add(new SQLiteParameter("@fileName", fileName));
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadDocument(reader);
                        }
                    }
                }

                return null;
            });
        }

        /// <inheritdoc />
        public Task UpsertAsync(DocumentMetadata document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    // SQLiteの INSERT OR REPLACE は主キー（Id）が一致する行があれば置き換え、無ければ挿入する。
                    command.CommandText =
                        "INSERT OR REPLACE INTO Documents " +
                        "(Id, FileName, FileType, FileSizeBytes, UploadedAtUtc, Status, IndexedAtUtc, ChunkCount, ErrorMessage) " +
                        "VALUES (@id, @fileName, @fileType, @fileSizeBytes, @uploadedAtUtc, @status, @indexedAtUtc, @chunkCount, @errorMessage)";
                    command.Parameters.Add(new SQLiteParameter("@id", document.Id));
                    command.Parameters.Add(new SQLiteParameter("@fileName", document.FileName));
                    command.Parameters.Add(new SQLiteParameter("@fileType", document.FileType));
                    command.Parameters.Add(new SQLiteParameter("@fileSizeBytes", document.FileSizeBytes));
                    command.Parameters.Add(new SQLiteParameter("@uploadedAtUtc", ToIso8601(document.UploadedAtUtc)));
                    command.Parameters.Add(new SQLiteParameter("@status", document.Status.ToString()));
                    command.Parameters.Add(new SQLiteParameter(
                        "@indexedAtUtc",
                        document.IndexedAtUtc.HasValue ? (object)ToIso8601(document.IndexedAtUtc.Value) : DBNull.Value));
                    command.Parameters.Add(new SQLiteParameter("@chunkCount", document.ChunkCount));
                    command.Parameters.Add(new SQLiteParameter("@errorMessage", (object)document.ErrorMessage ?? DBNull.Value));
                    command.ExecuteNonQuery();
                }
            });
        }

        /// <inheritdoc />
        public Task DeleteAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Documents WHERE Id = @id";
                    command.Parameters.Add(new SQLiteParameter("@id", id));
                    command.ExecuteNonQuery();
                }
            });
        }

        /// <summary>
        /// Documents テーブルが存在しない場合に作成する。
        /// </summary>
        private void EnsureSchema()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = CreateTableSql;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 接続文字列から新規に <see cref="SQLiteConnection"/> を開いて返す。
        /// </summary>
        /// <returns>オープン済みの接続。</returns>
        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// <see cref="IDataRecord"/> の1行から <see cref="DocumentMetadata"/> を組み立てる。
        /// </summary>
        /// <param name="reader">読み取り位置にある <see cref="IDataRecord"/>。</param>
        /// <returns>組み立てられたメタデータ。</returns>
        private static DocumentMetadata ReadDocument(IDataRecord reader)
        {
            return new DocumentMetadata
            {
                Id = reader.GetString(0),
                FileName = reader.GetString(1),
                FileType = reader.GetString(2),
                FileSizeBytes = reader.GetInt64(3),
                UploadedAtUtc = ParseIso8601(reader.GetString(4)),
                Status = (DocumentStatus)Enum.Parse(typeof(DocumentStatus), reader.GetString(5)),
                IndexedAtUtc = reader.IsDBNull(6) ? (DateTime?)null : ParseIso8601(reader.GetString(6)),
                ChunkCount = reader.GetInt32(7),
                ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
        }

        /// <summary>
        /// 指定したデータベースファイルパスから接続文字列を組み立てる。
        /// </summary>
        /// <param name="dbFilePath">SQLiteデータベースファイルの絶対パス。</param>
        /// <returns>
        /// <c>Data Source</c> パラメータを含む接続文字列。ファイルが存在しない場合は自動作成される
        /// （<c>FailIfMissing</c> 未指定時の既定値は <c>False</c>）。
        /// </returns>
        private static string BuildConnectionString(string dbFilePath)
        {
            return "Data Source=" + dbFilePath + ";";
        }

        /// <summary>
        /// <see cref="DateTime"/> をISO 8601形式の文字列に変換する。
        /// （"O"指定子は<see cref="DateTimeKind.Utc"/>の場合末尾に"Z"を付与したISO 8601形式を返す）
        /// </summary>
        /// <param name="value">変換対象の日時。</param>
        /// <returns>ISO 8601形式の文字列。</returns>
        private static string ToIso8601(DateTime value)
        {
            return value.ToString("O", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// ISO 8601形式の文字列を <see cref="DateTime"/> に復元する。
        /// （<see cref="DateTimeStyles.RoundtripKind"/>を指定することで、"O"形式で保存した<see cref="DateTimeKind"/>を復元できる）
        /// </summary>
        /// <param name="value">ISO 8601形式の文字列。</param>
        /// <returns>復元された日時。</returns>
        private static DateTime ParseIso8601(string value)
        {
            return DateTime.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
