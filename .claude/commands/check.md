プッシュ前の総点検として、以下のコマンドを実行してください。

```powershell
.claude\commands\check.ps1
```

以下を順に実行し、途中で失敗したら後続を実行せず停止します（`exit 1`）。

1. ドキュメントの参照先検査（`verify-docs.ps1`）— 追跡対象の `.md` について、相対リンクのリンク先・アンカー（`#見出し`）・言及されているスラッシュコマンドが実在するかを確認します
2. テストクラスの欠落検査（`verify-tests.ps1`）— `src/LocalRagApplication/` の各クラスに対応するテストクラスが存在するか、無い場合は `tests/LocalRagApplication.Tests/no-test-required.md` に理由付きで除外宣言されているかを確認します
3. Release ビルド + テスト（`vs-tools.ps1 -Task Test -Configuration Release`）— ビルドを実行してから、その成果物に対してテストを実行します（ビルドに失敗した場合はテストを実行せずに終了します）

自動フォーマッタ・スタイル検証（lint/format）は現状未導入のため、この総点検には含まれません（[lint.md](lint.md) 参照）。

失敗した場合はそこで停止し、エラー内容を報告してください。
すべて通った場合は「チェックOK。プッシュ可能です」と報告してください。
