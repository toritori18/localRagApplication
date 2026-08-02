using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LocalRagApplication.Controllers;
using LocalRagApplication.Models;
using LocalRagApplication.Services.Ollama;
using LocalRagApplication.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Controllers
{
    [TestClass]
    public class AskControllerTest
    {
        [TestMethod]
        public void Index_Get_履歴ストアの内容をビューモデルに詰めて返す()
        {
            // Arrange
            var history = new List<ChatTurn>
            {
                new ChatTurn { Question = "RAGとは？", Answer = "検索拡張生成のことです。", AskedAtUtc = DateTime.UtcNow }
            };
            var queryService = new FakeQueryService((AnswerResult)null);
            var chatHistoryStore = new FakeChatHistoryStore(history);
            var controller = new AskController(queryService, chatHistoryStore);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as AskViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.History.Count);
            Assert.AreEqual("RAGとは？", model.History[0].Question);
            Assert.AreEqual(0, queryService.AskCallCount);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_質問が空の場合はサービスを呼ばずメッセージを表示する()
        {
            // Arrange
            var queryService = new FakeQueryService((AnswerResult)null);
            var chatHistoryStore = new FakeChatHistoryStore(null);
            var controller = new AskController(queryService, chatHistoryStore);

            // Act
            var result = await controller.IndexAsync(string.Empty) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, queryService.AskCallCount);
            Assert.AreEqual("質問を入力してください。", controller.ViewBag.Message);
            var model = result.Model as AskViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.History.Count);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_質問が成功した場合は履歴へ追加してIndexへリダイレクトする()
        {
            // Arrange
            var expected = new AnswerResult
            {
                Question = "RAGとは？",
                Answer = "検索拡張生成のことです。",
                Sources = new List<SearchHit>()
            };
            var queryService = new FakeQueryService(expected);
            var chatHistoryStore = new FakeChatHistoryStore(null);
            var controller = new AskController(queryService, chatHistoryStore);

            // Act
            var result = await controller.IndexAsync("RAGとは？") as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual(1, queryService.AskCallCount);
            Assert.AreEqual("RAGとは？", queryService.LastQuestion);

            var history = chatHistoryStore.GetHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("RAGとは？", history[0].Question);
            Assert.AreEqual("検索拡張生成のことです。", history[0].Answer);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_Ollama接続エラーの場合も案内メッセージを持つ履歴を追加してIndexへリダイレクトする()
        {
            // Arrange
            var exception = new OllamaConnectionException("接続失敗", null);
            var queryService = new FakeQueryService(exception);
            var chatHistoryStore = new FakeChatHistoryStore(null);
            var controller = new AskController(queryService, chatHistoryStore);

            // Act
            var result = await controller.IndexAsync("質問") as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);

            var history = chatHistoryStore.GetHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("Ollamaが起動しているか確認してください。", history[0].Answer);
        }

        [TestMethod]
        public void Clear_履歴をクリアしてIndexへリダイレクトする()
        {
            // Arrange
            var initialHistory = new List<ChatTurn> { new ChatTurn { Question = "質問", Answer = "回答" } };
            var queryService = new FakeQueryService((AnswerResult)null);
            var chatHistoryStore = new FakeChatHistoryStore(initialHistory);
            var controller = new AskController(queryService, chatHistoryStore);

            // Act
            var result = controller.Clear() as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual(1, chatHistoryStore.ClearCallCount);
            Assert.AreEqual(0, chatHistoryStore.GetHistory().Count);
        }
    }
}
