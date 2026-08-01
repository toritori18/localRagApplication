-- data/rag.db（SQLite）の初期スキーマ。
-- マイグレーションツールは導入していないため、このファイルと
-- src/LocalRagApplication/Services/SqliteDocumentRepository.cs・SqliteVectorIndexRepository.cs
-- 内の CREATE TABLE IF NOT EXISTS 文は手動で同期させること（スキーマ変更時は両方を更新する）。

CREATE TABLE IF NOT EXISTS Documents (
    Id TEXT PRIMARY KEY,
    FileName TEXT NOT NULL,
    FileType TEXT NOT NULL,
    FileSizeBytes INTEGER NOT NULL,
    UploadedAtUtc TEXT NOT NULL,
    Status TEXT NOT NULL,
    IndexedAtUtc TEXT,
    ChunkCount INTEGER NOT NULL DEFAULT 0,
    ErrorMessage TEXT
);

CREATE TABLE IF NOT EXISTS Chunks (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL REFERENCES Documents(Id),
    ChunkIndex INTEGER NOT NULL,
    Text TEXT NOT NULL,
    Embedding BLOB NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_Chunks_DocumentId ON Chunks (DocumentId);
