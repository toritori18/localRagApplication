# src/LocalRagApplication/ 配下の .cs に対応するテストクラスが
# tests/LocalRagApplication.Tests/ 配下に存在するか、および
# tests/LocalRagApplication.Tests/ 配下の .cs が csproj の <Compile Include> に
# 登録されているかを検査する。
# /check（check.ps1）と CI（.github/workflows/ci.yml）の両方から呼ばれる。
#
# Visual Studio には依存しない（vswhere によるパス解決は行わない。vs-tools.ps1 とは責務が異なる）。
# ローカルの Windows PowerShell 5.1 と CI の pwsh（PowerShell 7）の両方から
# 同じ実体を呼び出すため、両方のパーサーが受け付ける構文のみを使用する。
#
# --- この検査の限界（code-reviewer エージェントのレビューで補う想定） ---
# - テストクラスファイルが「ディスク上に存在し、csproj に登録されているか」までしか見ない。
#   中身が空のテストクラスを置いても通ってしまう
# - 既存のテスト対象クラスへの public メソッド追加（テストクラス自体は既に存在する）は検出できない
# - src/LocalRagApplication/ 側の .cs の csproj 登録漏れは検査しない（未登録のクラスは参照された
#   瞬間に CS0246 でビルドが失敗するため、テスト側と異なり静かに壊れない）
# - .cshtml（<Content Include>）の登録漏れは検査しない（実測で実害なしを確認済み。IIS Express は
#   物理ファイルシステムを見てビューを解決するため、csproj への登録有無は経路に入らない）
# - <Compile Include> の照合は正規表現による簡易パースであり、Include 以外の属性が並ぶ形
#   （<Compile Include="x.cs" Condition="..." />）・属性値がシングルクォートの形・区切りが /
#   のパス（MSBuild は受け付ける）は、登録済みであっても「未登録」として誤検出される。
#   いずれもエラーが出るだけの安全側の誤検出である

# Claude Code は出力を UTF-8 として読むため、stdout を UTF-8 に固定する（文字化け防止）
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# このファイルは .claude/commands/ に置かれているため、2階層上がリポジトリルートになる。
# 呼び出し元のカレントディレクトリに依存させないため、$PSScriptRoot を基準に解決する。
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$SrcRoot = Join-Path $RepoRoot 'src\LocalRagApplication'
$TestsRoot = Join-Path $RepoRoot 'tests\LocalRagApplication.Tests'
$NoTestRequiredPath = Join-Path $TestsRoot 'no-test-required.md'
$TestCsprojPath = Join-Path $TestsRoot 'LocalRagApplication.Tests.csproj'

if (-not (Test-Path -LiteralPath $SrcRoot)) {
    Write-Host "ERROR: src ディレクトリが見つかりません（$SrcRoot）。" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $NoTestRequiredPath)) {
    Write-Host "ERROR: 除外宣言ファイルが見つかりません（$NoTestRequiredPath）。tests/LocalRagApplication.Tests/no-test-required.md を作成してください。" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $TestCsprojPath -PathType Leaf)) {
    Write-Host "ERROR: テストプロジェクトの csproj が見つかりません（$TestCsprojPath）。意図せず削除・移動していないか確認し、git status でリポジトリの状態を確認してください。" -ForegroundColor Red
    exit 1
}

<#
.SYNOPSIS
    .cs ファイルの中身が「インターフェース定義のみ」かどうかを判定する。
    docs/csharp-contributing.md の「テストを書く基準」の対象外カテゴリの1つに対応する。
.DESCRIPTION
    public interface を含み、かつ「インターフェース以外の public な型宣言」（class / struct / enum）を
    1つも含まない場合に true を返す。class / struct の判定は sealed / partial / static / abstract の
    任意の組み合わせ・並び順（例: `public sealed class`、`public sealed partial class`）に対応する
    1本の正規表現で行う。これにより、同一ファイルに `public interface` と
    `public sealed class Foo : IFoo` が同居するようなケースを、
    誤って「インターフェース定義のみ」と判定しない。
#>
function Test-IsInterfaceOnlyFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Content
    )

    if (-not ($Content -match 'public interface')) {
        return $false
    }

    $hasNonInterfaceType = $Content -match 'public\s+(?:(?:sealed|partial|static|abstract)\s+)*(?:class|struct|enum)\b'

    return -not $hasNonInterfaceType
}

<#
.SYNOPSIS
    src からの相対パス（/ 区切り）を受け取り、docs/csharp-contributing.md の
    「テストを書く基準」の対象外カテゴリ（インターフェース定義のみのファイルを除く）に
    自動的に一致するかどうかを判定する。
#>
function Test-IsAutoExcludedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    if ($RelativePath -eq 'Properties/AssemblyInfo.cs') {
        return $true
    }
    if ($RelativePath -like 'App_Start/*') {
        return $true
    }
    if ($RelativePath -eq 'Global.asax.cs') {
        return $true
    }
    if ($RelativePath -like 'Models/*') {
        return $true
    }

    return $false
}

