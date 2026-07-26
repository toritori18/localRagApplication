プッシュ前の総点検として、以下のコマンドを順番に実行してください（失敗した時点で停止します）。

```powershell
msbuild LocalRagApplication.slnx /p:Configuration=Release
vstest.console.exe tests\LocalRagApplication.Tests\bin\Release\LocalRagApplication.Tests.dll
```

自動フォーマッタ・スタイル検証（lint/format）は現状未導入のため、この総点検には含まれません（[lint.md](lint.md) 参照）。

失敗した場合はそこで停止し、エラー内容を報告してください。
すべて通った場合は「チェックOK。プッシュ可能です」と報告してください。
