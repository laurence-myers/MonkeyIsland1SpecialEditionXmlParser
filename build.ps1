<#
.SYNOPSIS
    Builds a release of the Monkey Island SE Sprite Editor and packages it as MISESE_<date>.zip.

.DESCRIPTION
    One script for local releases and CI (.github/workflows/build.yml):

      1. The application version is today's date, yyyy.MM.dd.0 (override with -Version).
      2. Builds the in-game hot-reload tools (se-file-hook: mise-mreload.dll + se-inject.exe) with the
         Visual Studio C++ toolset (se-file-hook\build.ps1), unless -SkipHook is given, in which case
         the binaries already in se-file-hook\ are used.
      3. Builds the solution (Release) with the version stamped in, and runs the tests.
      4. Stages the editor's output folder, the mi1se-patch CLI and the hot-reload tools into
         artifacts\MISESE_<yyyy-MM-dd>\ and zips that folder into artifacts\MISESE_<yyyy-MM-dd>.zip,
         where the date is the application version.

.PARAMETER Version
    Version to stamp, yyyy.MM.dd.N. Defaults to today's date with .0.

.PARAMETER Configuration
    Build configuration; Release by default.

.PARAMETER SkipHook
    Do not build se-file-hook; package the se-inject.exe / mise-mreload.dll already there.

.PARAMETER SkipTests
    Do not run the unit tests.

.PARAMETER OutDir
    Where the staged folder and the zip go; artifacts\ under the repo by default.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -SkipHook -SkipTests
    .\build.ps1 -Version 2026.08.19.1
#>
[CmdletBinding()]
param(
    [ValidatePattern('^\d{4}\.\d{2}\.\d{2}\.\d+$')]
    [string] $Version = ((Get-Date -Format 'yyyy.MM.dd') + '.0'),
    [string] $Configuration = 'Release',
    [switch] $SkipHook,
    [switch] $SkipTests,
    [string] $OutDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$repoRoot = $PSScriptRoot
$solution = Join-Path $repoRoot 'MonkeyIslandSpecialEditionSpriteEditor\MonkeyIslandSpecialEditionSpriteEditor.sln'
$editorDir = Join-Path $repoRoot 'MonkeyIslandSpecialEditionSpriteEditor'
$editorOut = Join-Path $editorDir "bin\$Configuration\net481"
$patchCliOut = Join-Path $editorDir "PatchCli\bin\$Configuration\net481"
$hookDir = Join-Path $repoRoot 'se-file-hook'
$hookFiles = @('se-inject.exe', 'mise-mreload.dll')
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'artifacts' }

# MISESE_2026-08-19 from 2026.08.19.0: the zip's date is the application version's date.
$versionParts = $Version.Split('.')
$dateTag = '{0}-{1}-{2}' -f $versionParts[0], $versionParts[1], $versionParts[2]
$packageName = "MISESE_$dateTag"
$stageDir = Join-Path $OutDir $packageName
$zipPath = Join-Path $OutDir "$packageName.zip"

function Invoke-Checked {
    param([string] $Description, [scriptblock] $Command)
    Write-Host ""
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed (exit code $LASTEXITCODE)"
    }
}

# ---------------------------------------------------------------------------------------------------
# 1. the hot-reload tools (32-bit C, built with the Visual Studio C++ toolset) — see se-file-hook/README.md
# ---------------------------------------------------------------------------------------------------

if ($SkipHook) {
    Write-Host "==> Skipping the se-file-hook build; using the binaries already in $hookDir" -ForegroundColor Cyan
} else {
    Invoke-Checked 'Building the hot-reload tools' {
        & (Join-Path $hookDir 'build.ps1')   # throws on failure
    }
}

foreach ($file in $hookFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $hookDir $file))) {
        throw "Hot-reload tool missing: $hookDir\$file (run se-file-hook\build.ps1, or drop the prebuilt binaries there and pass -SkipHook)"
    }
}

# ---------------------------------------------------------------------------------------------------
# 2. the editor + CLI + tests, stamped with the version
# ---------------------------------------------------------------------------------------------------

# start from clean output folders so nothing stale gets packaged
foreach ($dir in @($editorOut, $patchCliOut)) {
    if (Test-Path -LiteralPath $dir) { Remove-Item -LiteralPath $dir -Recurse -Force }
}

Invoke-Checked "Building $Configuration, version $Version" {
    dotnet build $solution -c $Configuration -nologo "-p:Version=$Version"
}

if (-not $SkipTests) {
    Invoke-Checked 'Running the tests' {
        dotnet test $solution -c $Configuration --no-build -nologo
    }
}

# the built exe must carry the version the zip is named after
$editorExe = Join-Path $editorOut 'MonkeyIslandSpecialEditionSpriteEditor.exe'
$builtVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($editorExe).ProductVersion
if ($builtVersion -ne $Version) {
    throw "Built editor reports version '$builtVersion', expected '$Version'"
}

# ---------------------------------------------------------------------------------------------------
# 3. stage + zip
# ---------------------------------------------------------------------------------------------------

Write-Host ""
Write-Host "==> Staging $stageDir" -ForegroundColor Cyan
if (Test-Path -LiteralPath $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
New-Item -ItemType Directory -Path $stageDir | Out-Null

# the editor's output folder, as built
Copy-Item -Path (Join-Path $editorOut '*') -Destination $stageDir -Recurse

# the standalone walk-box patch CLI (its other outputs duplicate the editor's)
foreach ($file in @('mi1se-patch.exe', 'mi1se-patch.exe.config', 'mi1se-patch.pdb')) {
    $source = Join-Path $patchCliOut $file
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $stageDir }
}

# the hot-reload tools, next to the editor exe where HotReloadInjector looks first
foreach ($file in $hookFiles) {
    Copy-Item -LiteralPath (Join-Path $hookDir $file) -Destination $stageDir
}
Copy-Item -LiteralPath (Join-Path $hookDir 'README.md') -Destination (Join-Path $stageDir 'HotReload-Readme.md')

Write-Host "==> Zipping to $zipPath" -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Written entry by entry (not ZipFile::CreateFromDirectory): on Windows PowerShell that API names the
# entries with backslashes, which non-Windows unzippers turn into literal file names.
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $stageRoot = (Resolve-Path -LiteralPath $stageDir).Path.TrimEnd('\') + '\'
    foreach ($file in Get-ChildItem -LiteralPath $stageDir -Recurse -File) {
        $entryName = $packageName + '/' + $file.FullName.Substring($stageRoot.Length).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $zip.Dispose()
}

Write-Host ""
Write-Host "Done: $zipPath (version $Version)" -ForegroundColor Green
Get-ChildItem -LiteralPath $stageDir | Select-Object -ExpandProperty Name | ForEach-Object { Write-Host "  $_" }
