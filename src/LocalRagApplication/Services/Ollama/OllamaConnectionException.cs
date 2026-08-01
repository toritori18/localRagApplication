using System;

namespace LocalRagApplication.Services.Ollama
{
    /// <summary>
    /// Ollamaサーバーへの接続に失敗した場合（未起動・タイムアウト等）にスローされる例外。
    /// 呼び出し元は <see cref="System.Net.Http.HttpRequestException"/> 等のHTTP通信固有の型に依存せず、
    /// この例外を捕捉するだけで「Ollamaが起動しているか確認してください」という主旨のメッセージを表示できる。
    /// </summary>
    public class OllamaConnectionException : Exception
    {
        /// <summary>
        /// <see cref="OllamaConnectionException"/> の新しいインスタンスを初期化する。
        /// </summary>
        /// <param name="message">利用者向けのエラーメッセージ。</param>
        /// <param name="innerException">原因となった例外（<see cref="System.Net.Http.HttpRequestException"/> 等）。</param>
        public OllamaConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
