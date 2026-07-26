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

ベクトルデータベースは未使用。ファイルを事前にチャンク分割して埋め込みベクトルを計算し、`data/index.json` に索引として保存する。質問時はこの索引を読み込み、コサイン類似度計算により関連チャンクを検索する（Ollama自体にはベクトル検索機能がないため、類似度計算は自前実装）。

## データベース

JSONファイルでデータを保存する（RDBMS等は使用しない）。保存先は `data/` フォルダ（個人データのため `.gitignore` 対象、フォルダ自体は `.gitkeep` で保持）。索引データの詳細スキーマ（チャンク単位・メタデータの持ち方等）は未定・要検討。

## インフラ / ホスティング

**未定・要検討。**

## 開発ツール

| 技術 | 用途 | 出典 |
|---|---|---|
| MSTest | テストフレームワーク | `packages/MSTest.TestFramework.1.2.0`, `packages/MSTest.TestAdapter.1.2.0` |

コードフォーマッタは未導入（`dotnet format` は非SDK形式プロジェクトを想定しておらず使用不可）。導入する場合は代替ツールの選定が要検討。
