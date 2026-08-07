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

## プロジェクト作成直後にやること

1. `/init:deps` — 初回セットアップ（.NET Framework 4.8 Developer Pack確認 → nuget restore）
2. `/git:init` — git リポジトリ初期化・hooks 登録（**必ず `/init:deps` の後に実行**）

## ディレクトリ構成

```
LocalRagApplication/
├── CLAUDE.md                          # Claude Code 向けガイド
├── README.md                          # このファイル（プロジェクト説明）
├── LocalRagApplication.slnx           # ソリューションファイル
├── .gitignore
├── .editorconfig                      # 文字コード・改行・インデント（.ps1 は BOM 必須・CRLF）
├── .env.example                       # 環境変数のサンプル（シークレット管理方式は未確定）
├── .claude/
│   ├── settings.json                  # 権限・フック設定
│   ├── factcheck.md                   # ハルシネーション防止ルール
│   ├── hooks/                         # Git hooks・Claude Code フックスクリプト
│   ├── agents/                        # サブエージェント定義
│   └── commands/                      # カスタムスラッシュコマンド
│       ├── git/                       # Git関連（init/branch/push/pr/merge/cleanup等）
│       ├── init/                      # 初回セットアップ関連（deps）
│       ├── server/                    # サーバー関連
│       ├── db/                        # DB関連
│       └── *.md                       # 名前空間なしのコマンド（build/check/test/typecheck/lint/format/deploy/plan）
├── .github/
│   └── workflows/
│       └── ci.yml                     # CI（nuget restore → Release ビルド → テスト）
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
│       │   └── Shared/                # _Layout.cshtml, _BackToHome.cshtml（各画面共通の「戻る」ボタン部分ビュー）, _ConfirmModal.cshtml（共通の確認ダイアログ）等
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
│       │   └── FileQueryMetricsLogger.cs / IQueryMetricsLogger.cs  # Ollama呼び出し・質問応答の処理時間内訳ログ
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
├── packages/                          # NuGet復元先（packages.config方式、.gitignore対象）
├── data/                              # アップロードされた元ファイル・索引データ（.gitignore 対象、フォルダのみ保持）
│   ├── sources/                       # アップロードされた元ファイルの保存先
│   ├── extracted/                     # テキスト抽出後の中間ファイル（.txt）
│   ├── logs/                          # ログ（ingestion.log: 取り込み処理、query-metrics-yyyy-MM-dd.log: Ollama呼び出し・質問応答の処理時間内訳、保持日数は既定7日で自動削除）
│   └── rag.db                         # ドキュメントメタデータ・チャンク・埋め込みベクトルを保存するSQLiteデータベース
├── docs/                              # ドキュメント
│   ├── git-rules.md                   # Git運用ルール
│   ├── tech-stack.md                  # 技術スタック
│   ├── development-setup.md           # 開発環境セットアップガイド
│   ├── csharp-contributing.md         # コントリビュートガイド（C#）
│   ├── powershell-contributing.md     # コントリビュートガイド（PowerShell）
│   ├── sample-documents/              # 動作検証用のサンプル文書（架空の内容。/Documents からアップロードして試せる）
│   └── sql/                           # SQLファイル（マイグレーション・初期データ等）
│       └── 001_create_tables.sql      # rag.db の初期スキーマ（Documents / Chunks テーブル）
```

## 技術スタック

詳細は [docs/tech-stack.md](docs/tech-stack.md) を参照してください。

## セットアップ

詳細は [docs/development-setup.md](docs/development-setup.md) を参照してください。

```powershell
nuget restore src\LocalRagApplication\LocalRagApplication.csproj -SolutionDirectory .
nuget restore tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj -SolutionDirectory .
.claude\commands\vs-tools.ps1 -Task Build -Configuration Debug
```

