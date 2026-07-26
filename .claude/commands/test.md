以下のコマンドを実行してテストを実行してください（テストフレームワークは MSTest）。

```powershell
msbuild LocalRagApplication.slnx /p:Configuration=Debug
vstest.console.exe tests\LocalRagApplication.Tests\bin\Debug\LocalRagApplication.Tests.dll
```

失敗したテストがあれば内容を確認して報告してください。
