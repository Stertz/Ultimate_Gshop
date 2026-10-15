#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $ReportPath = 'TestResults/application-coverage.cobertura.xml',
    [string] $PolicyPath = (Join-Path $PSScriptRoot '../tests/coverage-policy.json'),
    [string] $HtmlPath = 'TestResults/coverage/index.html'
)

$ErrorActionPreference = 'Stop'
$policy = Get-Content -LiteralPath $PolicyPath -Raw | ConvertFrom-Json
$MinimumLineCoverage = [double] $policy.minimumLineCoverage
$MinimumBranchCoverage = [double] $policy.minimumBranchCoverage
if ($MinimumLineCoverage -lt 0 -or $MinimumLineCoverage -gt 100 -or
    $MinimumBranchCoverage -lt 0 -or $MinimumBranchCoverage -gt 100) {
    throw 'Invalid application coverage policy.'
}
[xml] $report = Get-Content -LiteralPath $ReportPath -Raw
$coverage = $report.DocumentElement
$classes = $report.SelectNodes('/coverage/packages/package/classes/class')
if ($coverage.Name -ne 'coverage' -or $classes.Count -eq 0) {
    throw 'The application coverage report is empty or invalid.'
}
$packages = $report.SelectNodes('/coverage/packages/package')
if ($packages.Count -ne 1 -or $packages[0].GetAttribute('name') -cne $policy.assembly) {
    throw 'Coverage must measure the compiled application assembly, not a test-only subset.'
}

$coveredLines = [long] $coverage.GetAttribute('lines-covered')
$totalLines = [long] $coverage.GetAttribute('lines-valid')
$coveredBranches = [long] $coverage.GetAttribute('branches-covered')
$totalBranches = [long] $coverage.GetAttribute('branches-valid')
if ($totalLines -le 0 -or $totalBranches -le 0 -or
    $coveredLines -lt 0 -or $coveredBranches -lt 0 -or
    $coveredLines -gt $totalLines -or $coveredBranches -gt $totalBranches) {
    throw 'The application coverage report has missing or inconsistent measurements.'
}

$lineCoverage = 100.0 * $coveredLines / $totalLines
$branchCoverage = 100.0 * $coveredBranches / $totalBranches
$culture = [Globalization.CultureInfo]::InvariantCulture
$lineText = $lineCoverage.ToString('F2', $culture)
$branchText = $branchCoverage.ToString('F2', $culture)
$summary = @"
### Application coverage

| Measurement | Covered | Total | Percentage |
| --- | ---: | ---: | ---: |
| Lines | $coveredLines | $totalLines | $lineText% |
| Branches | $coveredBranches | $totalBranches | $branchText% |

Scope: the compiled Ultimate_Gshop application, including untested UI and mapped XAML code.
Current regression floors: $MinimumLineCoverage% lines, $MinimumBranchCoverage% branches.
Targets as coverage expands: $($policy.targetLineCoverage)% lines, $($policy.targetBranchCoverage)% branches.
Passing these initial floors does not establish complete UI or game compatibility.
"@

Write-Output $summary
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary -Encoding utf8
}

$rows = foreach ($class in ($classes | Sort-Object { $_.GetAttribute('filename') }, { $_.GetAttribute('name') })) {
    $name = [Net.WebUtility]::HtmlEncode($class.GetAttribute('name'))
    $filename = [IO.Path]::GetFileName($class.GetAttribute('filename'))
    $filename = [Net.WebUtility]::HtmlEncode($filename)
    $lines = $class.SelectNodes('lines/line')
    $classLines = if ($lines.Count -gt 0) {
        $value = [double]::Parse($class.GetAttribute('line-rate'), $culture)
        (100 * $value).ToString('F2', $culture) + '%'
    } else { 'N/A' }
    $classBranches = if (@($lines | Where-Object { $_.GetAttribute('branch') -ieq 'true' }).Count -gt 0) {
        $value = [double]::Parse($class.GetAttribute('branch-rate'), $culture)
        (100 * $value).ToString('F2', $culture) + '%'
    } else { 'N/A' }
    "<tr><td>$name</td><td>$filename</td><td>$classLines</td><td>$classBranches</td></tr>"
}
$html = @"
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Ultimate Gshop application coverage</title>
<style>body{font:16px system-ui,sans-serif;margin:2rem;color:#172033;background:#f8fafc}main{max-width:1200px;margin:auto}table{border-collapse:collapse;width:100%;background:white}th,td{padding:.6rem;text-align:left;border-bottom:1px solid #cbd5e1}th{background:#e2e8f0}td:first-child{overflow-wrap:anywhere}p{line-height:1.5}.metrics{font-size:1.25rem;font-weight:600}.table{overflow:auto}</style>
</head><body><main><h1>Ultimate Gshop application coverage</h1>
<p class="metrics">Lines: $lineText% ($coveredLines/$totalLines) &middot; Branches: $branchText% ($coveredBranches/$totalBranches)</p>
<p>Scope: the compiled application, including untested UI and mapped XAML code. Auto-properties follow the collector defaults. Test and generated build sources are excluded. These results do not certify game compatibility.</p>
<p>Current regression floors: $MinimumLineCoverage% lines and $MinimumBranchCoverage% branches. Targets: $($policy.targetLineCoverage)% and $($policy.targetBranchCoverage)%.</p>
<div class="table"><table><thead><tr><th>Class</th><th>Source</th><th>Lines</th><th>Branches</th></tr></thead><tbody>
$($rows -join "`n")
</tbody></table></div></main></body></html>
"@
$htmlDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($HtmlPath))
New-Item -ItemType Directory -Path $htmlDirectory -Force | Out-Null
Set-Content -LiteralPath $HtmlPath -Value $html -Encoding utf8

if ($lineCoverage -lt $MinimumLineCoverage -or $branchCoverage -lt $MinimumBranchCoverage) {
    throw 'Application coverage is below the recorded regression floor.'
}