> `-SolutionDirectory .` は省略しないこと。省略すると復元先がリポジトリ直下の `packages/` にならず、csproj の `HintPath`（`..\..\packages\`）と食い違ってビルドが失敗する。

その後、Visual Studio で `LocalRagApplication.slnx` を開いて IIS Express で実行するか、`/server:start` コマンドで起動する。

## カスタムコマンド

Claude Code で使えるカスタムスラッシュコマンドの一覧です。
各コマンドの実体は `.claude/commands/` 配下の `.md`（および `.ps1`）ファイルです。

### 開発

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/init:deps` | 初回セットアップ（.NET Framework 4.8 Developer Pack確認 → nuget restore） | `init/deps.ps1` | プロジェクト作成直後に1回 |
| `/server:start` | 開発サーバー（IIS Express）を起動する | `server/start.ps1` | 作業開始時 |
| `/server:stop` | 開発サーバー（IIS Express）を停止する | `server/stop.ps1` | 作業終了時 |
| `/lint` | スタイル検証（非SDK形式のため未導入。実行されず、その旨が報告される） | `lint.md` 参照 | — （未導入） |
| `/typecheck` | ビルドによる型検査（C#はコンパイル時に型検査されるため） | `vs-tools.ps1 -Task Build -Configuration Debug` | コード変更後 |
| `/format` | コード整形（非SDK形式のため未導入。実行されず、その旨が報告される） | `format.md` 参照 | — （未導入） |
| `/test` | MSTest によるテスト実行（Debug ビルド → `vstest.console.exe`） | `test.md` 参照 | コード変更後 |
| `/check` | プッシュ前の総点検（Release ビルド → Release ビルド成果物に対するテスト） | `check.md` 参照 | **プッシュ・デプロイ前** |
| `/build` | 本番用ビルド | `vs-tools.ps1 -Task Build -Configuration Release` | デプロイ前の確認 |
| `/deploy` | デプロイ手順の案内（PR マージ → 自動デプロイ、ホスティング先は未定） | `deploy.md` 参照 | リリース時 |
| `/db:migrate` | DB マイグレーション（DB未確定のため方針は要検討） | `db/migrate.md` 参照 | スキーマ変更時 |

### Git

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/git:init` | git リポジトリを初期化し、git hooks（pre-push の機密情報チェック）を登録 | `git/init.ps1` | **プロジェクト作成直後に1回（`/init:deps` の後）** |
| `/git:branch <名前>` | ブランチを作成してチェックアウト（`feature/…` `fix/…` `docs/…`） | `git/branch.ps1` | 作業開始時 |
| `/git:diff` | 変更内容の差分を表示 | `git diff` | コミット前の確認 |
| `/git:push "<メッセージ>"` | シークレット・main 直プッシュをチェックした上でコミット＆プッシュ | `git/push.ps1` | 作業の区切り |
| `/git:pr` | 現在のブランチから main への PR を作成 | `git/pr.md` 参照 | プッシュ後 |
| `/git:merge [PR番号]` | CI・マージ可否を確認した上で PR を main へマージ（ブランチは削除しない） | `git/merge.md` 参照 | PR 作成後 |
| `/git:cleanup` | main にマージ済みのローカルブランチを一覧表示し、確認後に削除（既定は一覧のみ） | `git/cleanup.ps1` | マージ後の整理時 |

> **注意**: プッシュは生の `git push` ではなく必ず `/git:push` を使うこと（[docs/git-rules.md](docs/git-rules.md) 参照）。

### その他

| コマンド | 内容 | 実体 | 使うタイミング |
|---|---|---|---|
| `/plan` | 会話内容をもとに実装プランを Plan mode で整理 | `plan.md` 参照 | 実装に入る前 |

### 日常の基本フロー

```
プロジェクト作成直後
  → /init:deps             初回セットアップ（.NET確認・nuget restore）
  → /git:init              git リポジトリ初期化・hooks 登録

セッション開始
  → /git:branch feature/<名前>   作業ブランチ作成
  → /server:start          開発サーバー起動
  → （実装・確認を繰り返す。随時 /git:diff）
  → /check                 プッシュ前の総点検
  → /git:push "feat: …"    コミット＆プッシュ
  → /git:pr                PR 作成
  → /git:merge             CI 確認 → main へマージ → 自動デプロイ
  → /git:cleanup           マージ済みブランチの整理（任意）
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
| `readme-syncer` | リポジトリ全体を読み、`README.md` と実体（`.claude/commands/`・`.claude/agents/`・ディレクトリ構成）の乖離を検出・修正する（`README.md` 以外は編集しない） | Read, Edit, Glob, Grep | コマンド・エージェント・ディレクトリ構成の追加/削除/リネーム後 |

## ドキュメント

- [開発環境セットアップガイド](docs/development-setup.md)
- [技術スタック](docs/tech-stack.md)
- [Git運用ルール](docs/git-rules.md)
- [コントリビュートガイド（C#）](docs/csharp-contributing.md)
- [コントリビュートガイド（PowerShell）](docs/powershell-contributing.md)
