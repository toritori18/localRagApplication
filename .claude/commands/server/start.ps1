# 開発サーバーを起動する（既存プロセスがあれば停止してから、IIS Express をバックグラウンドで起動する）

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 開発サーバーのポート（LocalRagApplication.csproj の DevelopmentServerPort）
$port = 58398

# ポート58398で稼働中のサーバーがあれば停止
$conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($conn) {
    $procId = $conn.OwningProcess
    Stop-Process -Id $procId -Force
    Write-Host "Stopped existing dev server (PID: $procId)" -ForegroundColor Yellow
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

# プロジェクトの物理パス（Web.config のあるディレクトリ）
$projectPath = Join-Path (Get-Location) "src\LocalRagApplication"
if (-not (Test-Path (Join-Path $projectPath "Web.config"))) {
    Write-Host "ERROR: Web.config が見つかりません: $projectPath" -ForegroundColor Red
    exit 1
}

# バックグラウンドで起動する（フォアグラウンド実行だと呼び出し元のシェルがブロックされるため）
# ログは .claude/dev-server.log に出力する（.gitignore 済み）
# iisexpress.exe の /path・/port 引数は公式ドキュメントで確認済み（"iisexpress /path:c:\myapp\ /port:80"）
# /systray:false はバックグラウンド起動時にシステムトレイアイコンを表示させないための指定
$log = ".claude/dev-server.log"
$proc = Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c `"$iisExpress`" /path:`"$projectPath`" /port:$port /systray:false > `"$log`" 2>&1" `
    -WorkingDirectory (Get-Location) -WindowStyle Hidden -PassThru

# 起動確認: 最大30秒、ポート58398が LISTEN になるのを待つ
$started = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        $started = $true
        break
    }
    # プロセスが即死した場合は待たずに打ち切る
    if ($proc.HasExited) {
        break
    }
}

if ($started) {
    Write-Host "Dev server started: http://localhost:$port/ (log: $log)" -ForegroundColor Green
} else {
    Write-Host "ERROR: Dev server did not start. Last log lines:" -ForegroundColor Red
    if (Test-Path $log) {
        Get-Content $log -Tail 20
    }
    exit 1
}
