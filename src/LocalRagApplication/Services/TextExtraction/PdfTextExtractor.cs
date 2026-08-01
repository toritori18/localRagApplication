using System;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LocalRagApplication.Services.TextExtraction
{
    /// <summary>
    /// <c>.pdf</c> ファイルからPdfPigを用いてプレーンテキストを抽出する抽出器。
    /// </summary>
    public class PdfTextExtractor : ITextExtractor
    {
        /// <inheritdoc />
        public bool CanHandle(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return false;
            }

            return string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// PDFの各ページを <see cref="ContentOrderTextExtractor"/> でページ順に抽出し、改行区切りで連結する。
        /// </summary>
        /// <param name="filePath">PDFファイルの絶対パス。</param>
        /// <returns>ページ順に連結したプレーンテキスト。</returns>
        /// <exception cref="Exception">
        /// 不正なPDFなど、PdfPigがドキュメントを開けない場合に <see cref="PdfDocument.Open(string, ParsingOptions)"/> がスローする例外。
        /// 呼び出し元（取り込みパイプライン）で <c>Status = Error</c> として扱う設計のため、ここでは捕捉せずそのまま伝播させる。
        /// </exception>
        public Task<string> ExtractTextAsync(string filePath)
        {
            // PDF解析はCPUバウンドな処理のため、Task.Runでスレッドプールに逃がして非同期化する。
            return Task.Run(() =>
            {
                var builder = new StringBuilder();

                using (var document = PdfDocument.Open(filePath))
                {
                    var isFirstPage = true;
                    foreach (var page in document.GetPages())
                    {
                        if (!isFirstPage)
                        {
                            builder.AppendLine();
                        }

                        isFirstPage = false;

                        // page.Text は公式READMEで直接使用を推奨されていないため、
                        // ContentOrderTextExtractor.GetText を使用する。
                        builder.Append(ContentOrderTextExtractor.GetText(page));
                    }
                }

                return builder.ToString();
            });
        }
    }
}
