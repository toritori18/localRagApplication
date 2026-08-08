# コントリビュートガイド（PowerShell）

対象は `.claude/` 配下の `.ps1`。用途は2種類ある。

| 場所 | 用途 |
|---|---|
| `.claude/commands/` | スラッシュコマンドから呼ばれる実行スクリプト |
| `.claude/hooks/` | Claude Code のフックスクリプト |

アプリケーション本体のコードは [csharp-contributing.md](csharp-contributing.md) を参照。

## 実行環境

- Windows PowerShell **5.1**（Desktop エディション）。PowerShell 7 系ではない
- スクリプトは Claude Code から非対話で実行される（標準入力からの応答は返らない）

出典: 本環境での実測（`$PSVersionTable.PSVersion` = `5.1.26100.8972`、`PSEdition` = `Desktop`）

### 5.1 で使えない構文

PowerShell 7 で追加された以下は **5.1 のパーサーが受け付けない**。使用しないこと。

| 使えないもの | 代替 |
|---|---|
| `&&` / `\|\|`（パイプラインチェーン演算子） | `if (-not $?) { ... }` で分岐する |
| `? :`（三項演算子） | `if` / `else` |
| `??` / `??=`（null 合体演算子） | `if ($null -eq $x) { ... }` |
| `ConvertFrom-Json -AsHashtable` | 既定の戻り値（`PSCustomObject`）のまま扱う |
| `Get-Process` の `CommandLine` プロパティ | `Get-CimInstance -ClassName Win32_Process`（実例: [.claude/commands/server/common.ps1](../.claude/commands/server/common.ps1)） |

出典: 本環境の PowerShell 5.1 での実測（`?` / `??` / `&&` はいずれもパーサーが「式またはステートメントのトークンを使用できません」で拒否した。`-AsHashtable` はパラメーターを解決できず失敗した。`Get-Process | Get-Member` に `CommandLine` は存在しなかった）

## ファイル形式

文字コード・改行・インデントは [.editorconfig](../.editorconfig) の `[*.ps1]` セクションを正とする。特に以下は必須。

- **文字コードは UTF-8（BOM あり）**
- 改行は CRLF（[.gitattributes](../.gitattributes) の `*.ps1 text eol=crlf` で作業ツリー側も固定している）
- インデントはスペース4

BOM を付け忘れると、5.1 はファイルを ANSI コードページ（日本語環境では CP932）として読む。UTF-8 の日本語コメントが Shift-JIS として区切られてずれ、**コメント行が直後の改行を飲み込んで次の1行が消える**。

- 消えた行が独立した文だった場合 → **エラーも出ず、終了コード 0 のまま挙動だけ壊れる**
- 消えた行がブロックの開始（`if (...) {` 等）だった場合 → パースエラーで停止する

どちらになるかは日本語文字列のバイト数次第で選べない。前者はビルド・テストの成功を素通りするため、実行して確認しても検出できない。

出典: 2026-08-02 に `.claude/commands/git/cleanup.ps1` を BOM なしで作成し、`Unexpected token ')'` のパースエラーが発生した事例。BOM を付けて解決した

## 出力

### 標準出力の文字コード

スクリプトの先頭（`param()` があればその直後）で stdout を UTF-8 に固定する。

