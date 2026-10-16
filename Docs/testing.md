# Development checks

Use the SDK selected by `global.json`.

## Build and tests

```sh
dotnet restore Ultimate_Gshop.sln
dotnet build Ultimate_Gshop.sln --configuration Release --no-restore --warnaserror
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-build --no-restore
```

The test project references the compiled application. Tests use synthetic shops and temporary session directories. UI tests run Avalonia's headless backend with Skia rendering.

## Coverage

From PowerShell, generate and check application coverage:

```powershell
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore --coverage --coverage-output-format cobertura --coverage-output (Join-Path $PWD 'TestResults/application-coverage.cobertura.xml') --coverage-settings tests/application-coverage.config
./scripts/Write-CoverageSummary.ps1
```

[coverage-policy.json](../tests/coverage-policy.json) sets minimum line and branch coverage. The collector includes application UI and mapped XAML; test and generated build sources are excluded. Reports are generated under `TestResults` and ignored by Git.

## Resources

```powershell
./scripts/Test-AssetBaseline.ps1
```

The check compares resource paths and SHA-256 hashes against [the resource manifest](baseline/assets.json). Published output can be checked with `-RepositoryRoot <publish-folder> -Mode Published`.

## CI

The CI workflow builds and runs tests on Windows, Linux and macOS, collects coverage on Linux, and verifies published resources. On successful default-branch pushes, its badge job updates README coverage and stack badges from the generated report and project metadata.

Pull requests adding regression tests, reproducible examples and platform checks are welcome.