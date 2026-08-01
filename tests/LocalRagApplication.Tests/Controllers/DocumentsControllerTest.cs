using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using LocalRagApplication.Controllers;
using LocalRagApplication.Models;
using LocalRagApplication.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalRagApplication.Tests.Controllers
{
    [TestClass]
    public class DocumentsControllerTest
    {
        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_ドキュメント一覧をモデルとして返す()
        {
            // Arrange
            var documents = new List<DocumentMetadata>
            {
                new DocumentMetadata { Id = "1", FileName = "sample.txt", Status = DocumentStatus.Indexed }
            };
            var documentRepository = new FakeDocumentRepository(documents);
            var ingestionService = new FakeDocumentIngestionService(null);
            var controller = new DocumentsController(ingestionService, documentRepository);

            // Act
            var result = await controller.IndexAsync() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as IReadOnlyList<DocumentMetadata>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("sample.txt", model[0].FileName);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexAsync_TempDataの取り込み結果をViewBagへ引き継ぐ()
        {
            // Arrange
            var documentRepository = new FakeDocumentRepository(new List<DocumentMetadata>());
            var ingestionService = new FakeDocumentIngestionService(null);
            var controller = new DocumentsController(ingestionService, documentRepository);
            var ingestResult = new IngestResult { AddedCount = 2 };
            controller.TempData["IngestResult"] = ingestResult;

            // Act
            var result = await controller.IndexAsync() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(ingestResult, result.ViewBag.IngestResult);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task UploadAsync_ファイル未選択の場合は取り込みを行わずIndexへリダイレクトする()
        {
            // Arrange
            var documentRepository = new FakeDocumentRepository(new List<DocumentMetadata>());
            var ingestionService = new FakeDocumentIngestionService(null);
            var controller = new DocumentsController(ingestionService, documentRepository);

            // Act
            var result = await controller.UploadAsync(new List<HttpPostedFileBase>()) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual(0, ingestionService.IngestCallCount);
            Assert.IsNotNull(controller.TempData["Message"]);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task UploadAsync_ファイルがある場合は取り込みサービスを呼びIndexへリダイレクトする()
        {
            // Arrange
            var documentRepository = new FakeDocumentRepository(new List<DocumentMetadata>());
            var ingestResult = new IngestResult { AddedCount = 1 };
            var ingestionService = new FakeDocumentIngestionService(ingestResult);
            var controller = new DocumentsController(ingestionService, documentRepository);
            var file = new FakeHttpPostedFile("sample.txt", new byte[] { 1, 2, 3 });

            // Act
            var result = await controller.UploadAsync(new List<HttpPostedFileBase> { file }) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual(1, ingestionService.IngestCallCount);
            Assert.AreSame(ingestResult, controller.TempData["IngestResult"]);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task DeleteAsync_指定Idで取り込みサービスの削除を呼びIndexへリダイレクトする()
        {
            // Arrange
            var documentRepository = new FakeDocumentRepository(new List<DocumentMetadata>());
            var ingestionService = new FakeDocumentIngestionService(null);
            var controller = new DocumentsController(ingestionService, documentRepository);

            // Act
            var result = await controller.DeleteAsync("doc-1") as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            CollectionAssert.Contains((System.Collections.ICollection)ingestionService.DeletedIds, "doc-1");
        }
    }
}
