#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ArchivePath,
    [Parameter(Mandatory)][ValidateSet('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')][string]$Runtime,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+-alpha\.\d+$')][string]$ExpectedVersion,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedCommit
)

$ErrorActionPreference = 'Stop'
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path
$architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()
$nativeRuntime = if ($IsWindows) { "win-$architecture" } elseif ($IsMacOS) { "osx-$architecture" } else { "linux-$architecture" }
if ($nativeRuntime -ne $Runtime) { throw 'Archive execution must use its native OS and architecture.' }
$bundleName = "UltimateGshop-$ExpectedVersion-$Runtime"
$extension = if ($IsWindows) { '.zip' } else { '.tar.gz' }
if ([IO.Path]::GetFileName($archive) -cne "$bundleName$extension") { throw 'Unexpected release archive filename.' }
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = [IO.File]::ReadAllText("$archive.sha256").TrimEnd("`r", "`n")
if ($checksum -cne "$hash  $([IO.Path]::GetFileName($archive))") { throw 'Release archive checksum does not match.' }

$expectedRoot = if ($IsMacOS) { 'Ultimate Gshop.app' } else { $bundleName }
if ($IsWindows) {
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try { $entries = @($zip.Entries | ForEach-Object { $_.FullName }) } finally { $zip.Dispose() }
} else {
    $entries = @(& tar -tf $archive)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot list the release archive.' }
}
if ($entries.Count -eq 0) { throw 'Release archive is empty.' }
foreach ($entry in $entries) {
    $normalized = $entry.Replace('\', '/').TrimEnd('/')
    if (($normalized -cne $expectedRoot -and -not $normalized.StartsWith("$expectedRoot/", [StringComparison]::Ordinal)) -or
        ($normalized.Split('/') -contains '..')) { throw 'Archive entry is outside the expected application folder.' }
}

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporary = [IO.Path]::GetFullPath((Join-Path $tempBase "ultimate-gshop-archive-$([Guid]::NewGuid().ToString('N'))"))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    if ($IsWindows) { Expand-Archive -LiteralPath $archive -DestinationPath $temporary }
    else {
        & tar -xf $archive -C $temporary
        if ($LASTEXITCODE -ne 0) { throw 'Release archive extraction failed.' }
    }
    $payload = Join-Path $temporary $expectedRoot
    if ($IsMacOS) { $payload = Join-Path $payload 'Contents/MacOS' }
    $info = Get-Content -LiteralPath (Join-Path $payload 'build-info.json') -Raw | ConvertFrom-Json
    if ($info.version -cne $ExpectedVersion -or $info.commit -cne $ExpectedCommit -or
        $info.runtime -cne $Runtime -or $info.selfContained -ne $true) { throw 'Extracted build metadata does not match.' }
    $config = Get-Content -LiteralPath (Join-Path $payload 'Ultimate_Gshop.runtimeconfig.json') -Raw | ConvertFrom-Json
    if (-not $config.runtimeOptions.includedFrameworks) { throw 'Extracted package is not self-contained.' }
    $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $payload 'Ultimate_Gshop.dll')).ProductVersion
    if ($productVersion -ne $ExpectedVersion -and -not $productVersion.StartsWith("$ExpectedVersion+", [StringComparison]::Ordinal)) {
        throw 'Extracted assembly version does not match.'
    }
    foreach ($name in @('LICENSE', 'README.md', 'THIRD_PARTY_NOTICES.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $payload $name) -PathType Leaf)) { throw "Missing distribution file: $name" }
    }
    & (Join-Path $PSScriptRoot 'Test-AssetBaseline.ps1') -RepositoryRoot $payload -ManifestPath (Join-Path $PSScriptRoot '../Docs/baseline/assets.json') -Mode Published
    $executableName = if ($IsWindows) { 'Ultimate_Gshop.exe' } else { 'Ultimate_Gshop' }
    $start = @{ FilePath = (Join-Path $payload $executableName); ArgumentList = '--verify-installation'; PassThru = $true; Wait = $true; WorkingDirectory = $tempBase }
    if ($IsWindows) { $start.WindowStyle = 'Hidden' }
    $process = Start-Process @start
    if ($process.ExitCode -ne 0) { throw "Extracted installation verification failed: $($process.ExitCode)" }
    Write-Output "Release archive extraction verified: $archive"
}
finally {
    $relative = [IO.Path]::GetRelativePath($tempBase, $temporary)
    if ($relative -cne [IO.Path]::GetFileName($temporary) -or -not $relative.StartsWith('ultimate-gshop-archive-', [StringComparison]::Ordinal)) {
        throw 'Unexpected archive verification cleanup path.'
    }
    Remove-Item -LiteralPath $temporary -Recurse -Force
}
