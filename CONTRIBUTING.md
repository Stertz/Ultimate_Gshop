# Contributing to Ultimate Gshop

Ultimate Gshop is undergoing modernization and remains a preview. Contributions are welcome, especially reproducible format issues, independent redistributable fixtures, compatibility tests, and small maintenance changes.

## Development

Install the .NET SDK selected by `global.json`, then run from the repository root:

```sh
dotnet restore Ultimate_Gshop.sln
dotnet build Ultimate_Gshop.sln --configuration Release --no-restore --warnaserror
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore
```

See [testing and coverage](Docs/testing.md), the [modernization plan](Docs/modernization-plan.md), and [current implementation status](Docs/stack-update.md). Native desktop checks need the OS desktop libraries used by Avalonia; the current raster tests do not require opening a window.

## Compatibility comes first

- Preserve the entire `Resources` structure and exact filenames, case, encodings, formats, and bytes. Run `./scripts/Test-AssetBaseline.ps1` with PowerShell 7.
- Preserve game-facing paths, binary widths/order/padding, reserved fields, and existing JSON keys. Do not translate serialized values or game identifiers for style consistency.
- Reproduce a bug with a focused test before changing its behavior. Keep characterization of legacy behavior separate from an independently confirmed format rule.
- Use synthetic fixtures or files you have permission to redistribute. Include the game-format version, producing tool/version, expected result, and provenance. Never attach credentials or private server data.
- Keep existing application folders in place. Prefer a small extraction or reuse of an existing rule over adding layers, dependencies, or a new project layout.

## Pull requests

Keep changes focused and describe the concrete trigger, resulting behavior, and validation. Use English for new code comments, documentation, messages, and PR descriptions. Explain non-obvious compatibility decisions; avoid comments that merely repeat the code. Follow surrounding C# style without reformatting unrelated files or changing legacy file encoding incidentally.

Include relevant local test/build results and any native checks you performed. Explain coverage changes and unresolved limits. Whole-application coverage includes untested UI; do not narrow the collector scope or lower floors to hide a regression. End-of-stage coverage targets are goals, not current claims.

Discuss large format changes, new dependencies, or distribution changes in an issue before implementation. A useful bug report includes app version/commit, OS/architecture, exact reproduction steps, expected and actual behavior, and a minimal shareable fixture where possible. Community checks use GitHub-hosted runners and require no deployment server or private credentials.

Contributions are made under the project's existing [GPL license](LICENSE), declared as GPL-3.0-or-later in project metadata. Preserve both original authors and third-party notices. Source licensing does not establish redistribution rights for game assets.
