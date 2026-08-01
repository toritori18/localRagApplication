# 技術スタック

## 言語・ランタイム

| 技術 | 用途 | 出典 |
|---|---|---|
| C# | 主要言語 | - |
| .NET Framework 4.8（非SDK形式プロジェクト、`packages.config` によるNuGet管理） | ランタイム | [LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) の `TargetFrameworkVersion` |

## Web フレームワーク

| 技術 | 用途 | 出典 |
|---|---|---|
| ASP.NET MVC 5.2.9 | UI・サーバーサイドロジック | [LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj)（`System.Web.Mvc, Version=5.2.9.0` 参照、`packages/Microsoft.AspNet.Mvc.5.2.9`） |

## フロントエンドライブラリ

| 技術 | 用途 | 出典 |
|---|---|---|
| Bootstrap | CSS フレームワーク | `packages/` 配下（`Content/bootstrap*.css`, `Scripts/bootstrap*.js`） |
| jQuery 3.7.0 | DOM操作・非同期通信 | `Scripts/jquery-3.7.0*.js` |
| jQuery Validation | フォームのクライアントサイド検証 | `Scripts/jquery.validate*.js` |
| Modernizr 2.8.3 | ブラウザ機能検出 | `Scripts/modernizr-2.8.3.js` |

## RAG関連（埋め込み・LLM連携）

| 技術 | 用途 | 出典 |
|---|---|---|
| Ollama | AI実行環境（ローカル・無料でLLM/埋め込みモデルを実行） | [Ollama](https://ollama.com/) |
| nomic-embed-text（Ollama上のモデル） | 文章のベクトル化（埋め込み）。コンテキスト長2,000トークン | [Ollama Library: nomic-embed-text](https://ollama.com/library/nomic-embed-text) |
| llama3.1（Ollama上のモデル） | 回答生成 | [Ollama Library: llama3.1](https://ollama.com/library/llama3.1) |
| phi3（Ollama上のモデル） | 回答生成（llama3.1の代替候補、Microsoft製の軽量モデル） | [Ollama Library: phi3](https://ollama.com/library/phi3) |
| PdfPig 0.1.15 | PDFからのテキスト抽出 | [PdfPig (GitHub)](https://github.com/UglyToad/PdfPig)、`packages.config` の `PdfPig` バージョン指定 |

Ollamaとの連携は REST API を直接呼び出す形で実装している（`Services/Ollama/OllamaClient.cs`）。

- `POST /api/embed`: テキスト群の埋め込みベクトルを取得する（出典: https://docs.ollama.com/capabilities/embeddings ）
- `POST /api/generate`: プロンプトから回答テキストを生成する（出典: https://docs.ollama.com/api/generate ）

ベクトルデータベースは未使用。ファイルを事前にチャンク分割して埋め込みベクトルを計算し、SQLite（`data/rag.db`）に索引として保存する。質問時はこの索引の全チャンクを読み込み、コサイン類似度計算（`Infrastructure/VectorMath.CosineSimilarity`）により関連チャンクを検索する（Ollama自体にはベクトル検索機能がないため、類似度計算は自前実装）。

チャンク分割の既定値（`RagChunkSize=500` 文字・`RagChunkOverlap=100` 文字）および類似度検索で取得する上位チャンク数（`RagTopN=5`）は、いずれも一次資料に基づくものではない暫定値（出典なし）。`Web.config` の `appSettings` で調整可能。

## データベース

SQLite（`data/rag.db`）を使用する。ドキュメントメタデータ（`Documents`テーブル）・チャンク本文と埋め込みベクトル（`Chunks`テーブル）をすべてこのDBに保存する。スキーマ定義は [docs/sql/001_create_tables.sql](sql/001_create_tables.sql) を参照。ADO.NETプロバイダとして `System.Data.SQLite.Core` 1.0.119（`Stub.System.Data.SQLite.Core.NetFramework` 1.0.119 と併用）を使用する（出典: `packages.config` の該当パッケージバージョン指定）。

保存先の `data/` フォルダは個人データのため `.gitignore` 対象（`data/rag.db` 本体は生成されるファイルであり、`sources/`・`extracted/`・`logs/` の各フォルダは `.gitkeep` で保持）。

**既知の制約**: 類似度計算はSQLiteのネイティブ機能ではなく、アプリケーション側（`VectorMath.CosineSimilarity`）で全チャンクを対象に総当たりで計算している。ドキュメント数・チャンク数が増加すると質問1件あたりの計算量が線形に増えるため、将来的なスケーラビリティ上の既知の制約として認識しておくこと（インデックス構造を用いた近似最近傍探索等への置き換えは未検討）。

## インフラ / ホスティング

**未定・要検討。**

## 開発ツール

| 技術 | 用途 | 出典 |
|---|---|---|
| MSTest | テストフレームワーク | `packages/MSTest.TestFramework.1.2.0`, `packages/MSTest.TestAdapter.1.2.0` |

コードフォーマッタは未導入（`dotnet format` は非SDK形式プロジェクトを想定しておらず使用不可）。導入する場合は代替ツールの選定が要検討。
