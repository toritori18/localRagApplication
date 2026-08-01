using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using LocalRagApplication.Models;
using LocalRagApplication.Services;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IDocumentIngestionService"/> の手書きテストダブル。<see cref="DocumentsControllerTest"/> 等、
    /// コントローラー層の検証で実際の取り込み処理（ファイルI/O・Ollama通信）を行わずに済ませるために使う。
    /// </summary>
    public class FakeDocumentIngestionService : IDocumentIngestionService
    {
        private readonly IngestResult _ingestResult;

        /// <summary>
        /// <see cref="IngestAsync"/> が返す固定の結果を指定して初期化する。
        /// </summary>
        /// <param name="ingestResult"><see cref="IngestAsync"/> の戻り値として返す取り込み結果。</param>
        public FakeDocumentIngestionService(IngestResult ingestResult)
        {
            _ingestResult = ingestResult;
        }

        /// <summary>
        /// <see cref="IngestAsync"/> が呼び出された回数。
        /// </summary>
        public int IngestCallCount { get; private set; }

        /// <summary>
        /// 直近の <see cref="IngestAsync"/> 呼び出しで渡されたファイル一覧。
        /// </summary>
        public IEnumerable<HttpPostedFileBase> LastFiles { get; private set; }

        /// <summary>
        /// <see cref="DeleteAsync"/> に渡されたIdの一覧。
        /// </summary>
        public IList<string> DeletedIds { get; } = new List<string>();

        /// <inheritdoc />
        public Task<IngestResult> IngestAsync(IEnumerable<HttpPostedFileBase> files)
        {
            IngestCallCount++;
            LastFiles = files;
            return Task.FromResult(_ingestResult);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string id)
        {
            DeletedIds.Add(id);
            return Task.CompletedTask;
        }
    }
}
