#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PublishDirectory,
    [Parameter(Mandatory)] [ValidateSet('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')] [string] $Runtime,
    [Parameter(Mandatory)] [ValidatePattern('^\d+\.\d+\.\d+-alpha\.\d+$')] [string] $Version,
    [Parameter(Mandatory)] [ValidatePattern('^[0-9a-f]{40}$')] [string] $Commit,
    [string] $OutputDirectory = 'artifacts/releases'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()
if (($Runtime -eq 'win-x64' -and (-not $IsWindows -or $architecture -ne 'x64')) -or
    ($Runtime -eq 'linux-x64' -and (-not $IsLinux -or $architecture -ne 'x64')) -or
    ($Runtime.StartsWith('osx-') -and (-not $IsMacOS -or $Runtime -ne "osx-$architecture"))) {
    throw 'Release packages must be verified on their native OS and architecture.'
}
$runtimeConfig = Get-Content -LiteralPath (Join-Path $publish 'Ultimate_Gshop.runtimeconfig.json') -Raw | ConvertFrom-Json
if (-not $runtimeConfig.runtimeOptions.includedFrameworks) {
    throw 'Release downloads must be self-contained.'
}
$productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publish 'Ultimate_Gshop.dll')).ProductVersion
if ($productVersion -ne $Version -and -not $productVersion.StartsWith("$Version+", [StringComparison]::Ordinal)) {
    throw 'Published assembly metadata does not match the release version.'
}
& (Join-Path $PSScriptRoot 'Test-AssetBaseline.ps1') -RepositoryRoot $publish -ManifestPath (Join-Path $root 'Docs/baseline/assets.json') -Mode Published
foreach ($name in @('LICENSE', 'README.md', 'THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $publish $name)
}
@{ version = $Version; commit = $Commit; runtime = $Runtime; selfContained = $true; status = 'alpha'; signed = $false } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publish 'build-info.json') -Encoding utf8

$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$staging = Join-Path $output "staging-$Runtime"
if (Test-Path -LiteralPath $staging) { throw 'Use a fresh package staging directory.' }
New-Item -ItemType Directory -Path $staging | Out-Null
$bundleName = "UltimateGshop-$Version-$Runtime"
if ($Runtime.StartsWith('osx-')) {
    $bundle = Join-Path $staging 'Ultimate Gshop.app'
    $contents = Join-Path $bundle 'Contents'
    $payload = Join-Path $contents 'MacOS'
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    # Keep the complete published layout together so AppContext.BaseDirectory still finds Resources.
    Get-ChildItem -LiteralPath $publish -Force | Copy-Item -Destination $payload -Recurse
    $numericVersion = $Version.Split('-')[0]
    @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>Ultimate Gshop</string>
<key>CFBundleDisplayName</key><string>Ultimate Gshop</string>
<key>CFBundleIdentifier</key><string>org.ultimategshop.editor</string>
<key>CFBundleExecutable</key><string>Ultimate_Gshop</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$numericVersion</string>
<key>CFBundleVersion</key><string>$($Version.Split('.')[-1])</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
"@ | Set-Content -LiteralPath (Join-Path $contents 'Info.plist') -Encoding utf8
    & plutil -lint (Join-Path $contents 'Info.plist')
    if ($LASTEXITCODE -ne 0) { throw 'The macOS bundle manifest is invalid.' }
} else {
    $payload = Join-Path $staging $bundleName
    New-Item -ItemType Directory -Path $payload | Out-Null
    Get-ChildItem -LiteralPath $publish -Force | Copy-Item -Destination $payload -Recurse
}
$executableName = if ($IsWindows) { 'Ultimate_Gshop.exe' } else { 'Ultimate_Gshop' }
$executable = Join-Path $payload $executableName
if (-not $IsWindows) {
    & chmod +x $executable
    if ($LASTEXITCODE -ne 0) { throw 'Cannot preserve executable permissions.' }
}
$start = @{ FilePath = $executable; ArgumentList = '--verify-installation'; PassThru = $true; Wait = $true; WorkingDirectory = [IO.Path]::GetTempPath() }
if ($IsWindows) { $start.WindowStyle = 'Hidden' }
$process = Start-Process @start
if ($process.ExitCode -ne 0) { throw "Installed package verification failed: $($process.ExitCode)" }
& (Join-Path $PSScriptRoot 'Test-AssetBaseline.ps1') -RepositoryRoot $payload -ManifestPath (Join-Path $root 'Docs/baseline/assets.json') -Mode Published

$extension = if ($IsWindows) { '.zip' } else { '.tar.gz' }
$archive = Join-Path $output ($bundleName + $extension)
if (Test-Path -LiteralPath $archive) { throw 'A release archive already exists.' }
if ($IsWindows) {
    Compress-Archive -LiteralPath $payload -DestinationPath $archive
} else {
    $archiveEntry = if ($IsMacOS) { 'Ultimate Gshop.app' } else { Split-Path -Leaf $payload }
    & tar -czf $archive -C $staging $archiveEntry
    if ($LASTEXITCODE -ne 0) { throw 'Archive creation failed.' }
}
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$archive.sha256", "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
& (Join-Path $PSScriptRoot 'Test-ReleaseArchive.ps1') -ArchivePath $archive -Runtime $Runtime -ExpectedVersion $Version -ExpectedCommit $Commit
Write-Output "Release package verified: $archive"
