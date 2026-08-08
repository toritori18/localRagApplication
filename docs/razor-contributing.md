# Razor ビュー コントリビュートガイド

`src/LocalRagApplication/Views/` 配下の `.cshtml`（Razor ビュー）を書くときの規約。

C# のソースファイル（コントローラー・サービス等の `.cs`）は [csharp-contributing.md](csharp-contributing.md) を参照する。
ビュー内に書く C# 式・コードブロックについても、命名・ブロック（`{}` の省略禁止）は同ドキュメントに従う。

## コーディング規約

### ファイル形式

- **BOM なし** UTF-8 で保存する
- 改行は LF、インデントはスペース4
- 正は [.editorconfig](../.editorconfig) の `[*.cshtml]` セクション

BOM 付き UTF-8 は `fileEncoding` の値によらず自動認識されるとドキュメント化されているが、本プロジェクトでは
`.cs` と揃えて BOM なしに統一する。BOM なしのビューが日本語を含んでも正しくレンダリングされることは実測で確認済み。

[src/LocalRagApplication/Web.config](../src/LocalRagApplication/Web.config) の `<globalization fileEncoding="utf-8" />`
は削除しないこと。ただしこの設定の適用対象として下記出典に明記されているのは `.aspx` / `.asmx` / `.asax` であり、
**`.cshtml` に効くとは書かれていない**。BOM なしのビューが動作している根拠はこの設定ではなく実測である点に注意する。

出典: [GlobalizationSection.FileEncoding プロパティ — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.web.configuration.globalizationsection.fileencoding)

### ビルド対象への登録

`.cshtml` を新規追加したら csproj に `<Content Include>` を追記する。登録し忘れてもビルドは成功してしまう。
詳細は [csharp-contributing.md の「ビルド対象への登録」](csharp-contributing.md#ビルド対象への登録)を参照。

なお [LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) は `MvcBuildViews` が `false` のため、
**ビューの構文エラーはビルドでは検出されない**（実行してその画面を開いたときに初めて失敗する）。
ビューを変更したら実際に画面を表示して確認すること。

### ビューの構成

ファイル冒頭は次の順に書く。

```cshtml
@using LocalRagApplication.Models
@model IEnumerable<DocumentMetadata>
@{
    ViewBag.Title = "ドキュメント管理";
}
```

- `@model` の型は `@using` を書いて短縮名で記述する（完全修飾名を直接書かない）
- `ViewBag.Title` を必ず設定する。[_Layout.cshtml](../src/LocalRagApplication/Views/Shared/_Layout.cshtml) が
  `<title>@ViewBag.Title - ローカルRAG検索</title>` として使う
- レイアウトは [_ViewStart.cshtml](../src/LocalRagApplication/Views/_ViewStart.cshtml) が一括で指定する。
  各ビューで `Layout` を設定しない

### レイアウトと共通部品

| 部分ビュー | 呼び出す場所 | 用途と制約 |
|---|---|---|
| [_ConfirmModal.cshtml](../src/LocalRagApplication/Views/Shared/_ConfirmModal.cshtml) | `_Layout.cshtml` の**1箇所のみ**。各画面から呼ばない | 確認ダイアログ。利用側は form 内のボタンに `data-confirm-message` 属性を付けるだけでよい。要素の `id` が固定のため複数回レンダリングすると壊れる |
| [_BackToHome.cshtml](../src/LocalRagApplication/Views/Shared/_BackToHome.cshtml) | ホーム以外の各画面の**先頭** | ホーム画面へ戻る唯一の導線。ヘッダーにナビゲーションが無いため、これを省くと戻れなくなる |

新しく部分ビューを追加するときは、ファイル先頭に `@* *@` で「なぜこの部品が必要か」を書く（既存2ファイルと同じ形式）。

### スクリプト・スタイル

- 共通の CSS / JS は [App_Start/BundleConfig.cs](../src/LocalRagApplication/App_Start/BundleConfig.cs) のバンドルにまとめ、
  `_Layout.cshtml` が `@Styles.Render` / `@Scripts.Render` で読み込む。ビューに `<script src>` / `<link>` を直接書かない
- ページ固有の JS は `@section scripts { }` に入れる。`_Layout.cshtml` の
  `@RenderSection("scripts", required: false)` が jQuery・bootstrap バンドルより後に出力するため、
  これらに依存してよい

### コメント

- コメントは `@* *@` を使い、日本語で書く
- 自明な処理には書かない。「なぜそうしているか」が非自明な場合のみ書く（`.cs` と同じ方針）

## 禁止事項

- **`@Html.Raw` による HTML エンコードの迂回**（現在の使用は0件）。
  ドキュメント本文や質問文などユーザー由来の文字列に使うと XSS になる
- **ネイティブの `confirm()` / `alert()`**。ホスト環境（Electron / ブラウザ）独自のタイトルが表示されてしまうため、
  確認ダイアログは `_ConfirmModal.cshtml` を使う
- **ビュー内でのデータアクセス・外部呼び出し**（リポジトリ・`OllamaClient` の直接利用など。現在の使用は0件）。
  コントローラーまたはサービス側に置き、ビューにはモデル経由で渡す
