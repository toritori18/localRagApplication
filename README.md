# LocalRagApplication

ローカル環境で動作する RAG（Retrieval-Augmented Generation）質問応答アプリケーション。C# / ASP.NET MVC 5（.NET Framework 4.8）で実装する。

## 機能

- ブラウザから `.pdf` / `.md` / `.txt` ファイルをアップロードして取り込める（`/Documents`）
- 取り込んだファイルはチャンク分割・埋め込みベクトル化した上で SQLite（`data/rag.db`）に索引として保存される
- 質問すると、索引から類似度の高いチャンクを検索し、それを文脈として Ollama が日本語で回答を生成する（`/Ask`）

### 前提条件（Ollama）

事前に [Ollama](https://ollama.com/) をインストールし、以下のモデルを取得しておくこと。

```bash
ollama pull nomic-embed-text
ollama pull llama3.1
```

### 使い方

1. `/Documents` を開き、ファイルをアップロードする（取り込み状況・エラーはこの画面に表示される）
2. `/Ask` を開き、質問を入力する。回答が生成されると、質問・回答は会話履歴としてチャット形式で画面に残る（参照元チャンクの一覧は画面には表示されない）。同じセッション内であれば履歴は残り続け、「履歴をクリア」ボタンで削除できる

## ディレクトリ構成

```
LocalRagApplication/
├── CLAUDE.md                          # Claude Code 向けガイド
├── README.md                          # このファイル（プロジェクト説明）
├── LocalRagApplication.slnx           # ソリューションファイル
├── .gitignore
├── .editorconfig
├── .claude/
│   ├── settings.json                  # 権限・フック設定
│   ├── factcheck.md                   # ハルシネーション防止ルール
│   ├── hooks/                         # Git hooks・Claude Code フックスクリプト
│   ├── agents/                        # サブエージェント定義
│   └── commands/                      # カスタムスラッシュコマンド
│       ├── git/                       # Git関連（init/branch/push/pr等）
│       ├── server/                    # サーバー関連
│       └── db/                        # DB関連
├── src/
│   └── LocalRagApplication/           # ASP.NET MVC 5（.NET Framework 4.8, packages.config）
│       ├── App_Start/                 # 起動時設定（Bundle/Filter/Route）
│       ├── Controllers/               # MVCコントローラー
│       │   ├── HomeController.cs
│       │   ├── DocumentsController.cs # ファイル取り込み（一覧・アップロード・削除）
│       │   └── AskController.cs       # 質問応答
│       ├── Views/                     # Razorビュー（.cshtml）
│       │   ├── Home/
│       │   ├── Documents/             # /Documents 画面
│       │   ├── Ask/                   # /Ask 画面（会話履歴をチャット形式で表示）
│       │   └── Shared/                # _Layout.cshtml, _BackToHome.cshtml（各画面共通の「戻る」ボタン部分ビュー）等
│       ├── Models/                    # モデル（DocumentMetadata, DocumentChunk, AnswerResult, SearchHit, ChatTurn, AskViewModel 等）
│       ├── Services/                  # アプリケーションサービス
│       │   ├── TextExtraction/        # PDF/テキストからのテキスト抽出（PdfTextExtractor, PlainTextExtractor）
│       │   ├── Chunking/              # テキストのチャンク分割（FixedLengthTextChunker）
│       │   ├── Ollama/                # Ollama REST APIクライアント（OllamaClient）
│       │   ├── DocumentIngestionService.cs   # 取り込みパイプライン（抽出→分割→埋め込み→保存）
│       │   ├── QueryService.cs        # 質問応答パイプライン（類似検索→回答生成）
│       │   ├── IChatHistoryStore.cs / SessionChatHistoryStore.cs  # /Ask 画面の会話履歴（セッション保持）の抽象化と実装
│       │   ├── SqliteDocumentRepository.cs
│       │   └── SqliteVectorIndexRepository.cs
│       ├── Infrastructure/            # 横断的な基盤コード
│       │   ├── AppPaths.cs            # data/ 配下の各パス解決
│       │   ├── RagSettings.cs         # Web.config設定値の読み取り
│       │   ├── VectorMath.cs          # コサイン類似度計算
│       │   ├── FileIngestionLogger.cs / IIngestionLogger.cs
│       ├── Content/                   # CSS（Bootstrap同梱）
│       ├── Scripts/                   # JS（jQuery, Bootstrap, Modernizr同梱）
│       ├── Global.asax / Global.asax.cs
│       ├── Web.config                 # 構成（Web.Debug.config / Web.Release.config で環境別上書き）
│       └── packages.config            # NuGet依存関係（classicパッケージ管理）
├── tests/
│   └── LocalRagApplication.Tests/     # MSTest テストプロジェクト（.NET Framework 4.8, packages.config）
│       ├── Controllers/               # コントローラーのテスト
│       ├── Services/                  # サービス層のテスト（TextExtraction/ Chunking/ を含む）
│       ├── Infrastructure/            # 基盤コードのテスト（VectorMath 等）
│       ├── TestDoubles/               # 手書きのフェイク実装（FakeOllamaClient 等。モックライブラリ未導入のため）
│       └── Fixtures/                  # テスト用サンプルファイル（sample.*・invalid_* は異常系検証用）
├── sample-documents/                  # 動作検証用のサンプル文書（架空の内容。/Documents からアップロードして試せる）
├── packages/                          # NuGet復元先（packages.config方式、.gitignore対象）
├── data/                               # アップロードされた元ファイル・索引データ（.gitignore 対象、フォルダのみ保持）
│   ├── sources/                       # アップロードされた元ファイルの保存先
│   ├── extracted/                     # テキスト抽出後の中間ファイル（.txt）
│   ├── logs/                          # 取り込み処理のログ
│   └── rag.db                        # ドキュメントメタデータ・チャンク・埋め込みベクトルを保存するSQLiteデータベース
├── docs/                              # ドキュメント
│   ├── git-rules.md                   # Git運用ルール
│   ├── tech-stack.md                  # 技術スタック
│   ├── development-setup.md           # 開発環境セットアップガイド
│   ├── csharp-contributing.md         # コントリビュートガイド（C#）
│   └── sql/                           # SQLファイル（マイグレーション・初期データ等）
│       └── 001_create_tables.sql      # rag.db の初期スキーマ（Documents / Chunks テーブル）
```

## 技術スタック

詳細は [docs/tech-stack.md](docs/tech-stack.md) を参照してください。

## セットアップ

詳細は [docs/development-setup.md](docs/development-setup.md) を参照してください。

```bash
nuget restore src\LocalRagApplication\LocalRagApplication.csproj
nuget restore tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj
msbuild LocalRagApplication.slnx /p:Configuration=Debug
```

その後、Visual Studio で `LocalRagApplication.slnx` を開いて IIS Express で実行するか、`/server:start` コマンドで起動する。

## カスタムコマンド

Claude Code で使えるカスタムスラッシュコマンドの一覧です。
各コマンドの実体は `.claude/commands/` 配下の `.md`（および `.ps1`）ファイルです。

### 開発

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/setup` | 初回セットアップ（.NET Framework 4.8 Developer Pack確認 → nuget restore） | `setup.ps1` | プロジェクト作成直後に1回 |
| `/server:start` | 開発サーバー（IIS Express）を起動する | `server/start.ps1` | 作業開始時 |
| `/server:stop` | 開発サーバー（IIS Express）を停止する | `server/stop.ps1` | 作業終了時 |
| `/lint` | スタイル検証（非SDK形式のため自動フォーマッタ未導入、要検討） | `lint.md` 参照 | コード変更後 |
| `/typecheck` | ビルドによる型検査（C#はコンパイル時に型検査されるため） | `msbuild LocalRagApplication.slnx /p:Configuration=Debug` | コード変更後 |
| `/format` | コード整形（非SDK形式のため自動フォーマッタ未導入、要検討） | `format.md` 参照 | コミット前 |
| `/test` | MSTest によるテスト実行 | `vstest.console.exe` | コード変更後 |
| `/check` | typecheck → test → build の一括総点検 | `check.md` 参照 | **プッシュ・デプロイ前** |
| `/build` | 本番用ビルド | `msbuild LocalRagApplication.slnx /p:Configuration=Release` | デプロイ前の確認 |
| `/deploy` | デプロイ手順の案内（PR マージ → 自動デプロイ、ホスティング先は未定） | `deploy.md` 参照 | リリース時 |
| `/db:migrate` | DB マイグレーション（DB未確定のため方針は要検討） | `db/migrate.md` 参照 | スキーマ変更時 |

### Git

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/git:init` | git リポジトリを初期化し、git hooks（pre-push の機密情報チェック）を登録 | `git/init.ps1` | プロジェクト作成直後に1回（`/setup` の後） |
| `/git:branch <名前>` | ブランチを作成してチェックアウト（`feature/…` `fix/…` `docs/…`） | `git/branch.ps1` | 作業開始時 |
| `/git:diff` | 変更内容の差分を表示 | `git diff` | コミット前の確認 |
| `/git:push "<メッセージ>"` | シークレット・main 直プッシュをチェックした上でコミット＆プッシュ | `git/push.ps1` | 作業の区切り |
| `/git:pr` | 現在のブランチから main への PR を作成 | `gh pr create` | プッシュ後 |

> **注意**: プッシュは生の `git push` ではなく必ず `/git:push` を使うこと（[docs/git-rules.md](docs/git-rules.md) 参照）。

### その他

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/plan` | 会話内容をもとに実装プランを Plan mode で整理 | `plan.md` 参照 | 実装に入る前 |

### 日常の基本フロー

```
プロジェクト作成直後
  → /setup                 初回セットアップ（.NET確認・nuget restore）
  → /git:init              git リポジトリ初期化・hooks 登録

セッション開始
  → /git:branch feature/<名前>   作業ブランチ作成
  → /server:start          開発サーバー起動
  → （実装・確認を繰り返す。随時 /git:diff）
  → /check                 プッシュ前の総点検
  → /git:push "feat: …"    コミット＆プッシュ
  → /git:pr                PR 作成 → マージ → 自動デプロイ
```

### コマンドを追加するには

1. `.claude/commands/` に `<コマンド名>.md` を作成する（サブフォルダは `:` 区切りの名前空間になる。例: `git/pr.md` → `/git:pr`）
2. ファイル本文に Claude への指示を書く。`$ARGUMENTS` で引数を受け取れる
3. この一覧表にも行を追加する

## サブエージェント

Claude Code が特定のタスクを委譲するサブエージェントの一覧です。
各エージェントの実体は `.claude/agents/` 配下の `.md` ファイルです。

### エージェント一覧

| エージェント | 役割 | ツール | 使うタイミング |
|---|---|---|---|
| `code-reviewer` | 変更ファイルの言語に応じた `docs/*-contributing.md`・`factcheck.md` に基づくコードレビュー | Read, Grep, Glob | 実装完了後、コミット前 |
| `coder` | 変更対象の言語に応じた `docs/*-contributing.md` に従った実装（クラス・メソッド・ロジックの新規実装や修正） | Read, Write, Edit, Glob, Grep, Bash | 実装時 |

## ドキュメント

- [開発環境セットアップガイド](docs/development-setup.md)
- [技術スタック](docs/tech-stack.md)
- [Git運用ルール](docs/git-rules.md)
- [コントリビュートガイド](docs/csharp-contributing.md)
