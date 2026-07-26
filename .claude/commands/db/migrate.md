データベースのマイグレーションを実行してください。

> **注意**: DB・ベクトルストアの選定は未定・要検討です。決定次第このファイルを実際のプロバイダに合わせて更新してください。
>
> `dotnet ef`（EF Core CLI）はSDK形式プロジェクト前提のため、現在の `src/LocalRagApplication`（.NET Framework 4.8, 非SDK形式）ではそのままでは使用できません。.NET FrameworkでEF Coreを使う場合はプロジェクトをSDK形式に変換するか、EF6（`Add-Migration`/`Update-Database`、Visual StudioのPackage Manager Console経由）を使う必要があります。方式は未定のため、以下は参考情報です。

## EF Core を使う場合（プロジェクトをSDK形式に変換した場合の参考手順）

```powershell
dotnet ef migrations add <マイグレーション名>
dotnet ef database update
```

- `dotnet-ef` ツールが未インストールの場合は `dotnet tool install --global dotnet-ef` を実行する
- 生SQLが必要な場合は `docs/sql/` 配下に配置する

注意事項:
- 本番環境では事前にバックアップを取得してください
