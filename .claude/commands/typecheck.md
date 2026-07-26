以下のコマンドを実行してビルドを行い、型検査をしてください（C# はコンパイル言語のため、型検査はビルドと同じ処理です）。

```powershell
msbuild LocalRagApplication.slnx /p:Configuration=Debug
```

型エラーが出た場合は内容を確認して報告してください。
