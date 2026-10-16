# Compatibility Tests

The suite uses xUnit.net v3 with Microsoft.Testing.Platform v2 on .NET 10. Its project references the compiled desktop application; production source is not copied or linked into a substitute test assembly. The original application folders and assets are preserved.

The existing test project groups its unchanged test files under `Assets`, `Serialization`, `Editing`, and `Persistence`. Shared fixture helpers stay at the test-project root; project names, namespaces, assembly references, and commands are unchanged.

## Run the suite

From the repository root, with the SDK selected by `global.json`:

```sh
dotnet restore Ultimate_Gshop.sln
dotnet build Ultimate_Gshop.sln --configuration Release --no-restore --warnaserror
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore
```

The SDK uses the MTP runner. Do not mix VSTest collector flags such as `--collect:"XPlat Code Coverage"` into these commands.

The latest local Windows run on 2026-10-16 passed all 256 tests. They cover:

- Fixed-width UTF-16 and CP936 bytes, padding, embedded nulls, Unicode, truncation, headers, record offsets, and categories.
- Synthetic client/server serialization, exact field offsets and record widths, load/save round trips, and stale item counts.
- Safe binary replacement, shorter saves, failed serialization, settings JSON keys, and valid new-shop defaults.
- Deep clones, item identity with duplicate or changed IDs, no-op edits, sale value comparison, and in-memory undo/redo.
- Selected price/time rules and existing category validation.
- Exact resource paths, case, sizes, and SHA-256 hashes.
- Portable image path resolution, DDS-to-PNG mapping, fallback, resizing, caching, and decoding actual bundled PNGs with Skia.
- Bounded rejection of negative/oversized counts and truncated fields; a valid 256-subcategory fixture verifies that the loop counter cannot wrap.
- Persisted undo/redo stack order, malformed-import preservation, original-byte snapshots, and protected client/server file export.
- Recoverable multi-file saves, interrupted transaction fixtures, concurrent writes, lock contention, metadata corruption, and session/history reset while preserving preferences.
- Search query broadening/replacement, partial IDs, Unicode and category scope; price rounding, decimal/integer limits and advanced arithmetic overflow.
- Filtered reorder boundaries, duplicate IDs, one-action undo/redo, save/reload and mixed edit history; duplicate-ID deletion, staged bulk validation and independent clone data.
- Clone position exhaustion, deduplicated/stale selections, independent sale/owner copies and saved clone history; reference-based Shift/Ctrl/Ctrl+Shift selection with stable anchors and refresh resets.
- Reversible category/subcategory resets, subcategory index shifts, metadata rename/add/delete history, no-op and invalid edits, real save/reload, and consistent item counts after undo/redo.

Raster tests initialize the real application resources and Skia renderer without starting a native window. They do not exercise desktop startup, bindings, clipboard, navigation, or drag/drop. Synthetic fixtures validate the documented test contracts; they do not independently certify compatibility with every game version. Legacy load normalization remains explicitly characterized rather than silently corrected.

## Resource contract

The immutable inventory is [baseline/assets.json](baseline/assets.json), captured before stack edits from commit `ef1aa7762b2c3bfcc2e0444bc214f76c7c2dea7f`. It covers all 4,580 files and 70,247,582 bytes in `Resources`.

```powershell
./scripts/Test-AssetBaseline.ps1
dotnet publish Ultimate_Gshop.csproj --configuration Release --no-restore --self-contained false --output artifacts/publish
./scripts/Test-AssetBaseline.ps1 -RepositoryRoot artifacts/publish -ManifestPath Docs/baseline/assets.json -Mode Published
```

Source mode checks all resources. Published mode checks the 4,554 game surface files copied alongside the executable; UI resources are embedded. Both checks enforce exact paths, case, sizes, and hashes, and reject additions or omissions in their selected tree. Neither modifies assets or regenerates the manifest. The local framework-dependent publish passed this check; self-contained releases and native execution on other operating systems remain pending.

## Whole-application coverage

In PowerShell, use an absolute report path to avoid runner working-directory differences:

```powershell
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore --coverage --coverage-output-format cobertura --coverage-output (Join-Path $PWD 'TestResults/application-coverage.cobertura.xml') --coverage-settings tests/application-coverage.config
./scripts/Write-CoverageSummary.ps1
```

