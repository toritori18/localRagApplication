using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Models;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <see cref="IVectorIndexRepository"/> の実装クラス。<c>data/rag.db</c>（SQLite）の Chunks テーブルに対して
    /// <c>System.Data.SQLite</c>（ADO.NETプロバイダ）を用いて読み書きを行う。
    /// </summary>
    public class SqliteVectorIndexRepository : IVectorIndexRepository
    {
        // docs/sql/001_create_tables.sql と同一内容のDDL。マイグレーションツールは導入していないため、
        // スキーマを変更する場合はこの文字列と docs/sql/ 配下のファイルを手動で同期させること。
        private const string CreateTableSql =
            "CREATE TABLE IF NOT EXISTS Chunks (" +
            "Id TEXT PRIMARY KEY," +
            "DocumentId TEXT NOT NULL REFERENCES Documents(Id)," +
            "ChunkIndex INTEGER NOT NULL," +
            "Text TEXT NOT NULL," +
            "Embedding BLOB NOT NULL" +
            ")";

        private const string CreateIndexSql =
            "CREATE INDEX IF NOT EXISTS IX_Chunks_DocumentId ON Chunks (DocumentId)";

        private const int BytesPerFloat = sizeof(float);

        private readonly string _connectionString;

        /// <summary>
        /// <c>data/rag.db</c>（<see cref="AppPaths.RagDbPath"/>）を接続先として初期化する。
        /// </summary>
        public SqliteVectorIndexRepository() : this("Data Source=" + AppPaths.RagDbPath + ";")
        {
        }

        /// <summary>
        /// 接続文字列を指定して初期化する（テスト等で一時ファイルを使う場合を想定）。
        /// </summary>
        /// <param name="connectionString">SQLiteの接続文字列。<c>Data Source</c> パラメータが必須。</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> が null の場合。</exception>
        public SqliteVectorIndexRepository(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _connectionString = connectionString;

            // コンストラクタ起動時にテーブル・索引が無ければ自動作成する。マイグレーションツールは導入していないため、
            // 起動のたびに CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT EXISTS を実行することで簡易的に保証する。
            EnsureSchema();
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<DocumentChunk>> GetAllAsync()
        {
            return Task.Run<IReadOnlyList<DocumentChunk>>(() =>
            {
                var results = new List<DocumentChunk>();
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, DocumentId, ChunkIndex, Text, Embedding FROM Chunks";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadChunk(reader));
                        }
                    }
                }

                return results;
            });
        }

        /// <inheritdoc />
        public Task ReplaceChunksAsync(string documentId, IReadOnlyList<DocumentChunk> chunks)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                using (var transaction = connection.BeginTransaction())
                {
                    using (var deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.Transaction = transaction;
                        deleteCommand.CommandText = "DELETE FROM Chunks WHERE DocumentId = @documentId";
                        deleteCommand.Parameters.Add(new SQLiteParameter("@documentId", documentId));
                        deleteCommand.ExecuteNonQuery();
                    }

                    foreach (var chunk in chunks)
                    {
                        using (var insertCommand = connection.CreateCommand())
                        {
                            insertCommand.Transaction = transaction;
                            insertCommand.CommandText =
                                "INSERT INTO Chunks (Id, DocumentId, ChunkIndex, Text, Embedding) " +
                                "VALUES (@id, @documentId, @chunkIndex, @text, @embedding)";
                            insertCommand.Parameters.Add(new SQLiteParameter("@id", chunk.Id));
                            insertCommand.Parameters.Add(new SQLiteParameter("@documentId", chunk.DocumentId));
                            insertCommand.Parameters.Add(new SQLiteParameter("@chunkIndex", chunk.ChunkIndex));
                            insertCommand.Parameters.Add(new SQLiteParameter("@text", chunk.Text));
                            insertCommand.Parameters.Add(new SQLiteParameter("@embedding", ToBytes(chunk.Embedding)));
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            });
        }

        /// <inheritdoc />
        public Task DeleteByDocumentIdAsync(string documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Chunks WHERE DocumentId = @documentId";
                    command.Parameters.Add(new SQLiteParameter("@documentId", documentId));
                    command.ExecuteNonQuery();
                }
            });
        }

        /// <summary>
        /// Chunks テーブル・関連索引が存在しない場合に作成する。
        /// </summary>
        private void EnsureSchema()
        {
            using (var connection = OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = CreateTableSql;
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = CreateIndexSql;
                    command.ExecuteNonQuery();
                }
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
        /// <see cref="IDataRecord"/> の1行から <see cref="DocumentChunk"/> を組み立てる。
        /// </summary>
        /// <param name="reader">読み取り位置にある <see cref="IDataRecord"/>。</param>
        /// <returns>組み立てられたチャンク。</returns>
        private static DocumentChunk ReadChunk(IDataRecord reader)
        {
            return new DocumentChunk
            {
                Id = reader.GetString(0),
                DocumentId = reader.GetString(1),
                ChunkIndex = reader.GetInt32(2),
                Text = reader.GetString(3),
                Embedding = ToFloatArray((byte[])reader.GetValue(4))
            };
        }

        /// <summary>
        /// 埋め込みベクトル（<see cref="float"/>配列）をSQLiteのBLOBに保存するための<see cref="byte"/>配列に変換する。
        /// SQLiteに配列専用の型が無いため、メモリ表現をそのままバイト列としてコピーする。
        /// </summary>
        /// <param name="embedding">変換対象の埋め込みベクトル。</param>
        /// <returns>変換された<see cref="byte"/>配列。</returns>
        private static byte[] ToBytes(float[] embedding)
        {
            var bytes = new byte[embedding.Length * BytesPerFloat];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// SQLiteのBLOBから読み出した<see cref="byte"/>配列を埋め込みベクトル（<see cref="float"/>配列）に復元する。
        /// </summary>
        /// <param name="bytes">BLOBから読み出したバイト列。</param>
        /// <returns>復元された埋め込みベクトル。</returns>
        private static float[] ToFloatArray(byte[] bytes)
        {
            var floats = new float[bytes.Length / BytesPerFloat];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }
    }
}
