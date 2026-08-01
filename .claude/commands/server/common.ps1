# 開発サーバー（IIS Express）操作の共通処理
# start.ps1 / stop.ps1 から dot-source して使用する。このファイル単体では何も実行しない。

# リポジトリのルートディレクトリ
# このファイルは .claude/commands/server/ に置かれているため、3階層上がリポジトリルートになる。
# 呼び出し元のカレントディレクトリに依存させないため、$PSScriptRoot を基準に解決する。
$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

<#
.SYNOPSIS
    このプロジェクト（Web.config のあるディレクトリ）の物理パスを返す。
#>
function Get-ProjectPhysicalPath {
    $path = Join-Path $RepoRoot "src\LocalRagApplication"
    if (Test-Path -LiteralPath $path) {
        # 大文字小文字や相対表記を正規化した絶対パスにそろえる（コマンドラインとの比較に使うため）
        return (Resolve-Path -LiteralPath $path).ProviderPath
    }
    return $path
}

<#
.SYNOPSIS
    このプロジェクトを serve している iisexpress.exe のプロセスを返す（無ければ何も返さない）。
#>
function Get-ProjectIisExpressProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectPath
    )

    # ポートの所有者からはサーバープロセスを特定できない。
    # 本環境での実測では、Get-NetTCPConnection -LocalPort 58398 -State Listen の OwningProcess は
    # iisexpress.exe（PID 1952）ではなく 4（System）を返した。
    # そのため、ポートではなくプロセスのコマンドラインからこのプロジェクトのサーバーを特定する。
    # 出典: 本環境での実測結果（PowerShell 5.1.26100.8972）
    # ※ 「IIS Express の待ち受けは http.sys が担うためソケット所有者が System になる」という説明は
    #    一次資料で裏付けを確認できていない（出典なし）。上記の実測結果のみを根拠としている。

    # Windows PowerShell 5.1 の Get-Process は CommandLine プロパティを公開しないため、
    # コマンドラインの取得には WMI クラス Win32_Process（CommandLine / ProcessId）を使用する。
    # 出典: 本環境で Get-CimInstance -ClassName Win32_Process の Get-Member により
    #       ProcessId（uint32）・CommandLine（string）の存在を確認済み
    $candidates = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'iisexpress.exe'" -ErrorAction SilentlyContinue

    foreach ($candidate in $candidates) {
        $commandLine = $candidate.CommandLine
        if ([string]::IsNullOrEmpty($commandLine)) {
            # 権限不足などでコマンドラインを取得できないプロセスは、判別できないため対象外にする
            continue
        }

        # 他プロジェクトの IIS Express を巻き添えで停止しないよう、コマンドラインに
        # このプロジェクトの物理パスを含むものだけを対象にする。
        # パス区切りの \ は正規表現・ワイルドカードのメタ文字と衝突するため、-match / -like ではなく
        # String.IndexOf による大文字小文字を区別しない部分一致で判定する。
        $index = $commandLine.IndexOf($ProjectPath, [System.StringComparison]::OrdinalIgnoreCase)
        if ($index -lt 0) {
            continue
        }

        # 前方一致による誤検出（例: ...\LocalRagApplicationOther）を避けるため、
        # 一致直後の文字がパス区切り・引用符・空白・文字列終端のいずれかであることを確認する
        $nextIndex = $index + $ProjectPath.Length
        if ($nextIndex -lt $commandLine.Length) {
            $nextChar = $commandLine[$nextIndex]
            if ($nextChar -ne '\' -and $nextChar -ne '/' -and $nextChar -ne '"' -and $nextChar -ne ' ') {
                continue
            }
        }

        $candidate
    }
}

<#
.SYNOPSIS
    このプロジェクトの iisexpress.exe を停止し、停止したプロセスIDを返す（無ければ何も返さない）。
#>
function Stop-ProjectIisExpress {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectPath
    )

    foreach ($process in @(Get-ProjectIisExpressProcess -ProjectPath $ProjectPath)) {
        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
            $process.ProcessId
        } catch {
            # 停止に失敗した場合でも他のプロセスの停止は続行する（呼び出し元はポート解放待ちで最終判定する）
            Write-Host "WARNING: iisexpress.exe (PID: $($process.ProcessId)) の停止に失敗しました: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

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
    for ($i = 0; $i -lt $TimeoutSeconds; $i++) {
        if (-not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)) {
            return $true
        }
        Start-Sleep -Seconds 1
    }

    return (-not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue))
}
