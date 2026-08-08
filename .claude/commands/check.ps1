# /check（プッシュ前の総点検）の実体。
# verify-docs.ps1（ドキュメントの参照先検査）→ verify-tests.ps1（テストクラスの欠落検査）
# → vs-tools.ps1（Release ビルド + テスト）の順に実行し、
# 途中で失敗したら後続を実行せず exit 1 する。
# check.md に2ステップを自然言語で並べる形にすると「1行目の失敗を無視して2行目が走る」問題が
# 起き得るため、スクリプト側で確実に止める。

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 呼び出し元のカレントディレクトリに依存させないため、呼び出す各スクリプトは
# $PSScriptRoot（このファイルと同じ .claude/commands/ ディレクトリ）を基準に解決する。

# ビルドを伴わず最も速いため、ドキュメントの検査を最初に実行する。
Write-Host "[1/3] ドキュメントの参照先を検査しています..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify-docs.ps1')
# $? と $LASTEXITCODE の両方を見て、呼び出した .ps1 の失敗を確実に止める。
if (-not $? -or $LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ドキュメントの参照先検査に失敗しました。上記のログを確認してください。" -ForegroundColor Red
    exit 1
}

Write-Host "[2/3] テストクラスの欠落を検査しています..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify-tests.ps1')
# $? と $LASTEXITCODE の両方を見て、呼び出した .ps1 の失敗を確実に止める。
if (-not $? -or $LASTEXITCODE -ne 0) {
    Write-Host "ERROR: テストクラスの欠落検査に失敗しました。上記のログを確認してください。" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] Release ビルド + テストを実行しています..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'vs-tools.ps1') -Task Test -Configuration Release
# $? と $LASTEXITCODE の両方を見て、呼び出した .ps1 の失敗を確実に止める。
if (-not $? -or $LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ビルドまたはテストに失敗しました。上記のログを確認してください。" -ForegroundColor Red
    exit 1
}

exit 0
