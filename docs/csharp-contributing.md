# コントリビュートガイド

## 開発フロー

1. `main` ブランチから feature ブランチを作成する
2. 変更を実装する
3. プルリクエストを作成する

## コーディング規約

### 言語・型

- 拡張子は `.cs` のみを使用する
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
| `src/LocalRagApplication/Views/` | Razorビュー（`.cshtml`） |
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

## 禁止事項

- 存在しない NuGet パッケージ・API・メソッドの使用（実装前に [.claude/factcheck.md](../.claude/factcheck.md) のチェックリストに従い実在確認する）
- APIキー・シークレットのコードへの直書き（管理方法は [docs/development-setup.md](development-setup.md) の「シークレットの設定」を参照）
- シークレットを含むファイルの git へのコミット
- 本番コードでの `Console.WriteLine` 等の直接出力（ロギングは `Infrastructure/` のロガー抽象を使用する。取り込み処理の警告・エラーは [`IIngestionLogger`](../src/LocalRagApplication/Infrastructure/IIngestionLogger.cs)、処理時間の計測は [`IQueryMetricsLogger`](../src/LocalRagApplication/Infrastructure/IQueryMetricsLogger.cs)）

## プルリクエストのルール

- タイトルはコミットメッセージ規約に従う（[git-rules.md](git-rules.md) 参照）
- CI がすべて通過していること
