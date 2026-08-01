using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalRagApplication.Models;
using LocalRagApplication.Services;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IDocumentRepository"/> の手書きテストダブル。メモリ上のリストに対して読み書きを行い、
    /// SQLiteへの実I/Oなしにコントローラー層を検証できるようにする。
    /// </summary>
    public class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<DocumentMetadata> _documents;

        /// <summary>
        /// 初期状態のドキュメント一覧を指定して初期化する。
        /// </summary>
        /// <param name="documents">初期状態として保持するドキュメントメタデータの一覧。<c>null</c> の場合は空で初期化する。</param>
        public FakeDocumentRepository(IEnumerable<DocumentMetadata> documents)
        {
            _documents = documents != null ? new List<DocumentMetadata>(documents) : new List<DocumentMetadata>();
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<DocumentMetadata>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<DocumentMetadata>>(_documents.ToList());
        }

        /// <inheritdoc />
        public Task<DocumentMetadata> FindByFileNameAsync(string fileName)
        {
            return Task.FromResult(_documents.FirstOrDefault(d => d.FileName == fileName));
        }

        /// <inheritdoc />
        public Task UpsertAsync(DocumentMetadata document)
        {
            _documents.RemoveAll(d => d.Id == document.Id);
            _documents.Add(document);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(string id)
        {
            _documents.RemoveAll(d => d.Id == id);
            return Task.CompletedTask;
        }
    }
}
