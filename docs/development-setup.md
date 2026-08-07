# 開発環境セットアップガイド

## 必要環境

- .NET Framework 4.8 Developer Pack（[LocalRagApplication.csproj](../src/LocalRagApplication/LocalRagApplication.csproj) の `TargetFrameworkVersion` が `v4.8`）
- Visual Studio 2022（「ASP.NET と Web開発」ワークロード。IIS Express・MSBuild・vstest.console.exe を含む）
- Git

## インストール

```bash
git clone <リポジトリURL>
cd LocalRagApplication
nuget restore src\LocalRagApplication\LocalRagApplication.csproj -SolutionDirectory .
nuget restore tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj -SolutionDirectory .
```

> `nuget restore` に `LocalRagApplication.slnx`（新形式のソリューションファイル）を直接渡す方法は、classic な `nuget.exe` CLI の対応状況が未確認のため、プロジェクト単位で個別に復元する（[.github/workflows/ci.yml](../.github/workflows/ci.yml) も同じ方式）。

> **`-SolutionDirectory .` は省略しないこと。** packages.config 方式では復元先の `packages/` フォルダを決める基準が必要だが、プロジェクト単位の復元ではそれを特定できず、`NuGet パッケージを復元するパッケージ フォルダーを特定できません` というエラーで停止する（実測）。リポジトリルートを明示することで、csproj の `HintPath`（`..\..\packages\`）と復元先が一致する。

## Git フックの登録

push 前の機密情報チェック（`.claude/hooks/pre-push`）を有効にするため、クローン後に必ず実行してください。

```bash
git config core.hooksPath .claude/hooks
```

> `.claude/commands/init/deps.ps1`（初回セットアップスクリプト、`/init:deps` コマンド）を実行した場合は自動で登録されます。

## Claude Code 設定の編集制限

`.claude/settings.json` の `permissions.deny` により、以下は Claude Code 自身が編集できないよう設定されている。

| 対象 | 内容 |
|---|---|
| `.claude/settings.json` | 権限設定・フック登録 |
| `.claude/hooks/` 配下 | `pre-push`（push 前の機密情報チェック）、`guard-claude-md.ps1`、`inject-factcheck.ps1`、`session-check.ps1` |

Claude Code に自身の権限設定やガード用フックを書き換えさせないための意図的な設定（自己保護）。**これらを変更する場合は人が直接編集すること。** Claude Code に依頼しても拒否される。

この禁止は `Edit`/`Write` ツールだけでなく、シェル経由のファイル操作にも適用される（`.claude/hooks/` に対する `Move-Item`・`Remove-Item` がいずれもブロックされることを実測で確認済み）。なおブロック時のメッセージは「許可された作業ディレクトリ外」という文面になるが、実際の原因は `deny` パターンである。

あわせて `Read(./.env)` / `Read(./.env.*)` も禁止されており、シークレットファイルの内容は読み取れない。

> **フックスクリプトは必ず `.claude/hooks/` 配下に置くこと。** `deny` のパターンが `./.claude/hooks/**` であるため、`.claude/` 直下など別の場所に置くと保護対象から外れる。新しいフックを追加する際は、`settings.json` に登録するパスと実際の配置の両方を `.claude/hooks/` に揃える。

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

```powershell
.claude\commands\vs-tools.ps1 -Task Build -Configuration Debug
```

`-Configuration Release` を指定すると本番用ビルドになる（`/build` と同じ）。

> **`msbuild` / `vstest.console.exe` は Visual Studio に同梱されているが、PATH には登録されない。** そのままコマンド名で実行しても「認識されません」で失敗する。上記スクリプトは `vswhere.exe`（Visual Studio Installer 同梱、`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\` の固定パス）で実体のパスを解決してから実行するため、通常の PowerShell からそのまま実行できる。
>
> Visual Studio の Developer PowerShell（インストール先の `Common7\Tools\Launch-VsDevShell.ps1`）から起動した場合は PATH が通るため、`msbuild LocalRagApplication.slnx /p:Configuration=Debug` を直接実行してもよい。
>
> なお `dotnet` は PATH にあるが、`dotnet msbuild` ではビルドできない（非SDK形式の ASP.NET プロジェクトに必要な `Microsoft.WebApplication.targets` が .NET SDK 側に存在せず、`MSB4019` で失敗する。実測）。Visual Studio 同梱の MSBuild が必須。

## テスト

```powershell
.claude\commands\vs-tools.ps1 -Task Test -Configuration Debug
```

ビルドを実行してから、その成果物に対してテストを実行する（ビルドに失敗した場合はテストを実行せず終了する）。`-Configuration Release` を指定すると Release 成果物に対して実行する（`/check` と同じ）。

テストフレームワークは MSTest（[tests/LocalRagApplication.Tests](../tests/LocalRagApplication.Tests)）。`vstest.console.exe` は Visual Studio に同梱されている。
