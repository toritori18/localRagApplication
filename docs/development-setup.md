# 開発環境セットアップガイド

## 必要環境

- .NET Framework 4.8 Developer Pack（[LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) の `TargetFrameworkVersion` が `v4.8`）
- Visual Studio 2022（「ASP.NET と Web開発」ワークロード。IIS Express・MSBuild・vstest.console.exe を含む）
- Git

## インストール

```bash
git clone <リポジトリURL>
cd LocalRagApplication
nuget restore src\LocalRagApplication\LocalRagApplication.csproj
nuget restore tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj
```

> `nuget restore` に `LocalRagApplication.slnx`（新形式のソリューションファイル）を直接渡す方法は、classic な `nuget.exe` CLI の対応状況が未確認のため、プロジェクト単位で個別に復元する（[.github/workflows/ci.yml](../.github/workflows/ci.yml) も同じ方式）。

## Git フックの登録

push 前の機密情報チェック（`.claude/hooks/pre-push`）を有効にするため、クローン後に必ず実行してください。

```bash
git config core.hooksPath .claude/hooks
```

> `.claude/commands/init/deps.ps1`（初回セットアップスクリプト、`/init:deps` コマンド）を実行した場合は自動で登録されます。

## シークレットの設定

APIキー等のシークレットはコードや `Web.config` に直書きしない。.NET Framework（非SDK形式）では `dotnet user-secrets` は使用できないため、以下のいずれかで管理する:

- ローカル限定の設定値は `.gitignore` 対象のファイル（例: `Web.Development.config` のような非コミットのconfig変換ファイルや環境変数）で管理する
- 具体的な方式（Web.config変換／環境変数／その他）は RAG関連の構成が確定した時点で要検討・要確認

> 上記は現時点で確定した方式ではない。実装時に改めて確認すること。

- RAG関連（埋め込みモデル・ベクトルDB・LLMプロバイダ）の構成は未定のため、詳細は [docs/tech-stack.md](tech-stack.md) を参照してください

## 開発サーバーの起動

- Visual Studio で `LocalRagApplication.slnx` を開いてデバッグ実行（IIS Express）する場合: ブラウザで `https://localhost:44367/`（[LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) の `IISUrl`）を開く
- `/server:start` コマンド（`iisexpress.exe` のアドホック起動）を使う場合: HTTPのみで待ち受けるため `http://localhost:58398/`（`DevelopmentServerPort`）を開く

## ビルド

```bash
msbuild LocalRagApplication.slnx /p:Configuration=Debug
```

## テスト

```bash
vstest.console.exe tests\LocalRagApplication.Tests\bin\Debug\LocalRagApplication.Tests.dll
```

テストフレームワークは MSTest（[tests/LocalRagApplication.Tests](../tests/LocalRagApplication.Tests)）。`vstest.console.exe` は Visual Studio に同梱されている。
