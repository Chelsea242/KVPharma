param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $PSScriptRoot "version.json") | ConvertFrom-Json).version
$publishScript = Join-Path $PSScriptRoot "publish.ps1"
& $publishScript -Configuration $Configuration -Runtime $Runtime
if ($LASTEXITCODE -ne 0) { throw "La publication a échoué." }

$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if ($null -eq $iscc) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }
    if ($candidates.Count -eq 0) { throw "Inno Setup 6 est requis pour produire Setup-Pharma.exe. Installez Inno Setup puis relancez ce script." }
    $isccPath = $candidates[0]
} else { $isccPath = $iscc.Source }

$publishDir = Join-Path $projectRoot "Livrable\Deployment\publish"
$installerDir = Join-Path $projectRoot "Livrable\Deployment\installer"
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
$iss = Join-Path $PSScriptRoot "Pharma.iss"
& $isccPath "/DAppVersion=$version" "/DPUBLISH_DIR=$publishDir" "/DOUTPUT_DIR=$installerDir" $iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup a échoué." }
Write-Host "Installateur : $(Join-Path $installerDir "Setup-Pharma.exe")"
