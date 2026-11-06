#Requires -Version 5.1
<#
.SYNOPSIS
    Checks that a built copy of WuffPad starts without the (shut down) update server.

.DESCRIPTION
    Copies the Release output to a temporary folder, deletes the Resources folder so
    the run looks like a fresh install, and runs "WuffPad.exe --selftest". That runs
    the same checks the start-up does (embedded data files, extraction, parsing of the
    language blueprint, descriptions, default keys and emojis) without opening a window.

.PARAMETER BuildDirectory
    Folder with the built executable. Defaults to "XML-Editor WuffPad/bin/Release".

.PARAMETER KeepFiles
    Keep the temporary folder instead of deleting it.

.EXAMPLE
    pwsh tools/smoke-test.ps1
#>
[CmdletBinding()]
param(
    [string] $BuildDirectory = (Join-Path $PSScriptRoot '..\XML-Editor WuffPad\bin\Release'),
    [switch] $KeepFiles
)

$ErrorActionPreference = 'Stop'

$source = (Resolve-Path $BuildDirectory).Path
$exeName = 'XML-Editor WuffPad.exe'
if (-not (Test-Path (Join-Path $source $exeName))) {
    throw "No $exeName in $source - build the solution first (msbuild `"XML-Editor WuffPad.sln`" /p:Configuration=Release)."
}

$fresh = Join-Path ([System.IO.Path]::GetTempPath()) ('wuffpad-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fresh | Out-Null
try {
    Copy-Item -Path (Join-Path $source '*') -Destination $fresh -Recurse -Force
    # A fresh install has no data folder: everything must come out of the exe.
    $resources = Join-Path $fresh 'Resources'
    if (Test-Path $resources) { Remove-Item $resources -Recurse -Force }

    $exe = Join-Path $fresh $exeName
    $report = Join-Path $fresh 'selftest.txt'

    Write-Host "Running self-test from a clean copy in $fresh" -ForegroundColor Cyan
    $process = Start-Process -FilePath $exe -ArgumentList '--selftest', $report -Wait -PassThru
    if (Test-Path $report) { Get-Content $report | ForEach-Object { Write-Host "    $_" } }

    if ($process.ExitCode -ne 0) {
        throw "Self-test failed with exit code $($process.ExitCode)."
    }
    Write-Host 'Self-test passed: WuffPad works without the original update server.' -ForegroundColor Green
}
finally {
    if (-not $KeepFiles) {
        Remove-Item $fresh -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "Files kept in $fresh"
    }
}
