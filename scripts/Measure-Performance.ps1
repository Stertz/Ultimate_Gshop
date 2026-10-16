[CmdletBinding()]
param(
    [string]$OutputDirectory = 'TestResults/performance',
    [ValidateRange(1, 10)][int]$Runs = 2
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    $project = 'tests/Ultimate_Gshop.Performance/Ultimate_Gshop.Performance.csproj'
    dotnet restore $project
    if ($LASTEXITCODE -ne 0) { throw 'Performance project restore failed.' }
    dotnet build $project --configuration Release --no-restore --warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Performance project build failed.' }
    for ($run = 1; $run -le $Runs; $run++) {
        $report = Join-Path $OutputDirectory "run-$run.json"
        dotnet run --project $project --configuration Release --no-build --no-restore -- $report
        if ($LASTEXITCODE -ne 0) { throw "Performance run $run failed." }
    }
}
finally {
    Pop-Location
}
