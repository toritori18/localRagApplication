using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="HttpMessageHandler"/> の手書きテストダブル。実際のHTTP通信は行わず、
    /// あらかじめ与えた固定レスポンスを順番に返す。送信されたリクエストのURI・本文を記録し、
    /// テストから検証できるようにする。
    /// </summary>
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responseQueue;
        private readonly Exception _exceptionToThrow;
        private readonly List<RecordedRequest> _requests = new List<RecordedRequest>();

        /// <summary>
        /// 固定のレスポンス本文（ステータスコードは200 OK）を1回返すよう初期化する。
        /// </summary>
        /// <param name="responseBody">返すレスポンス本文（JSON文字列等）。</param>
        public FakeHttpMessageHandler(string responseBody) : this(new[] { responseBody }, HttpStatusCode.OK)
        {
        }

        /// <summary>
        /// 固定のレスポンス本文とステータスコードを1回返すよう初期化する。
        /// </summary>
        /// <param name="responseBody">返すレスポンス本文。</param>
        /// <param name="statusCode">返すHTTPステータスコード。</param>
        public FakeHttpMessageHandler(string responseBody, HttpStatusCode statusCode)
            : this(new[] { responseBody }, statusCode)
        {
        }

        /// <summary>
        /// 複数回の呼び出しに対して順番に異なるレスポンス本文（ステータスコードはすべて200 OK）を返すよう
        /// 初期化する。1回のリクエストが複数バッチに分割されるケースの検証に使う。
        /// </summary>
        /// <param name="responseBodies">呼び出し順に返すレスポンス本文の一覧。</param>
        public FakeHttpMessageHandler(IEnumerable<string> responseBodies) : this(responseBodies, HttpStatusCode.OK)
        {
        }

        /// <summary>
        /// 実通信を行わず、送信のたびに指定した例外をスローするよう初期化する
        /// （<see cref="HttpRequestException"/> や <see cref="TaskCanceledException"/> の再現に使う）。
        /// </summary>
        /// <param name="exceptionToThrow">送信時にスローする例外。</param>
        /// <exception cref="ArgumentNullException"><paramref name="exceptionToThrow"/> が null の場合。</exception>
        public FakeHttpMessageHandler(Exception exceptionToThrow)
        {
            if (exceptionToThrow == null)
            {
                throw new ArgumentNullException(nameof(exceptionToThrow));
            }

            _exceptionToThrow = exceptionToThrow;
            _responseQueue = new Queue<HttpResponseMessage>();
        }

        private FakeHttpMessageHandler(IEnumerable<string> responseBodies, HttpStatusCode statusCode)
        {
            _responseQueue = new Queue<HttpResponseMessage>(responseBodies.Select(body => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }));
        }

        /// <summary>
        /// 送信されたリクエストの一覧（送信順）。
        /// </summary>
        public IReadOnlyList<RecordedRequest> Requests
        {
            get { return _requests; }
        }

        /// <summary>
        /// リクエストを実際には送信せず、記録した上で設定済みの例外・レスポンスを返す。
        /// </summary>
        /// <param name="request">送信されるリクエスト。</param>
        /// <param name="cancellationToken">キャンセルトークン。</param>
        /// <returns>コンストラクタで設定した固定レスポンス。</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content != null
                ? await request.Content.ReadAsStringAsync().ConfigureAwait(false)
                : null;
            _requests.Add(new RecordedRequest(request.RequestUri, body));

            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            return _responseQueue.Dequeue();
        }

        /// <summary>
        /// 記録された1回分のHTTPリクエスト（送信先URIと本文）。
        /// </summary>
        public class RecordedRequest
        {
            /// <summary>
            /// <see cref="RecordedRequest"/> の新しいインスタンスを初期化する。
            /// </summary>
            /// <param name="uri">リクエストの送信先URI。</param>
            /// <param name="body">リクエスト本文の文字列。本文がない場合は null。</param>
            public RecordedRequest(Uri uri, string body)
            {
                Uri = uri;
                Body = body;
            }

            /// <summary>
            /// リクエストの送信先URI。
            /// </summary>
            public Uri Uri { get; private set; }

            /// <summary>
            /// リクエスト本文の文字列。本文がない場合は null。
            /// </summary>
            public string Body { get; private set; }
        }
    }
}
