# 開発サーバーのポート（LocalRagApplication.csproj の DevelopmentServerPort）
$port = 58398

$conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($conn) {
    $procId = $conn.OwningProcess
    Stop-Process -Id $procId -Force
    Write-Host "Stopped dev server (PID: $procId)" -ForegroundColor Yellow
} else {
    Write-Host "No server running on port $port" -ForegroundColor Gray
}
