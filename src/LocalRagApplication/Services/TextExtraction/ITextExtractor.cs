using System.Threading.Tasks;

namespace LocalRagApplication.Services.TextExtraction
{
    /// <summary>
    /// ファイルからプレーンテキストを抽出するテキスト抽出器のインターフェース。
    /// </summary>
    public interface ITextExtractor
    {
        /// <summary>
        /// この抽出器が指定の拡張子を処理できるかを判定する。
        /// </summary>
        /// <param name="fileExtension">判定対象の拡張子（例: <c>".pdf"</c>）。大文字・小文字は区別しない。</param>
        /// <returns>処理できる場合は true。</returns>
        bool CanHandle(string fileExtension);

        /// <summary>
        /// 指定ファイルからプレーンテキストを抽出する。
        /// </summary>
        /// <param name="filePath">抽出対象ファイルの絶対パス。</param>
        /// <returns>抽出したプレーンテキスト。</returns>
        Task<string> ExtractTextAsync(string filePath);
    }
}
