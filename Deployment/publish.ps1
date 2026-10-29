param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $PSScriptRoot "version.json") | ConvertFrom-Json).version
$outputRoot = Join-Path $projectRoot "Livrable\Deployment"
$publishDir = Join-Path $outputRoot "publish"
$portableDir = Join-Path $outputRoot "portable"

Remove-Item -LiteralPath $outputRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir,$portableDir | Out-Null

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
dotnet restore (Join-Path $projectRoot "Pharma.csproj") --ignore-failed-sources --disable-parallel
if ($LASTEXITCODE -ne 0) { throw "dotnet restore a échoué." }
dotnet publish (Join-Path $projectRoot "Pharma.csproj") --no-restore -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishDir=$publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish a échoué." }

Copy-Item -LiteralPath (Join-Path $publishDir "Pharma.exe") -Destination (Join-Path $portableDir "Pharma.exe") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "README.md") -Destination (Join-Path $portableDir "README.md") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "LICENSE") -Destination (Join-Path $portableDir "LICENSE") -Force
Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath (Join-Path $outputRoot "Pharma-$version-win-x64-portable.zip") -Force
Write-Host "Publication autonome : $publishDir"
Write-Host "ZIP portable : $(Join-Path $outputRoot "Pharma-$version-win-x64-portable.zip")"