The configuration includes the compiled `Ultimate_Gshop` assembly, including untested UI logic and mapped XAML source. Tests and generated `obj` sources are excluded; auto-properties are skipped by the collector's default configuration. The latest local Windows measurement on 2026-10-16 is **969/3,986 executable lines (24.31%)** and **512/1,524 branches (33.60%)**, up from the first increment's 818/3,838 lines (21.31%) and 378/1,388 branches (27.23%). The original baseline was 18.41%/20.70%. Rule extractions removed duplicated untested screen code and added tested application services; no additional source exclusions were introduced.

[coverage-policy.json](../tests/coverage-policy.json) records current regression floors of 24.0% lines and 33.0% branches, raised after clone, selection, category metadata and reset regressions. The final targets remain 80% and 70%; the [three-stage quality plan](quality-plan.md) proposes intermediate milestones of 35%/30% and 60%/50%. The first 30% branch milestone is reached; its 35% line milestone remains pending. Keep untested application code in the denominator and confirm the raised floors in the next hosted Linux CI run.

The summary script rejects missing, empty, inconsistent, or wrong-assembly reports and fails below the recorded floors. It writes a GitHub step summary when available and a standalone per-class HTML report at `TestResults/coverage/index.html`. Reports are ignored by Git. Hosted CI collects coverage on Linux and uploads Cobertura/HTML artifacts with seven-day retention. The [first successful run](https://github.com/Stertz/Ultimate_Gshop/actions/runs/38798147466) passed the original 149-test suite on all three operating systems and measured the same coverage as the original local baseline. The expanded 256-test suite and raised floors are locally validated on Windows; their hosted multi-platform validation is pending.

## README badges

The default-branch push workflow refreshes the README after every build/test job succeeds. `scripts/Update-ReadmeBadges.ps1` reads executable-line coverage from that run's whole-application Cobertura artifact and reads .NET/Avalonia versions from the project. It replaces only the marked badge blocks; percentages and stack versions are not maintained by hand.

CI status uses GitHub's workflow badge. The release badge reads published GitHub releases, including previews. It does not advertise an unpublished local version as an available download. Coverage describes the latest successful measurement, not a promise that the current build is passing.

The badge job receives write permission only on default-branch pushes in this repository. Pull requests cannot publish README changes. A newer source commit causes an older badge update to be skipped; pushes use no force. Badge-only commits use `GITHUB_TOKEN` and `[skip ci]` to avoid a recursive build/release loop. See [GitHub token event behavior](https://docs.github.com/en/actions/concepts/security/github_token).

Refresh the marked blocks locally after generating a report:

```powershell
./scripts/Update-ReadmeBadges.ps1
```

## Remaining coverage before broader improvements

- Independent known-good, redistributable game files and original-revision differential fixtures.
- Broader malformed parser cases, unsupported variants, and documented resource limits for unusually large inputs.
- Physical interruption/full-disk scenarios, broader backup policies, and native presentation of recovery errors. Arbitrary corrupt JSON without recovery backups remains an explicit error.
- Broader delete/move and combined bulk-operation sequences with duplicate game IDs and persisted history. Initial clone, reorder, selection, category reset and metadata histories now have regression tests.
- Confirmed game-specific sale/time/status rules and boundary conditions.
- Headless UI bindings, commands, navigation, search, clipboard, and drag/drop.
- Native startup and critical edit/export workflows from packaged output on Windows, macOS, and Linux.

Add a reproducing test before changing each affected behavior. Saves and session creation now stage their binary/JSON payloads together and maintain a recoverable journal. This is not an instantaneous filesystem transaction for arbitrary external readers. See [persistence and recovery](persistence.md) for the protocol and its limits.

Imports validate a private snapshot before replacing session files and retain its original bytes, including fields normalized in memory by the legacy loader. A successful import/new shop resets history and preserves user preferences; malformed imports leave existing files intact. Native dialogs and interaction glue remain separately unvalidated.

The published executable's `--verify-installation` diagnostic passed locally on Windows, exercising actual application resources/native raster decoding and synthetic save/load/export from a different working directory. It uses an isolated temporary directory, not the user's session. This smoke run is separate from the test count/coverage measurement. See [release checks](releasing.md); self-contained multi-OS packaging is prepared but not yet executed.

Native UI tests are deferred beyond this first phase at the user's request. They remain useful before advertising validated desktop downloads; they do not block tested data-layer fixes.
