using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LocalRagApplication.Infrastructure;
using LocalRagApplication.Models;
using LocalRagApplication.Services.Chunking;
using LocalRagApplication.Services.Ollama;
using LocalRagApplication.Services.TextExtraction;

namespace LocalRagApplication.Services
{
    /// <summary>
    /// <see cref="IDocumentIngestionService"/> の実装クラス。ファイルの保存、テキスト抽出、チャンク分割、
    /// 埋め込みベクトル化、索引・メタデータ登録までの取り込みパイプライン全体を行う。
    /// </summary>
    public class DocumentIngestionService : IDocumentIngestionService
    {
        // 対応する拡張子（大文字・小文字は区別しない）。
        private static readonly string[] SupportedExtensions = { ".pdf", ".md", ".txt" };

        private readonly IDocumentRepository _documentRepository;
        private readonly IVectorIndexRepository _vectorIndexRepository;
        private readonly IReadOnlyList<ITextExtractor> _textExtractors;
        private readonly ITextChunker _textChunker;
        private readonly IOllamaClient _ollamaClient;
        private readonly IIngestionLogger _logger;

        /// <summary>
        /// 既定の実装（SQLiteリポジトリ・標準の抽出器/チャンカー・<see cref="OllamaClient"/>・
        /// ファイルロガー）を組み立てて初期化する。
        /// </summary>
        public DocumentIngestionService()
            : this(
                new SqliteDocumentRepository(),
                new SqliteVectorIndexRepository(),
                new List<ITextExtractor> { new PlainTextExtractor(), new PdfTextExtractor() },
                new FixedLengthTextChunker(),
                new OllamaClient(),
                new FileIngestionLogger())
        {
        }

