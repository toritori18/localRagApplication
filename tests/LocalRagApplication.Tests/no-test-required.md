# テストを書かないクラスの除外宣言

`docs/csharp-contributing.md`「テストを書く基準」に照らして public メソッドを持つクラスであっても、
テストクラスを書かないと判断した場合はこのファイルに理由付きで登録する。

`.claude/commands/verify-tests.ps1`（`/check` と CI から実行される）は以下を検査する。

- `src/LocalRagApplication/` 配下の各クラスに対応するテストクラスが
  `tests/LocalRagApplication.Tests/` 配下に存在するか
  （存在しない場合、対象が自動除外カテゴリ [`Models/` 等] でなければ、このファイルへの登録が必要）
- このファイルに登録されているパスが `src/LocalRagApplication/` 側に実在するか
  （実在しない場合は陳腐化した除外宣言としてエラーになる）
- このファイルの各エントリに理由が書かれているか（空の場合はエラーになる）

同スクリプトは上記に加えて `tests/LocalRagApplication.Tests/` 配下の `.cs` が csproj の
`<Compile Include>` に登録されているかも検査するが、これはこのファイル（除外宣言）の
内容とは無関係な検査である。

## 書式

```
- `<src/LocalRagApplication/ からの相対パス>` — <テストを書かない理由>
```

- パスはバックティックで囲む
- パスと理由の区切りは全角ダッシュ（`—`）
- `#` で始まる行（見出し）と空行は無視される

## エントリ

- `Services/Ollama/OllamaConnectionException.cs` — メッセージと InnerException を保持するだけの例外クラスで、分岐・変換を持たない
