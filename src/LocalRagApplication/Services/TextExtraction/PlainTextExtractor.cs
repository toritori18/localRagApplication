using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LocalRagApplication.Services.TextExtraction
{
    /// <summary>
    /// <c>.txt</c>・<c>.md</c> ファイルをUTF-8のプレーンテキストとしてそのまま読み込む抽出器。
    /// Markdownの変換ライブラリは導入せず、記法込みのテキストをそのまま検索対象とする。
    /// </summary>
    public class PlainTextExtractor : ITextExtractor
    {
        /// <inheritdoc />
        public bool CanHandle(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return false;
            }

            return string.Equals(fileExtension, ".txt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileExtension, ".md", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public Task<string> ExtractTextAsync(string filePath)
        {
            // .NET Framework 4.8 には File.ReadAllTextAsync が存在しないため、Task.Run で非同期化する。
            return Task.Run(() => File.ReadAllText(filePath, Encoding.UTF8));
        }
    }
}
