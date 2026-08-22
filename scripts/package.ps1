[CmdletBinding()]
param(
    [string]$Version = '0.1.0.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'PotPlayerBridge.slnx'
$project = Join-Path $repositoryRoot 'src\Jellyfin.Plugin.PotPlayerBridge\Jellyfin.Plugin.PotPlayerBridge.csproj'
$publishDirectory = Join-Path $repositoryRoot 'artifacts\publish'
$releaseDirectory = Join-Path $repositoryRoot 'artifacts\release'
$fileName = "Jellyfin.Plugin.PotPlayerBridge_$Version.zip"
$zipPath = Join-Path $releaseDirectory $fileName

dotnet restore $solution
dotnet test $solution -c Release --no-restore --nologo
dotnet publish $project -c Release --no-restore --nologo -o $publishDirectory

New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive `
    -Path (Join-Path $publishDirectory 'Jellyfin.Plugin.PotPlayerBridge.dll') `
    -DestinationPath $zipPath

$md5 = (Get-FileHash -LiteralPath $zipPath -Algorithm MD5).Hash.ToLowerInvariant()
$sha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$zipPath.md5" -Value "$md5  $fileName" -Encoding utf8NoBOM
Set-Content -LiteralPath "$zipPath.sha256" -Value "$sha256  $fileName" -Encoding utf8NoBOM

Write-Host "Created $zipPath"
Write-Host "MD5:    $md5"
Write-Host "SHA256: $sha256"
