# Contributing to Ultimate Gshop

Pull requests are welcome for bug fixes, usage guides, game compatibility reports, examples and platform improvements.

## Development

Install the .NET SDK selected by `global.json`, then run:

```sh
dotnet restore Ultimate_Gshop.sln
dotnet build Ultimate_Gshop.sln --configuration Release --no-restore --warnaserror
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-build --no-restore
```

See [development checks](Docs/testing.md) for coverage and resource verification, and [persistence](Docs/persistence.md) for session storage and recovery.

## Code changes

- Keep pull requests focused and describe the resulting behavior and relevant validation.
- Add a regression test when fixing a reproducible bug.
- Follow surrounding C# style, format only edited code and preserve existing file encodings.
- Preserve resource paths, filenames, case, formats and bytes. Run `./scripts/Test-AssetBaseline.ps1` with PowerShell 7.
- Preserve binary field order/width/padding, reserved fields, game identifiers and JSON keys when making unrelated changes.
- Keep application code in coverage measurements and follow the configured coverage floors.

## Documentation and compatibility

Improve the [usage guide](Docs/usage.md) with worked examples and screenshots. Add game-build reports using the format in [compatibility](Docs/compatibility.md). Include concrete values and reproduction steps when documenting sale dates, status flags or format differences.

Bug reports should include the application version/commit, OS/architecture, reproduction steps, and expected/actual behavior. Use synthetic or redistributable fixtures for examples and tests.

Use English for documentation, code comments and pull request descriptions. Contributions use the existing [GPL-3.0-or-later license](LICENSE); preserve author attribution and third-party notices.