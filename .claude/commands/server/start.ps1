# 開発サーバーを起動する（既存プロセスがあれば停止してから、IIS Express をバックグラウンドで起動する）

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# プロセス特定・ポート解放待ちの共通処理を読み込む
# dot-source のパスは呼び出し元のカレントディレクトリに依存しないよう $PSScriptRoot を基準に解決する
. (Join-Path $PSScriptRoot "common.ps1")

# 開発サーバーのポート（LocalRagApplication.csproj の DevelopmentServerPort）
$port = 58398

# プロジェクトの物理パス（Web.config のあるディレクトリ）
$projectPath = Get-ProjectPhysicalPath
if (-not (Test-Path (Join-Path $projectPath "Web.config"))) {
    Write-Host "ERROR: Web.config が見つかりません: $projectPath" -ForegroundColor Red
    exit 1
}

# このプロジェクトを serve 中の開発サーバーがあれば停止する
# （ポート所有者ではなくコマンドラインでプロセスを特定する理由は common.ps1 のコメントを参照）
$stoppedIds = @(Stop-ProjectIisExpress -ProjectPath $projectPath)
if ($stoppedIds.Count -gt 0) {
    Write-Host "Stopped existing dev server (PID: $($stoppedIds -join ', '))" -ForegroundColor Yellow

    # ポートが解放される前に新しいインスタンスを起動すると、後段の起動確認が古い待ち受けを拾って
    # 「起動成功」と誤判定するため、解放を待ってから起動する（最大10秒）
    if (-not (Wait-PortReleased -Port $port -TimeoutSeconds 10)) {
        Write-Host "ERROR: ポート $port が10秒以内に解放されませんでした。ポートを使用しているプロセスを手動で停止してから再実行してください。" -ForegroundColor Red
        exit 1
    }
}

# iisexpress.exe のパスを解決する（インストール先はOSのbit数により異なる）
# 出典: https://learn.microsoft.com/en-us/iis/extensions/using-iis-express/running-iis-express-from-the-command-line
$iisExpressCandidates = @(
    "$env:ProgramFiles\IIS Express\iisexpress.exe",
    "${env:ProgramFiles(x86)}\IIS Express\iisexpress.exe"
)
$iisExpress = $iisExpressCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iisExpress) {
    Write-Host "ERROR: iisexpress.exe が見つかりません。Visual Studio 2022 の「ASP.NET と Web開発」ワークロードをインストールしてください（docs/development-setup.md 参照）。" -ForegroundColor Red
    exit 1
}

# ログの出力先（.gitignore の .claude/dev-server*.log で除外済み）
# Start-Process のリダイレクト先は呼び出し元のカレントディレクトリを基準に解決されるため、
# common.ps1 が $PSScriptRoot から求めた $RepoRoot を使って絶対パスで指定する。
# 標準出力と標準エラーに同一パスを指定するとエラーになるため、ファイルを分ける
# （出典: 本環境での実測結果）
$log = Join-Path $RepoRoot ".claude\dev-server.log"
$errorLog = Join-Path $RepoRoot ".claude\dev-server.err.log"

# 前回実行分のログが残っていると起動失敗時の tail で古い内容を読んでしまうため、事前に削除する
Remove-Item -LiteralPath $log, $errorLog -Force -ErrorAction SilentlyContinue

# バックグラウンドで起動する（フォアグラウンド実行だと呼び出し元のシェルがブロックされるため）
# iisexpress.exe の /path・/port 引数は公式ドキュメントで確認済み（"iisexpress /path:c:\myapp\ /port:80"）
# /systray:false はバックグラウンド起動時にシステムトレイアイコンを表示させないための指定
#
# cmd.exe 経由（cmd /c "iisexpress.exe" ... > log 2>&1）では、本環境で cmd.exe が即座に終了コード0で終了し、
# iisexpress.exe が1つも起動せず、リダイレクト先のログすら生成されない事象が再現した（入れ子の引用符が
# cmd 側で正しく解釈されないため）。そのため cmd.exe を介さず iisexpress.exe を直接起動し、
# ログは Start-Process のリダイレクトで取得する。（出典: 本環境での実測結果）
$proc = Start-Process -FilePath $iisExpress `
    -ArgumentList "/path:`"$projectPath`"", "/port:$port", "/systray:false" `
    -RedirectStandardOutput $log -RedirectStandardError $errorLog `
    -WindowStyle Hidden -PassThru

# 起動確認: 最大30回（約1秒間隔）待つ
# ポートが LISTEN かどうかだけでは、別プロセスの待ち受けを拾って誤判定するおそれがあるため、
# このプロジェクトの iisexpress.exe が実在することも条件にする。
# 本環境の実測では起動完了まで10秒以内のため30回で十分。なお Get-NetTCPConnection の呼び出しに
# 1回あたり 0.3〜0.5秒かかるため、実時間は30秒より長くなるが許容する。
$started = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $isListening = [bool](Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
    $isRunning = @(Get-ProjectIisExpressProcess -ProjectPath $projectPath).Count -gt 0
    if ($isListening -and $isRunning) {
        $started = $true
        break
    }
    # $proc は iisexpress.exe そのものなので、即死した場合は待たずに打ち切れる
    if ($proc.HasExited) {
        break
    }
}

if ($started) {
    Write-Host "Dev server started: http://localhost:$port/ (log: $log)" -ForegroundColor Green
} else {
    Write-Host "ERROR: Dev server did not start. Last log lines:" -ForegroundColor Red
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 20
    }
    # 起動失敗の原因は標準エラー側にのみ出ることがあるため、内容があればそちらも表示する
    if ((Test-Path -LiteralPath $errorLog) -and (Get-Item -LiteralPath $errorLog).Length -gt 0) {
        Write-Host "--- stderr ($errorLog) ---" -ForegroundColor Red
        Get-Content -LiteralPath $errorLog -Tail 20
    }
    exit 1
}
