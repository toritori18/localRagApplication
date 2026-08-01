using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalRagApplication.Services.Ollama
{
    /// <summary>
    /// Ollama REST APIを介した埋め込みベクトル化・回答生成を行うクライアントのインターフェース。
    /// </summary>
    public interface IOllamaClient
    {
        /// <summary>
        /// 複数のテキストをまとめて埋め込みベクトル化する。
        /// </summary>
        /// <param name="texts">埋め込み対象のテキスト一覧。1件のみの場合も要素数1のリストを渡す想定。</param>
        /// <returns><paramref name="texts"/> と同じ順序で並んだ埋め込みベクトルの一覧。</returns>
        /// <exception cref="OllamaConnectionException">Ollamaサーバーに接続できない、またはタイムアウトした場合。</exception>
        Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts);

        /// <summary>
        /// プロンプトから回答テキストを生成する。
        /// </summary>
        /// <param name="prompt">Ollamaに送信するプロンプト文字列。</param>
        /// <returns>生成された回答テキスト。</returns>
        /// <exception cref="OllamaConnectionException">Ollamaサーバーに接続できない、またはタイムアウトした場合。</exception>
        Task<string> GenerateAsync(string prompt);
    }
}
