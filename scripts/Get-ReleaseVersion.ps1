#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)] [ValidateRange(1, 2147483647)] [int] $RunNumber)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../Ultimate_Gshop.csproj'
$prefix = (& dotnet msbuild $project -getProperty:VersionPrefix -nologo).Trim()
if ($LASTEXITCODE -ne 0 -or $prefix -notmatch '^\d+\.\d+\.\d+$') {
    throw 'The project must define one valid numeric VersionPrefix.'
}
$version = "$prefix-alpha.$RunNumber"
Write-Output $version
if ($env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "version=$version`ntag=v$version" -Encoding utf8
}
