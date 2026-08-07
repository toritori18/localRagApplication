# msbuild / vstest.console.exe は Visual Studio Installer 配下にのみ存在し PATH に登録されていないため、
# vswhere.exe で実体のパスを解決してからビルド・テストを実行する。
# /typecheck・/build・/test・/check の実体はすべてこのスクリプトに集約している。

param(
    [ValidateSet("Build", "Test")]
    [string]$Task = "Build",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# このファイルは .claude/commands/ に置かれているため、2階層上がリポジトリルートになる。
# 呼び出し元のカレントディレクトリに依存させないため、$PSScriptRoot を基準に解決する。
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$solutionPath = Join-Path $RepoRoot "LocalRagApplication.slnx"

# vswhere.exe は Visual Studio Installer に同梱される。
# 出典: 本環境での実測（このパスに存在することを確認済み）
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) {
    Write-Host "ERROR: vswhere.exe が見つかりません（$vswhere）。Visual Studio 2022 の「ASP.NET と Web開発」ワークロードをインストールしてください（docs/development-setup.md 参照）。" -ForegroundColor Red
    exit 1
}

Write-Host "MSBuild.exe のパスを解決しています..." -ForegroundColor Cyan
# -find のパターンは Bin 直下の MSBuild.exe にのみ一致するため、amd64 版は戻り値に含まれない。
# 出典: 本環境での実測（インスタンス内に MSBuild.exe は Bin\MSBuild.exe と Bin\amd64\MSBuild.exe の
#       2つ実在するが、この呼び出しの戻り値は Bin\MSBuild.exe の1件のみだった）
# VS のレイアウトやパターンが変わって複数件返った場合に備え、先頭のみを採用する。
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1

# パイプで Select-Object を挟んでいるため $? は vswhere ではなく Select-Object の結果になる。
# そのため成否は戻り値の中身（空でないこと・実在すること）で判定する。
if ([string]::IsNullOrEmpty($msbuild) -or -not (Test-Path -LiteralPath $msbuild)) {
    Write-Host "ERROR: MSBuild.exe を解決できませんでした。Visual Studio Installer で「ASP.NET と Web開発」ワークロードをインストールしてください（docs/development-setup.md 参照）。" -ForegroundColor Red
    exit 1
}

if ($Task -eq "Test") {
    Write-Host "[1/2] ビルドしています（Configuration=$Configuration）..." -ForegroundColor Cyan
} else {
    Write-Host "ビルドしています（Configuration=$Configuration）..." -ForegroundColor Cyan
}
# .github/workflows/ci.yml は /p:Platform="Any CPU" を付けているが、ここでは付けない。
# 従来 .claude/commands/*.md が素の msbuild を実行していた挙動を変えないため。
# 出典: 本環境での実測（付けずに Debug / Release とも exit 0 でビルドが成功することを確認済み）
& $msbuild $solutionPath /p:Configuration=$Configuration
if (-not $?) {
    Write-Host "ERROR: ビルドに失敗しました（Configuration=$Configuration）。上記のログを確認してください。" -ForegroundColor Red
    exit 1
}

if ($Task -eq "Build") {
    exit 0
}

Write-Host "[2/2] テストを実行しています（Configuration=$Configuration）..." -ForegroundColor Cyan

# vstest.console.exe は vswhere から直接解決できないため、インストールパスから組み立てる
# （.github/workflows/ci.yml と同じ方式。出典: 本環境での実測でパスの存在を確認済み）
# こちらは -find ではなく -property のため、-latest により1インスタンス分の1件が返る（実測）。
# MSBuild.exe の解決とは理由が異なるが、同様に防御的に先頭のみを採用する。
$installationPath = & $vswhere -latest -products * -property installationPath | Select-Object -First 1
if ([string]::IsNullOrEmpty($installationPath)) {
    Write-Host "ERROR: Visual Studio のインストールパスを取得できませんでした。" -ForegroundColor Red
    exit 1
}

$vstest = Join-Path $installationPath "Common7\IDE\Extensions\TestPlatform\vstest.console.exe"
if (-not (Test-Path -LiteralPath $vstest)) {
    Write-Host "ERROR: vstest.console.exe が見つかりません（$vstest）。Visual Studio Installer で「ASP.NET と Web開発」ワークロードをインストールしてください（docs/development-setup.md 参照）。" -ForegroundColor Red
    exit 1
}

$testDll = Join-Path $RepoRoot "tests\LocalRagApplication.Tests\bin\$Configuration\LocalRagApplication.Tests.dll"
if (-not (Test-Path -LiteralPath $testDll)) {
    Write-Host "ERROR: テスト DLL が見つかりません（$testDll）。ビルドが正しく完了しているか確認してください。" -ForegroundColor Red
    exit 1
}

& $vstest $testDll
if (-not $?) {
    Write-Host "ERROR: テストが失敗しました。上記のログを確認してください。" -ForegroundColor Red
    exit 1
}

exit 0
