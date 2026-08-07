# 初回セットアップスクリプト（.NET Framework 4.8 / ASP.NET MVC 5、非SDK形式・packages.config 方式）

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=== セットアップ ===" -ForegroundColor Cyan

# [1/2] .NET Framework 4.8 のインストール状況を確認する
# .NET Framework 4.5 以降のバージョンは、レジストリ HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full の
# Release（REG_DWORD）値で判定する。4.8 の最小値は 528040（Windows 10 May 2019 Update / November 2019 Update の場合。
# OS により値は異なるが、いずれの環境でも 528040 以上であれば 4.8 以降と判定できる）。
# 出典: https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
Write-Host "[1/2] .NET Framework 4.8 のインストール状況を確認しています..."
$ndpKey = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
$release = Get-ItemPropertyValue -LiteralPath $ndpKey -Name Release -ErrorAction SilentlyContinue
if (-not $release -or $release -lt 528040) {
    Write-Host "ERROR: .NET Framework 4.8 が見つかりません。Developer Pack をインストールしてください: https://dotnet.microsoft.com/download/dotnet-framework/net48" -ForegroundColor Red
    exit 1
}
Write-Host ".NET Framework 4.8 以降を検出しました（Release: $release）。" -ForegroundColor Green

# [2/2] NuGet パッケージを復元する
# 非SDK形式プロジェクト（packages.config 方式）のため、dotnet restore ではなく nuget restore を使用する
# classic な nuget.exe CLI が新形式のソリューションファイル（.slnx）を直接解釈できるかは未確認のため、
# プロジェクト単位で個別に復元する（docs/development-setup.md・.github/workflows/ci.yml と同じ方式）
Write-Host "[2/2] NuGet パッケージを復元しています..."
if (-not (Get-Command nuget -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: nuget コマンドが見つかりません。NuGet CLI をインストールしてください: https://www.nuget.org/downloads" -ForegroundColor Red
    exit 1
}
nuget restore src\LocalRagApplication\LocalRagApplication.csproj -SolutionDirectory .
if (-not $?) {
    Write-Host "ERROR: nuget restore に失敗しました（LocalRagApplication.csproj）。" -ForegroundColor Red
    exit 1
}
nuget restore tests\LocalRagApplication.Tests\LocalRagApplication.Tests.csproj -SolutionDirectory .
if (-not $?) {
    Write-Host "ERROR: nuget restore に失敗しました（LocalRagApplication.Tests.csproj）。" -ForegroundColor Red
    exit 1
}

Write-Host "=== セットアップ完了 ===" -ForegroundColor Cyan
Write-Host "次のステップ:"
Write-Host "  1. git リポジトリの初期化・hooks 登録: /git:init コマンドを実行してください"
Write-Host "  2. シークレットの設定方法: docs/development-setup.md を参照してください"
Write-Host "  3. サーバー起動: /server:start コマンドを実行してください"
Write-Host "  4. ブラウザで開く: http://localhost:58398/（/server:start で起動した場合）"
