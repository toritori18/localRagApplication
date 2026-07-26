以下のスクリプトを実行して初回セットアップを行ってください。

```powershell
.\.claude\commands\init\deps.ps1
```

このスクリプトが実行する内容:

1. **.NET Framework 4.8 の確認** — レジストリを見て、Developer Pack がインストール済みか確認する（未インストールならエラーで停止し、ダウンロードURLを表示）
2. **NuGet パッケージの復元** — `nuget restore` を本体プロジェクト（`src\LocalRagApplication\LocalRagApplication.csproj`）とテストプロジェクト（`tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj`）それぞれに対して実行する（`nuget` コマンドが無い場合はエラーで停止し、インストールURLを表示）

git リポジトリの初期化・hooks 登録はこのスクリプトには含まれない。別コマンド `/git:init` で行う。

セットアップ完了後、以下を案内してください:

1. git リポジトリの初期化・hooks 登録は `/git:init` コマンドで行うこと
2. .env.local に必要な接続情報を設定すること
