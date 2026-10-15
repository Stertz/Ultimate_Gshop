#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $ReadmePath = (Join-Path $PSScriptRoot '../README.md'),
    [string] $ProjectPath = (Join-Path $PSScriptRoot '../Ultimate_Gshop.csproj'),
    [string] $CoverageReportPath = (Join-Path $PSScriptRoot '../TestResults/application-coverage.cobertura.xml'),
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')] [string] $Repository = 'Stertz/Ultimate_Gshop'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[xml] $project = Get-Content -LiteralPath $ProjectPath -Raw
$framework = $project.SelectSingleNode('/Project/PropertyGroup/TargetFramework').InnerText
if ($framework -notmatch '^net(\d+\.\d+)$') { throw 'Cannot determine the target .NET version.' }
$dotnet = $Matches[1]
$avalonia = $project.SelectSingleNode('/Project/ItemGroup/PackageReference[@Include="Avalonia"]/@Version').Value
if ($avalonia -notmatch '^\d+\.\d+\.\d+$') { throw 'Cannot determine the Avalonia version.' }

[xml] $report = Get-Content -LiteralPath $CoverageReportPath -Raw
$coverage = $report.DocumentElement
$packages = $report.SelectNodes('/coverage/packages/package')
if ($coverage.Name -ne 'coverage' -or $packages.Count -ne 1 -or
    $packages[0].GetAttribute('name') -cne 'Ultimate_Gshop' -or
    $report.SelectNodes('/coverage/packages/package/classes/class').Count -eq 0) {
    throw 'The badge requires whole-application coverage.'
}
$total = [long] $coverage.GetAttribute('lines-valid')
$covered = [long] $coverage.GetAttribute('lines-covered')
if (-not $coverage.HasAttribute('lines-valid') -or -not $coverage.HasAttribute('lines-covered') -or
    $total -le 0 -or $covered -lt 0 -or $covered -gt $total) {
    throw 'The badge requires a nonempty, consistent coverage measurement.'
}
$percentage = 100.0 * $covered / $total
$text = $percentage.ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
$color = if ($percentage -ge 80) { 'brightgreen' } elseif ($percentage -ge 60) { 'yellow' } else { 'red' }
$readme = (Get-Content -LiteralPath $ReadmePath -Raw).Replace("`r`n", "`n")
$blocks = @{
    'COVERAGE-BADGE' = "  <a href=`"https://github.com/$Repository/actions/workflows/ci.yml`"><img src=`"https://img.shields.io/badge/Coverage-$text%25-${color}?logo=dotnet`" alt=`"Code coverage`" /></a>"
    'STACK-BADGES' = @"
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-$dotnet-512BD4?logo=dotnet&amp;logoColor=white" alt=".NET $dotnet" /></a>
  <a href="https://avaloniaui.net/"><img src="https://img.shields.io/badge/Avalonia-$avalonia-FF6600" alt="Avalonia $avalonia" /></a>
"@
}
foreach ($name in $blocks.Keys) {
    $pattern = '(?s)<!-- ' + [regex]::Escape($name) + ':START -->.*?<!-- ' + [regex]::Escape($name) + ':END -->'
    if ([regex]::Matches($readme, $pattern).Count -ne 1) {
        throw "The README must contain exactly one $name marker block."
    }
    $replacement = "<!-- ${name}:START -->`n$($blocks[$name])`n  <!-- ${name}:END -->"
    $readme = [regex]::Replace($readme, $pattern, [Text.RegularExpressions.MatchEvaluator] { param($match) $replacement })
}
[IO.File]::WriteAllText([IO.Path]::GetFullPath($ReadmePath), $readme.TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "README badges updated: coverage $text%, .NET $dotnet, Avalonia $avalonia."