        /// <summary>
        /// 各依存コンポーネントを注入して初期化する（テスト等でフェイク実装を使う場合を想定）。
        /// </summary>
        /// <param name="documentRepository">ドキュメントメタデータのリポジトリ。</param>
        /// <param name="vectorIndexRepository">チャンク・埋め込みベクトルのリポジトリ。</param>
        /// <param name="textExtractors">拡張子ごとのテキスト抽出器の一覧。</param>
        /// <param name="textChunker">テキストチャンカー。</param>
        /// <param name="ollamaClient">Ollamaクライアント。</param>
        /// <param name="logger">取り込みログの記録先。</param>
        /// <exception cref="ArgumentNullException">いずれかの引数が null の場合。</exception>
        public DocumentIngestionService(
            IDocumentRepository documentRepository,
            IVectorIndexRepository vectorIndexRepository,
            IReadOnlyList<ITextExtractor> textExtractors,
            ITextChunker textChunker,
            IOllamaClient ollamaClient,
            IIngestionLogger logger)
        {
            if (documentRepository == null)
            {
                throw new ArgumentNullException(nameof(documentRepository));
            }

            if (vectorIndexRepository == null)
            {
                throw new ArgumentNullException(nameof(vectorIndexRepository));
            }

            if (textExtractors == null)
            {
                throw new ArgumentNullException(nameof(textExtractors));
            }

            if (textChunker == null)
            {
                throw new ArgumentNullException(nameof(textChunker));
            }

            if (ollamaClient == null)
            {
                throw new ArgumentNullException(nameof(ollamaClient));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _documentRepository = documentRepository;
            _vectorIndexRepository = vectorIndexRepository;
            _textExtractors = textExtractors;
            _textChunker = textChunker;
            _ollamaClient = ollamaClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IngestResult> IngestAsync(IEnumerable<HttpPostedFileBase> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var skippedFileNames = new List<string>();
            var addedCount = 0;
            var updatedCount = 0;
            var errorCount = 0;

            foreach (var file in files)
            {
                if (file == null)
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                if (!IsSupportedExtension(extension))
                {
                    skippedFileNames.Add(file.FileName);
                    continue;
                }

                var normalizedExtension = extension.ToLowerInvariant();
                var existing = await _documentRepository.FindByFileNameAsync(file.FileName).ConfigureAwait(false);
                var isNew = existing == null;
                var id = isNew ? Guid.NewGuid().ToString() : existing.Id;

                // 再取り込み時は最初にアップロードされた日時を保持する（取り込みのたびに更新すると
                // 「いつからこのファイルを管理しているか」という情報が失われてしまうため）。
                var uploadedAtUtc = isNew ? DateTime.UtcNow : existing.UploadedAtUtc;

                try
                {
                    var documentChunks = await SaveAndIndexAsync(file, id, normalizedExtension).ConfigureAwait(false);

                    var metadata = new DocumentMetadata
                    {
                        Id = id,
                        FileName = file.FileName,
                        FileType = normalizedExtension,
                        FileSizeBytes = file.ContentLength,
                        UploadedAtUtc = uploadedAtUtc,
                        Status = DocumentStatus.Indexed,
                        IndexedAtUtc = DateTime.UtcNow,
                        ChunkCount = documentChunks.Count,
                        ErrorMessage = null
                    };

                    await _documentRepository.UpsertAsync(metadata).ConfigureAwait(false);

                    if (isNew)
                    {
                        addedCount++;
                    }
                    else
                    {
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(string.Format("ファイル '{0}' の取り込みに失敗しました。", file.FileName), ex);

                    await RecordErrorAsync(id, file, normalizedExtension, uploadedAtUtc, ex).ConfigureAwait(false);
                }
            }

            return new IngestResult
            {
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                ErrorCount = errorCount,
                SkippedFileNames = skippedFileNames
            };
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var allDocuments = await _documentRepository.GetAllAsync().ConfigureAwait(false);
            var target = allDocuments.FirstOrDefault(d => d.Id == id);

            if (target != null)
            {
                DeleteFileIfExists(Path.Combine(AppPaths.SourcesDir, id + target.FileType));
            }

            DeleteFileIfExists(Path.Combine(AppPaths.ExtractedDir, id + ".txt"));

            await _vectorIndexRepository.DeleteByDocumentIdAsync(id).ConfigureAwait(false);
            await _documentRepository.DeleteAsync(id).ConfigureAwait(false);
        }

        /// <summary>
        /// ファイル本体の保存、テキスト抽出、チャンク分割、埋め込みベクトル化、索引登録を行う。
        /// </summary>
        /// <param name="file">アップロードされたファイル。</param>
        /// <param name="id">対象ドキュメントのId。</param>
        /// <param name="normalizedExtension">小文字化済みの拡張子。</param>
        /// <returns>登録したチャンクの一覧。</returns>
        /// <exception cref="InvalidOperationException">拡張子に対応するテキスト抽出器が見つからない場合。</exception>
        private async Task<IReadOnlyList<DocumentChunk>> SaveAndIndexAsync(
            HttpPostedFileBase file, string id, string normalizedExtension)
        {
            // 保存パスにアップロード元のファイル名は使わない（パストラバーサル・不正文字対策）。
            var sourcePath = Path.Combine(AppPaths.SourcesDir, id + normalizedExtension);
            using (var fileStream = File.Create(sourcePath))
            {
                await file.InputStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            var extractor = _textExtractors.FirstOrDefault(e => e.CanHandle(normalizedExtension));
            if (extractor == null)
            {
                throw new InvalidOperationException(
                    string.Format("拡張子 '{0}' に対応するテキスト抽出器が見つかりません。", normalizedExtension));
            }

            var text = await extractor.ExtractTextAsync(sourcePath).ConfigureAwait(false);

            var extractedPath = Path.Combine(AppPaths.ExtractedDir, id + ".txt");
            await Task.Run(() => File.WriteAllText(extractedPath, text, Encoding.UTF8)).ConfigureAwait(false);

            var chunkTexts = _textChunker.Split(text, RagSettings.RagChunkSize, RagSettings.RagChunkOverlap);
            var embeddings = await _ollamaClient.EmbedAsync(chunkTexts).ConfigureAwait(false);

            var documentChunks = new List<DocumentChunk>(chunkTexts.Count);
            for (var i = 0; i < chunkTexts.Count; i++)
            {
                documentChunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentId = id,
                    ChunkIndex = i,
                    Text = chunkTexts[i],
                    Embedding = embeddings[i]
                });
            }

            await _vectorIndexRepository.ReplaceChunksAsync(id, documentChunks).ConfigureAwait(false);

            return documentChunks;
        }

        /// <summary>
        /// 取り込み中に例外が発生したファイルについて、<c>Status = Error</c> のメタデータを記録する。
        /// ファイルの保存自体が失敗している場合でも、メタデータだけは記録できるよう独立して呼び出す。
        /// </summary>
        /// <param name="id">対象ドキュメントのId。</param>
        /// <param name="file">アップロードされたファイル。</param>
        /// <param name="normalizedExtension">小文字化済みの拡張子。</param>
        /// <param name="uploadedAtUtc">アップロード日時（UTC）。</param>
        /// <param name="exception">発生した例外。</param>
        private async Task RecordErrorAsync(
            string id, HttpPostedFileBase file, string normalizedExtension, DateTime uploadedAtUtc, Exception exception)
        {
            var errorMetadata = new DocumentMetadata
            {
                Id = id,
                FileName = file.FileName,
                FileType = normalizedExtension,
                FileSizeBytes = file.ContentLength,
                UploadedAtUtc = uploadedAtUtc,
                Status = DocumentStatus.Error,
                IndexedAtUtc = null,
                ChunkCount = 0,
                ErrorMessage = exception.Message
            };

            try
            {
                await _documentRepository.UpsertAsync(errorMetadata).ConfigureAwait(false);
            }
            catch (Exception upsertException)
            {
                // メタデータの記録自体に失敗しても、他ファイルの処理は継続させたいためここで握りつぶし、ログにのみ残す。
                _logger.LogError(
                    string.Format("ファイル '{0}' のエラーメタデータ記録に失敗しました。", file.FileName), upsertException);
            }
        }

        /// <summary>
        /// 指定した拡張子が取り込み対応拡張子（<c>.pdf</c>/<c>.md</c>/<c>.txt</c>）かどうかを判定する。
        /// </summary>
        /// <param name="extension">判定対象の拡張子。大文字・小文字は区別しない。</param>
        /// <returns>対応拡張子の場合は true。</returns>
        private static bool IsSupportedExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 指定したファイルが存在する場合に削除する。存在しない場合は何もしない。
        /// </summary>
        /// <param name="path">削除対象ファイルの絶対パス。</param>
        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
