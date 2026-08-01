using System;
using System.IO;
using System.Web;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="HttpPostedFileBase"/> の手書きテストダブル。実際のHTTPアップロードを介さず、
    /// メモリ上のバイト配列またはファイルを疑似的なアップロードファイルとして扱えるようにする。
    /// </summary>
    public class FakeHttpPostedFile : HttpPostedFileBase
    {
        private readonly byte[] _content;
        private readonly string _fileName;
        private readonly string _contentType;

        /// <summary>
        /// ファイル名とバイト配列から初期化する。
        /// </summary>
        /// <param name="fileName">アップロードされたファイル名として扱う値。</param>
        /// <param name="content">ファイルの内容。<c>null</c> の場合は空配列として扱う。</param>
        /// <param name="contentType">Content-Type。既定値は <c>application/octet-stream</c>。</param>
        public FakeHttpPostedFile(string fileName, byte[] content, string contentType = "application/octet-stream")
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            _fileName = fileName;
            _content = content ?? new byte[0];
            _contentType = contentType;
        }

        /// <summary>
        /// 実ファイルを読み込み、指定したファイル名でアップロードされたものとして扱う <see cref="FakeHttpPostedFile"/> を作る。
        /// </summary>
        /// <param name="fileName">アップロードされたファイル名として扱う値。</param>
        /// <param name="sourceFilePath">読み込む実ファイルの絶対パス（<c>Fixtures/</c> 配下のサンプルファイルを想定）。</param>
        /// <returns>読み込んだ内容を保持する <see cref="FakeHttpPostedFile"/>。</returns>
        public static FakeHttpPostedFile FromFile(string fileName, string sourceFilePath)
        {
            return new FakeHttpPostedFile(fileName, File.ReadAllBytes(sourceFilePath));
        }

        /// <inheritdoc />
        public override string FileName
        {
            get { return _fileName; }
        }

        /// <inheritdoc />
        public override string ContentType
        {
            get { return _contentType; }
        }

        /// <inheritdoc />
        public override int ContentLength
        {
            get { return _content.Length; }
        }

        /// <inheritdoc />
        public override Stream InputStream
        {
            // アクセスのたびに読み取り位置0の新しいストリームを返すことで、同一インスタンスを
            // 複数回のアップロード（再取り込みのシナリオ）に使い回せるようにする。
            get { return new MemoryStream(_content); }
        }

        /// <inheritdoc />
        public override void SaveAs(string filename)
        {
            File.WriteAllBytes(filename, _content);
        }
    }
}