```powershell
# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

省略すると 5.1 は ANSI コードページ（CP932）で出力するため、Claude Code 側で日本語が文字化けして読めなくなる。エラーメッセージも読めなくなるため、失敗時に原因が分からなくなる。

他のファイルから dot-source されるだけで単体実行しないファイル（[server/common.ps1](../.claude/commands/server/common.ps1)）は、呼び出し元が設定するため不要。

出典: 本環境での実測（同一の日本語文字列を出力したとき、未設定では `83 5a 83 62 ...`（CP932）、設定後は `e3 82 bb e3 83 83 ...`（UTF-8）となった）

### メッセージ

- 本文は日本語で書く
- 異常系は `ERROR:` / `WARNING:` を先頭に付け、`-ForegroundColor Red` / `Yellow` を指定する
- エラーは「何が起きたか」だけで終わらせず、**復旧手順**まで出す

例：
```powershell
Write-Host "ERROR: iisexpress.exe が見つかりません。Visual Studio 2022 の「ASP.NET と Web開発」ワークロードをインストールしてください（docs/development-setup.md 参照）。" -ForegroundColor Red
```

## エラー処理

### 実行スクリプト（`commands/`）

- 外部コマンド（`git` / `nuget` 等）を呼んだ直後は `if (-not $?) { ... }` で成否を確認する
- 失敗したら原因を出力して `exit 1` で終了する。握りつぶして続行しない
- 別の `.ps1` を `&` で呼んだ場合は、`$?` と `$LASTEXITCODE` の両方を見るのが確実（`if (-not $? -or $LASTEXITCODE -ne 0) { ... }`）。実例: [check.ps1](../.claude/commands/check.ps1)

```powershell
git commit -m $Message
if (-not $?) {
    Write-Host "ERROR: git commit に失敗しました。" -ForegroundColor Red
    exit 1
}
```

```powershell
& (Join-Path $PSScriptRoot 'verify-tests.ps1')
if (-not $? -or $LASTEXITCODE -ne 0) {
    Write-Host "ERROR: テストクラスの欠落検査に失敗しました。" -ForegroundColor Red
    exit 1
}
```

### フックスクリプト（`hooks/`）

フックは終了コードではなく、**stdout に出力する JSON** で Claude Code に結果を伝える。`exit 1` は使わない。

```powershell
@{
    hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'ask'
        permissionDecisionReason = '（理由）'
    }
} | ConvertTo-Json -Compress
```

条件に該当しないときは何も出力しない。

なお `.claude/hooks/**` は [.claude/settings.json](../.claude/settings.json) の `deny` により Edit / Write が禁止されている。フックスクリプトの変更が必要な場合は、自分で編集せずユーザーに依頼すること。

## パス解決

呼び出し元のカレントディレクトリに依存させない。基準は `$PSScriptRoot` から解決する。

```powershell
# このファイルは .claude/commands/server/ に置かれているため、3階層上がリポジトリルートになる
$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
```

dot-source のパス、ログの出力先、`Start-Process` のリダイレクト先も同様に絶対パスで指定する（`Start-Process` のリダイレクト先は呼び出し元のカレントディレクトリを基準に解決されるため）。

## パラメータ

- `param()` ブロックで宣言し、型を明示する（`[string]` / `[int]` 等）
- パラメータ名は PascalCase
- 必須は `[Parameter(Mandatory = $true)]`、取りうる値が決まっているものは `[ValidateSet(...)]` で縛る
- **破壊的な操作は既定で実行しない。** 明示的に指定された場合のみ行う

```powershell
param(
    # 既定は一覧表示のみ。削除は明示的に指定された場合しか行わない
    [ValidateSet("None", "Local", "LocalAndRemote")]
    [string]$Delete = "None"
)
```

## コメント

- コメントは日本語で書く
- 自明な処理にコメントは書かない。「なぜそうしているか」が非自明な場合のみ書く
- 関数には `<# .SYNOPSIS #>` 形式のコメントベースヘルプを書く
- **実測を根拠に実装を決めた場合は、その実測内容と根拠をコメントに残す**（[.claude/factcheck.md](../.claude/factcheck.md) の方針。一次資料で裏が取れない場合は「出典なし」と明記する）

例：
```powershell
<#
.SYNOPSIS
    指定ポートの LISTEN が消えるまで待つ。解放されれば $true、タイムアウトすれば $false を返す。
#>
function Wait-PortReleased {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Port,

        [int] $TimeoutSeconds = 10
    )
    # プロセス停止後も待ち受けの解放には僅かな時間差があるため、1秒間隔でポーリングする
    ...
}
```

## 禁止事項

- BOM なしでの `.ps1` の作成（上記「ファイル形式」参照）
- `[Console]::OutputEncoding` を設定せずに日本語を出力すること（上記「標準出力の文字コード」参照）
- 対話を要求するコマンドの使用（`Read-Host` / `Get-Credential` / `pause` / `Out-GridView`）— 非対話で実行されるため応答が返らず、処理が止まる
- 確認プロンプトが出る破壊的コマンドを、意図を明示せずに使うこと（`Remove-Item` 等は `-Force` / `-ErrorAction SilentlyContinue` などを明示する）
- `main` への直接プッシュを行う処理の追加（[git-rules.md](git-rules.md) 参照。[git/push.ps1](../.claude/commands/git/push.ps1) はブランチ名を検査して拒否している）
- APIキー・シークレットのコードへの直書き（管理方法は [development-setup.md](development-setup.md) の「シークレットの設定」を参照）
