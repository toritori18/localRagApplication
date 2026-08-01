# 開発サーバー（IIS Express）を停止する

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# プロセス特定・ポート解放待ちの共通処理を読み込む
# dot-source のパスは呼び出し元のカレントディレクトリに依存しないよう $PSScriptRoot を基準に解決する
. (Join-Path $PSScriptRoot "common.ps1")

# 開発サーバーのポート（LocalRagApplication.csproj の DevelopmentServerPort）
$port = 58398

# プロジェクトの物理パス（Web.config のあるディレクトリ）
$projectPath = Get-ProjectPhysicalPath

# このプロジェクトを serve 中の開発サーバーを停止する
# （ポート所有者ではなくコマンドラインでプロセスを特定する理由は common.ps1 のコメントを参照）
$stoppedIds = @(Stop-ProjectIisExpress -ProjectPath $projectPath)
if ($stoppedIds.Count -gt 0) {
    Write-Host "Stopped dev server (PID: $($stoppedIds -join ', '))" -ForegroundColor Yellow

    # プロセス停止後もポートの解放には時間差があるため、LISTEN が消えるまで待つ（最大10秒）
    if (-not (Wait-PortReleased -Port $port -TimeoutSeconds 10)) {
        Write-Host "WARNING: ポート $port が10秒以内に解放されませんでした。別のプロセスが使用している可能性があります。" -ForegroundColor Yellow
    }
} else {
    Write-Host "No server running on port $port" -ForegroundColor Gray
}