Write-Host "テストクラスの欠落を検査しています..." -ForegroundColor Cyan

# no-test-required.md をパースする。
# 書式: - `<src からの相対パス>` — <理由>
# `#` で始まる行（見出し）と空行は無視する。
$declaredReasons = @{}
$emptyReasonPaths = New-Object System.Collections.Generic.List[string]

#
# Get-Content には明示的に -Encoding UTF8 を指定する。BOM なしの UTF-8 ファイルを
# 指定なしで読むと、Windows PowerShell 5.1 は既定の ANSI コードページ（日本語環境では
# CP932）として読んでしまい、日本語部分の解釈がずれて改行が消えることがある
# （docs/powershell-contributing.md の「ファイル形式」参照。本環境での実測でも、
# -Encoding UTF8 を指定しない場合に no-test-required.md の行数が 27 行から 16 行に
# 減る＝一部の行が結合して消えることを確認した）。
foreach ($line in (Get-Content -LiteralPath $NoTestRequiredPath -Encoding UTF8)) {
    $trimmedLine = $line.Trim()
    if ($trimmedLine -eq '' -or $trimmedLine.StartsWith('#')) {
        continue
    }
    # パスは常に .cs で終わる実ソースファイルパスであることを要求する。
    # このファイルの説明文中にもバッククォート付きの断片（例:
    # `src/LocalRagApplication/`）が現れるため、.cs 終端を必須にすることで
    # 説明文をエントリと誤認しないようにする。
    if (-not ($trimmedLine -match '^-\s*`([^`]+\.cs)`\s*(.*)$')) {
        continue
    }

    $declaredPath = $Matches[1]
    $reasonPart = $Matches[2]
    $reason = ($reasonPart -replace '^—\s*', '').Trim()

    $declaredReasons[$declaredPath] = $reason
    if ([string]::IsNullOrWhiteSpace($reason)) {
        $emptyReasonPaths.Add($declaredPath)
    }
}

# 逆方向の検査: 除外宣言に載っているのに src 側に実在しないパスがないか（陳腐化した除外宣言）。
$staleExclusions = New-Object System.Collections.Generic.List[string]
foreach ($declaredPath in $declaredReasons.Keys) {
    $declaredFullPath = Join-Path $SrcRoot ($declaredPath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $declaredFullPath -PathType Leaf)) {
        $staleExclusions.Add($declaredPath)
    }
}

$csFiles = Get-ChildItem -LiteralPath $SrcRoot -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

$targetCount = 0
$autoExcludedCount = 0
$declaredExcludedCount = 0
$missingTests = New-Object System.Collections.Generic.List[string]

foreach ($file in $csFiles) {
    $relativePath = $file.FullName.Substring($SrcRoot.Length + 1) -replace '\\', '/'

    if (Test-IsAutoExcludedPath -RelativePath $relativePath) {
        $autoExcludedCount++
        continue
    }

    # BOM なしの .cs ファイルを CP932 として誤読しないよう -Encoding UTF8 を明示する
    # （上記の no-test-required.md 読み込みと同じ理由）
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    if (Test-IsInterfaceOnlyFile -Content $content) {
        $autoExcludedCount++
        continue
    }

    $targetCount++

    $dirRelative = Split-Path -Parent $relativePath
    $testFileName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name) + 'Test.cs'
    if ([string]::IsNullOrEmpty($dirRelative)) {
        $testRelativePath = $testFileName
    } else {
        $testRelativePath = "$dirRelative/$testFileName"
    }
    $testFullPath = Join-Path $TestsRoot ($testRelativePath -replace '/', '\')

    if (Test-Path -LiteralPath $testFullPath -PathType Leaf) {
        continue
    }

    if ($declaredReasons.ContainsKey($relativePath)) {
        $declaredExcludedCount++
        continue
    }

    $missingTests.Add($relativePath)
}

# --- テストプロジェクトの csproj 登録漏れ検査 ---
# tests/LocalRagApplication.Tests/ 配下の .cs ファイルを csproj に登録し忘れると、
# コンパイル対象にならないため、その [TestClass] はテスト実行の対象に現れず、
# テストが1件も実行されないまま exit 0 になる
# （実測。Assert.Fail のみのテストを csproj 未登録のまま置いて /check を実行した結果、
# 「OK: 検査対象 18 件」「テストの合計数: 121 / 成功: 121」で完全に静かに素通りした）。
#
# src/LocalRagApplication/ 側の .cs は検査しない。未登録のクラスは参照された瞬間に
# CS0246 でビルドが失敗するため、テスト側と異なり静かに壊れない。
#
# .cshtml（<Content Include>）の登録漏れも検査しない。実測で実害なしを確認済み
# （<Content Include="Views\Ask\Index.cshtml" /> を削除して GET /Ask した結果は
# HTTP 200・日本語正常・3788文字だった。IIS Express はプロジェクトフォルダをそのまま
# 配信し、Razor のビュー解決は物理ファイルシステムを見るため csproj は経路に入らない）。
#
# 逆方向（csproj に登録されているのに実ファイルが無い）は検査しない。存在しないパスを
# <Compile Include> に足してビルドすると CSC : error CS2001 でビルド自体が失敗する
# （exit 1）ため、この検査と役割が重複する。
Write-Host "テストプロジェクトの csproj 登録漏れを検査しています..." -ForegroundColor Cyan

