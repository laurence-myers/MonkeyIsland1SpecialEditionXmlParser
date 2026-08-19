<#
.SYNOPSIS
    Builds the MI1SE in-place hot-reload tools (32-bit, to match MISE.exe) with the Visual Studio C++ toolset:
      se-inject.exe    - CreateRemoteThread(LoadLibraryA) injector
      mise-mreload.dll - the hot-reload hook (metadata re-parse + texture LockRect swap)

.DESCRIPTION
    Needs Visual Studio (any edition, 2022 or later) with the "Desktop development with C++" workload -
    the same VS that builds the editor. The toolset is found with vswhere; nothing else to install.
    Run from anywhere:  .\se-file-hook\build.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$here = $PSScriptRoot

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw "vswhere.exe not found at $vswhere - install Visual Studio with the 'Desktop development with C++' workload"
}
$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) {
    throw "No Visual Studio with the C++ toolset (Microsoft.VisualStudio.Component.VC.Tools.x86.x64) found - add the 'Desktop development with C++' workload"
}
$vcvars = Join-Path $vsPath 'VC\Auxiliary\Build\vcvarsall.bat'
if (-not (Test-Path -LiteralPath $vcvars)) {
    throw "vcvarsall.bat not found under $vsPath"
}
Write-Host "using MSVC from $vsPath (x86)"

# intermediates (.obj / .exp / .lib) go to obj\, only the two tools land in this folder
$objDir = Join-Path $here 'obj'
New-Item -ItemType Directory -Force -Path $objDir | Out-Null

# cl runs inside one cmd session that has vcvarsall x86 applied. /MT: self-contained (static CRT).
# /Oy-: keep frame pointers (reliable stack frames in the hook). Both match the original gcc build.
$common = '/nologo /W4 /MT /D_CRT_SECURE_NO_WARNINGS /Fo"{0}\\"' -f $objDir
$buildInjector = 'cl {0} /O2 inject.c /Fe"se-inject.exe" /link kernel32.lib' -f $common
$buildDll = 'cl {0} /O1 /Oy- /LD mreload.c /Fe"mise-mreload.dll" /link kernel32.lib user32.lib /IMPLIB:"{1}\mise-mreload.lib"' -f $common, $objDir

Push-Location $here
try {
    # vcvarsall.bat itself calls vswhere.exe by name
    $env:PATH = (Split-Path $vswhere) + ';' + $env:PATH
    cmd /c "`"$vcvars`" x86 >nul && $buildInjector && $buildDll"
    if ($LASTEXITCODE -ne 0) {
        throw "se-file-hook build failed (exit code $LASTEXITCODE)"
    }
} finally {
    Pop-Location
}

Write-Host 'built:'
Get-ChildItem -LiteralPath $here -Filter 'se-inject.exe' | ForEach-Object { Write-Host ("  {0,10} {1}" -f $_.Length, $_.Name) }
Get-ChildItem -LiteralPath $here -Filter 'mise-mreload.dll' | ForEach-Object { Write-Host ("  {0,10} {1}" -f $_.Length, $_.Name) }
