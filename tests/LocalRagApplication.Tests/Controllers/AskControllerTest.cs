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
        public void Index_Get_空のAnswerResultを表示する()
        {
            // Arrange
            var queryService = new FakeQueryService((AnswerResult)null);
            var controller = new AskController(queryService);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as AnswerResult;
            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.Sources.Count);
            Assert.AreEqual(0, queryService.AskCallCount);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_質問が空の場合はサービスを呼ばずメッセージを表示する()
        {
            // Arrange
            var queryService = new FakeQueryService((AnswerResult)null);
            var controller = new AskController(queryService);

            // Act
            var result = await controller.IndexAsync(string.Empty) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, queryService.AskCallCount);
            Assert.AreEqual("質問を入力してください。", controller.ViewBag.Message);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_質問がある場合はサービスの結果をそのまま表示する()
        {
            // Arrange
            var expected = new AnswerResult
            {
                Question = "RAGとは？",
                Answer = "検索拡張生成のことです。",
                Sources = new List<SearchHit>()
            };
            var queryService = new FakeQueryService(expected);
            var controller = new AskController(queryService);

            // Act
            var result = await controller.IndexAsync("RAGとは？") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(expected, result.Model);
            Assert.AreEqual(1, queryService.AskCallCount);
            Assert.AreEqual("RAGとは？", queryService.LastQuestion);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_Ollama接続エラーの場合は案内メッセージを表示する()
        {
            // Arrange
            var exception = new OllamaConnectionException("接続失敗", null);
            var queryService = new FakeQueryService(exception);
            var controller = new AskController(queryService);

            // Act
            var result = await controller.IndexAsync("質問") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as AnswerResult;
            Assert.IsNotNull(model);
            Assert.AreEqual("Ollamaが起動しているか確認してください。", model.Answer);
        }
    }
}