$testCsFiles = Get-ChildItem -LiteralPath $TestsRoot -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

# BOM ありの csproj でも -Encoding UTF8 を明示する（他の箇所と同様、指定漏れによる
# ANSI（CP932）誤読を防ぐ習慣を統一する）
$testCsprojContent = Get-Content -LiteralPath $TestCsprojPath -Raw -Encoding UTF8

# コメントアウトされた <Compile Include> を登録済みと誤認しないよう、XML コメントを除去してから照合する
$testCsprojContent = [regex]::Replace($testCsprojContent, '(?s)<!--.*?-->', '')

# <Compile Include="..." /> の自己完結形と <Compile Include="...">...</Compile> の
# 開始タグ形の両方に一致させる（将来 <DependentUpon> 等の子要素が付く可能性があるため）。
$compileMatches = [regex]::Matches($testCsprojContent, '<Compile\s+Include\s*=\s*"([^"]+)"\s*/?>')
$registeredCompilePaths = New-Object System.Collections.Generic.List[string]
foreach ($compileMatch in $compileMatches) {
    $registeredCompilePaths.Add($compileMatch.Groups[1].Value)
}

# -notcontains は既定で大文字小文字を区別しない。Windows のファイルシステムに合わせて
# ここでも区別しない比較にする（区切り文字はどちらも \ のため、正規化は不要）。
$unregisteredTests = New-Object System.Collections.Generic.List[string]
foreach ($file in $testCsFiles) {
    $testRelativePath = $file.FullName.Substring($TestsRoot.Length + 1)
    if ($registeredCompilePaths -notcontains $testRelativePath) {
        $unregisteredTests.Add($testRelativePath)
    }
}

$hasError = $false

if ($missingTests.Count -gt 0) {
    $hasError = $true
    Write-Host ""
    Write-Host "ERROR: 以下の $($missingTests.Count) 件はテストクラスがなく、除外宣言もありません:" -ForegroundColor Red
    foreach ($path in ($missingTests | Sort-Object)) {
        Write-Host "  - $path" -ForegroundColor Red
    }
    Write-Host "  対応方法: tests/LocalRagApplication.Tests/<相対パス>/<クラス名>Test.cs を追加するか、" -ForegroundColor Red
    Write-Host "  テストを書かないと判断した場合は tests/LocalRagApplication.Tests/no-test-required.md に理由付きで追記してください。" -ForegroundColor Red
}

if ($staleExclusions.Count -gt 0) {
    $hasError = $true
    Write-Host ""
    Write-Host "ERROR: 以下の $($staleExclusions.Count) 件は no-test-required.md に登録されていますが、src 側に実体がありません（陳腐化した除外宣言です）:" -ForegroundColor Red
    foreach ($path in ($staleExclusions | Sort-Object)) {
        Write-Host "  - $path" -ForegroundColor Red
    }
    Write-Host "  対応方法: tests/LocalRagApplication.Tests/no-test-required.md から該当行を削除してください。" -ForegroundColor Red
}

if ($emptyReasonPaths.Count -gt 0) {
    $hasError = $true
    Write-Host ""
    Write-Host "ERROR: 以下の $($emptyReasonPaths.Count) 件は no-test-required.md の理由が空です:" -ForegroundColor Red
    foreach ($path in ($emptyReasonPaths | Sort-Object)) {
        Write-Host "  - $path" -ForegroundColor Red
    }
    Write-Host "  対応方法: tests/LocalRagApplication.Tests/no-test-required.md にテストを書かない理由を追記してください。" -ForegroundColor Red
}

if ($unregisteredTests.Count -gt 0) {
    $hasError = $true
    Write-Host ""
    Write-Host "ERROR: 以下の $($unregisteredTests.Count) 件は tests 配下に .cs ファイルが存在しますが、csproj の <Compile Include> に登録されていません:" -ForegroundColor Red
    foreach ($path in ($unregisteredTests | Sort-Object)) {
        Write-Host "  - $path" -ForegroundColor Red
    }
    Write-Host "  対応方法: tests/LocalRagApplication.Tests/LocalRagApplication.Tests.csproj の <ItemGroup> に <Compile Include=""<相対パス>"" /> を追記してください。" -ForegroundColor Red
}

if ($hasError) {
    exit 1
}

Write-Host ""
Write-Host "OK: 検査対象 $targetCount 件（自動除外 $autoExcludedCount 件、除外宣言 $declaredExcludedCount 件）。すべてのクラスにテストクラスが存在するか、除外が宣言されています。テストプロジェクトの .cs $($testCsFiles.Count) 件はすべて csproj に登録されています。" -ForegroundColor Green
exit 0
