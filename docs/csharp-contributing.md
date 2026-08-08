# コントリビュートガイド

## 開発フロー

1. `main` ブランチから feature ブランチを作成する
2. 変更を実装する
3. プルリクエストを作成する

## コーディング規約

### 言語・型

- C# のソースファイルの拡張子は `.cs` のみを使用する（Razor ビューの `.cshtml` は対象外。規約は [razor-contributing.md](razor-contributing.md) を参照）
- 対象ランタイムは .NET Framework 4.8（[docs/tech-stack.md](tech-stack.md) 参照）。ASP.NET MVC 5 として実装する
- `var` は代入の右辺から型が明確に分かる場合のみ使用する

出典:
- [.NET コーディング規則 - C# — Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Nullable reference types - C# reference — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-reference-types)

### 命名

- クラス名・メソッド名: PascalCase（例: `UserRepository`）
- メソッド引数・ローカル変数・プライベートフィールド: camelCase
- プライベートインスタンスフィールド: `_` で始める（例: `_workerQueue`）
- 定数（フィールド・ローカル定数）: PascalCase
- インターフェース名: 先頭に `I` を付けた PascalCase（例: `IUserRepository`）
- 識別子に連続する2つのアンダースコア（`__`）を含めない（コンパイラ生成識別子用に予約されているため）

出典: [識別子名 - 規則 - C# — Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/identifier-names)

### 非同期処理

- I/O を伴う処理は `async`/`await` を使用する
- 非同期メソッド名には `Async` サフィックスを付ける（イベントハンドラーなど、コードから明示的に呼び出されないメソッドは対象外）
- `async void` はイベントハンドラー以外では使用しない（例外が呼び出し元でキャッチできない、テストが困難、呼び出し元が非同期を想定していない場合に問題を起こす、という理由から）

出典: [非同期プログラミングのシナリオ - C# — Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/asynchronous-programming/async-scenarios)

### ディレクトリ

| パス | 役割 |
|---|---|
| `src/LocalRagApplication/Controllers/` | MVCコントローラー |
| `src/LocalRagApplication/Views/` | Razorビュー（`.cshtml`）。規約は [razor-contributing.md](razor-contributing.md) |
| `src/LocalRagApplication/App_Start/` | 起動時設定（Bundle/Filter/Route） |
| `src/LocalRagApplication/Models/` | モデル |
| `src/LocalRagApplication/Services/` | アプリケーションサービス・リポジトリ（`TextExtraction/`・`Chunking/`・`Ollama/` に機能別のサブフォルダを作る） |
| `src/LocalRagApplication/Infrastructure/` | 横断的な基盤コード（パス解決・設定読み取り・数値計算・ロギング等） |
| `tests/LocalRagApplication.Tests/` | MSTest テストクラス（テスト対象の `src/` 側と同じフォルダ構成をミラーする） |
| `tests/LocalRagApplication.Tests/TestDoubles/` | 手書きのフェイク実装（モックライブラリは未導入） |
| `tests/LocalRagApplication.Tests/Fixtures/` | テスト用のサンプルファイル |
| `docs/sql/` | SQLファイル（マイグレーション・初期データ等） |

### ビルド対象への登録

本プロジェクトの csproj は非SDK形式（packages.config 方式）のため、新規ファイルは csproj に登録しないとコンパイル対象にならない。

- `.cs` を追加したら `<Compile Include>` を追記する
- `.cshtml` を追加したら `<Content Include>` を追記する

対象の csproj は以下の2つ。

| 追加先 | csproj |
|---|---|
| `src/LocalRagApplication/` | [src/LocalRagApplication/LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) |
| `tests/LocalRagApplication.Tests/` | [tests/LocalRagApplication.Tests/LocalRagApplication.Tests.csproj](../tests/LocalRagApplication.Tests/LocalRagApplication.Tests.csproj) |

登録し忘れてもビルドは成功してしまう（そのファイルが存在しないものとして扱われる）ため、ビルドの成否では検出できない。ファイルを追加したときは必ず登録を確認すること。

### コメント

- コメントは日本語で書く
- 自明な処理にコメントは書かない。「なぜそうしているか」が非自明な場合のみ書く
- public なクラス・メソッド・フィールドには XML ドキュメントコメントを記載する（`<summary>` / `<param>` / `<returns>` / `<exception>`）

出典: [.NET コーディング規則 - C#（コメントのスタイル） — Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/coding-conventions)

例：
```csharp
/// <summary>
/// ユーザーの認証トークンを検証する。
/// </summary>
/// <param name="token">JWT トークン文字列。</param>
/// <returns>トークンが有効な場合は true。</returns>
/// <exception cref="TokenExpiredException">トークンの有効期限が切れている場合。</exception>
public bool ValidateToken(string token)
{
    // 有効期限が切れていないかチェック
    if (token.ExpiresAt < DateTime.UtcNow)
    {
        throw new TokenExpiredException();
    }

    // 実装
}
```

### ブロック

- `if` / `for` などのブロックは、1行であっても必ず `{}` で囲む（省略しない）

例：
```csharp
// 良い
if (isValid)
{
    return true;
}

// 悪い
if (isValid) return true;
```

出典なし（プロジェクト独自の方針。可読性・差分の見やすさを優先するための取り決め）

### テスト

- テストフレームワークは MSTest を使用する（`[TestClass]` / `[TestMethod]` 属性を付与する）
- テストクラスは対象クラス名 + `Test`（例: `HomeControllerTest.cs`、[tests/LocalRagApplication.Tests/Controllers/HomeControllerTest.cs](../tests/LocalRagApplication.Tests/Controllers/HomeControllerTest.cs) 参照）とする
- テストメソッド名は `対象メソッド名_期待する振る舞い` とし、振る舞いの部分は日本語で書く（例: `Split_chunkSizeが0以下の場合はArgumentExceptionをスローする`）

#### テストを書く基準

以下のいずれかに当てはまる public メソッドを追加・変更したときはテストを書く。

- 条件分岐がある（入力によって戻り値・副作用が変わる）
- データ変換を行う（型変換・シリアライズ・分割・整形）
- 引数を検証して例外をスローする
- 件数・順序・境界値を制御する（上位N件・オーバーラップ等）

以下は対象外とする。

- `Models/` 配下のファイル
- `App_Start/` の起動時設定・`Global.asax.cs`・`Properties/AssemblyInfo.cs`
- インターフェース定義のみのファイル
- メッセージ保持のみの例外クラス

上記のいずれにも明確に当てはまらず判断に迷う場合は、実装を進める前にユーザーに確認すること。

`Models/` は機械検査（[.claude/commands/verify-tests.ps1](../.claude/commands/verify-tests.ps1)）ではディレクトリ単位で無条件に除外している。データ保持のみのクラス・enum を置く分には問題ないが、`Models/` にロジックを持つクラスを置く場合は機械検査では検出されないため、レビューで見る必要がある。

#### 外部依存の扱い

| 依存先 | テストでの扱い | 参考にする既存テスト |
|---|---|---|
| SQLite | 実DBを使う。`Path.GetTempFileName()` の一時ファイルに接続し、`[TestCleanup]` で `SQLiteConnection.ClearAllPools()` を呼んでからファイルを削除する | [tests/LocalRagApplication.Tests/Services/QueryServiceTest.cs](../tests/LocalRagApplication.Tests/Services/QueryServiceTest.cs) |
| Ollama | 実通信は行わない。`IOllamaClient` を `FakeOllamaClient` に差し替える | [tests/LocalRagApplication.Tests/Services/DocumentIngestionServiceTest.cs](../tests/LocalRagApplication.Tests/Services/DocumentIngestionServiceTest.cs) |
| 入力ファイル | `Fixtures/` の実ファイルを `FakeHttpPostedFile.FromFile` 経由で渡す | 同上 |
| 出力ファイル | `Path.GetTempPath()` 配下に GUID 付きディレクトリを作り、`[TestCleanup]` で削除する | [tests/LocalRagApplication.Tests/Infrastructure/FileQueryMetricsLoggerTest.cs](../tests/LocalRagApplication.Tests/Infrastructure/FileQueryMetricsLoggerTest.cs) |
| ロギング | `FakeIngestionLogger` / `FakeQueryMetricsLogger` に差し替える | [tests/LocalRagApplication.Tests/Services/DocumentIngestionServiceTest.cs](../tests/LocalRagApplication.Tests/Services/DocumentIngestionServiceTest.cs) |

- モックライブラリは未導入。テストダブルは `tests/LocalRagApplication.Tests/TestDoubles/` に手書きで追加する（既存ファイルと同じ形式）
- 依存を外から差し替えられない場合は、既定コンストラクタを残したまま依存を受け取るコンストラクタを追加する。本番経路は既定コンストラクタのまま変えない（既存例: `SqliteDocumentRepository(string connectionString)`、`FileQueryMetricsLogger(string logsDir)`。どちらも「テスト等で〜を使う場合を想定」という XML コメント付きで実在する）

#### 関連ルール

- 新規テストファイルも csproj への `<Compile Include>` 登録が必要。詳細は同ファイル内の「[ビルド対象への登録](#ビルド対象への登録)」を参照
- テストクラスの欠落は機械検査（`/check` と CI が実行する [.claude/commands/verify-tests.ps1](../.claude/commands/verify-tests.ps1)）が検出する。除外を宣言する場合は [tests/LocalRagApplication.Tests/no-test-required.md](../tests/LocalRagApplication.Tests/no-test-required.md) に理由付きで追記する

## 禁止事項

- 存在しない NuGet パッケージ・API・メソッドの使用（実装前に [.claude/factcheck.md](../.claude/factcheck.md) のチェックリストに従い実在確認する）
- APIキー・シークレットのコードへの直書き（管理方法は [docs/development-setup.md](development-setup.md) の「シークレットの設定」を参照）
- シークレットを含むファイルの git へのコミット
- 本番コードでの `Console.WriteLine` 等の直接出力（ロギングは `Infrastructure/` のロガー抽象を使用する。取り込み処理の警告・エラーは [`IIngestionLogger`](../src/LocalRagApplication/Infrastructure/IIngestionLogger.cs)、処理時間の計測は [`IQueryMetricsLogger`](../src/LocalRagApplication/Infrastructure/IQueryMetricsLogger.cs)）

## プルリクエストのルール

- タイトルはコミットメッセージ規約に従う（[git-rules.md](git-rules.md) 参照）
- CI がすべて通過していること
