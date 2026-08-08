using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Web.SessionState;
using LocalRagApplication.Models;
using LocalRagApplication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Services
{
    /// <summary>
    /// <see cref="SessionChatHistoryStore"/> の単体テスト。
    /// <see cref="HttpSessionStateContainer"/> と
    /// <see cref="SessionStateUtility.AddHttpSessionStateToContext(HttpContext, IHttpSessionState)"/> の
    /// 組み合わせにより、ホスティング環境（IIS等）が無いテスト実行下でも <see cref="HttpContext.Current"/> に
    /// 実際に動作するセッション（get/set/removeが機能するセッション）を組み立てられることを確認済みのため、
    /// セッションが利用できる場合の観点（追加・上限件数・クリア・戻り値の独立性）も検証する。
    /// </summary>
    [TestClass]
    public class SessionChatHistoryStoreTest
    {
        [TestInitialize]
        public void Setup()
        {
            // 「HttpContextが無い」系のテストがテストホストの既定状態（null）に暗黙に依存しないよう、
            // 実行順序に関わらず明示的にnullへ初期化する。
            HttpContext.Current = null;
        }

        [TestCleanup]
        public void Cleanup()
        {
            // HttpContext.Current はスレッド／プロセス状態のため、次のテスト（他クラスを含む）に
            // 影響を残さないよう、セッションを組み立てたかどうかに関わらず必ずnullへ戻す。
            HttpContext.Current = null;
        }

        // ---- セッションが利用できる場合 ----

        [TestMethod]
        public void Append_履歴に追加されGetHistoryで取得できる()
        {
            CreateContextWithSession();
            var store = new SessionChatHistoryStore();
            var turn = new ChatTurn { Question = "質問1", Answer = "回答1", AskedAtUtc = DateTime.UtcNow };

            store.Append(turn);
            var history = store.GetHistory();

            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("質問1", history[0].Question);
            Assert.AreEqual("回答1", history[0].Answer);
        }

        [TestMethod]
        public void Append_上限50件を超えると古いものから削除される()
        {
            CreateContextWithSession();
            var store = new SessionChatHistoryStore();

            // MaxHistoryCount（50）を1件超える51件を追加する。
            for (var i = 0; i < 51; i++)
            {
                store.Append(new ChatTurn
                {
                    Question = "質問" + i,
                    Answer = "回答" + i,
                    AskedAtUtc = DateTime.UtcNow
                });
            }

            var history = store.GetHistory();

            Assert.AreEqual(50, history.Count);
            Assert.AreEqual("質問1", history[0].Question);
            Assert.AreEqual("質問50", history[49].Question);
        }

        [TestMethod]
        public void Clear_履歴が消える()
        {
            CreateContextWithSession();
            var store = new SessionChatHistoryStore();
            store.Append(new ChatTurn { Question = "質問1", Answer = "回答1", AskedAtUtc = DateTime.UtcNow });

            store.Clear();
            var history = store.GetHistory();

            Assert.AreEqual(0, history.Count);
        }

        [TestMethod]
        public void GetHistory_返り値を変更しても内部状態には影響しない()
        {
            CreateContextWithSession();
            var store = new SessionChatHistoryStore();
            store.Append(new ChatTurn { Question = "質問1", Answer = "回答1", AskedAtUtc = DateTime.UtcNow });

            var firstCall = (List<ChatTurn>)store.GetHistory();
            firstCall.Add(new ChatTurn { Question = "改ざん", Answer = "改ざん", AskedAtUtc = DateTime.UtcNow });
            firstCall.Clear();

            var secondCall = store.GetHistory();

            Assert.AreEqual(1, secondCall.Count);
            Assert.AreEqual("質問1", secondCall[0].Question);
        }

        // ---- セッションが利用できない場合（HttpContext.Current が null。テストホストの既定状態） ----

        [TestMethod]
        public void GetHistory_HttpContextが無い場合は空リストを返し例外を投げない()
        {
            var store = new SessionChatHistoryStore();

            var history = store.GetHistory();

            Assert.AreEqual(0, history.Count);
        }

        [TestMethod]
        public void Append_HttpContextが無い場合は例外を投げず追加を無視する()
        {
            var store = new SessionChatHistoryStore();

            store.Append(new ChatTurn { Question = "質問1", Answer = "回答1", AskedAtUtc = DateTime.UtcNow });

            Assert.AreEqual(0, store.GetHistory().Count);
        }

        [TestMethod]
        public void Clear_HttpContextが無い場合は例外を投げず無視する()
        {
            var store = new SessionChatHistoryStore();

            store.Clear();

            Assert.AreEqual(0, store.GetHistory().Count);
        }

        [TestMethod]
        public void Append_turnがnullの場合は例外を投げず無視する()
        {
            var store = new SessionChatHistoryStore();

            store.Append(null);

            Assert.AreEqual(0, store.GetHistory().Count);
        }

        /// <summary>
        /// <see cref="HttpContext.Current"/> に、実際にget/set/removeが機能するセッションを組み立てて設定する。
        /// </summary>
        private static void CreateContextWithSession()
        {
            var workerRequest = new SimpleWorkerRequest("/", string.Empty, "default.aspx", string.Empty, new StringWriter());
            var context = new HttpContext(workerRequest);
            HttpContext.Current = context;

            var sessionContainer = new HttpSessionStateContainer(
                Guid.NewGuid().ToString(),
                new SessionStateItemCollection(),
                new HttpStaticObjectsCollection(),
                20,
                true,
                HttpCookieMode.AutoDetect,
                SessionStateMode.InProc,
                false);

            SessionStateUtility.AddHttpSessionStateToContext(context, sessionContainer);
        }
    }
}
