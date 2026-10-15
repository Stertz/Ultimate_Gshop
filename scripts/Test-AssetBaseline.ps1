#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string] $ManifestPath,
    [ValidateSet('Source', 'Published')] [string] $Mode = 'Source'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
if (-not $ManifestPath) {
    $ManifestPath = Join-Path $RepositoryRoot 'Docs/baseline/assets.json'
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.root -cne 'Resources') {
    throw 'Unsupported asset baseline schema or resource root.'
}

$allExpected = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($entry in $manifest.files) {
    $allExpected.Add($entry.path, $entry)
}
if ($allExpected.Count -ne $manifest.fileCount) {
    throw 'The asset baseline count is inconsistent.'
}

$assetRoot = if ($Mode -eq 'Published') { 'Resources/surfaces' } else { 'Resources' }
$expected = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($entry in $manifest.files) {
    if ($entry.path.StartsWith("$assetRoot/", [StringComparison]::Ordinal)) {
        $expected.Add($entry.path, $entry)
    }
}
if ($expected.Count -eq 0) { throw 'The selected asset baseline is empty.' }

$actualRoot = $RepositoryRoot
foreach ($segment in $assetRoot.Split('/')) {
    $directory = @(Get-ChildItem -LiteralPath $actualRoot -Directory -Force | Where-Object { $_.Name -ceq $segment })
    if ($directory.Count -ne 1) { throw "Missing or case-mismatched asset directory: $assetRoot" }
    $actualRoot = $directory[0].FullName
}

$actual = [Collections.Generic.Dictionary[string, IO.FileInfo]]::new([StringComparer]::Ordinal)
foreach ($file in Get-ChildItem -LiteralPath $actualRoot -Recurse -File -Force) {
    $relativePath = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
    $actual.Add($relativePath, $file)
}

$differences = [Collections.Generic.List[string]]::new()
foreach ($path in $expected.Keys) {
    if (-not $actual.ContainsKey($path)) {
        $differences.Add("Missing or renamed asset: $path")
        continue
    }

    $file = $actual[$path]
    $entry = $expected[$path]
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ($file.Length -ne $entry.size -or $hash -ine $entry.sha256) {
        $differences.Add("Changed asset: $path")
    }
}
foreach ($path in $actual.Keys) {
    if (-not $expected.ContainsKey($path)) {
        $differences.Add("Unexpected asset: $path")
    }
}

if ($differences.Count -gt 0) {
    $differences | ForEach-Object { Write-Output $_ }
    throw "Asset compatibility check failed with $($differences.Count) difference(s)."
}

Write-Output "Asset compatibility check passed ($Mode): $($actual.Count) files retain their exact paths and SHA-256 hashes."
