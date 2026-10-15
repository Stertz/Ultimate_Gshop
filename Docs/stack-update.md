# Stack Update Status

Updated: 2026-10-15.
Branch: `main` (modernization originally prepared on `chore/stack-update`).
Starting commit: `ef1aa7762b2c3bfcc2e0444bc214f76c7c2dea7f`.
Public repository: `https://github.com/Stertz/Ultimate_Gshop`. Original source is retained as `upstream`.

## Changes prepared

| Component | Before | Current project reference |
| --- | --- | --- |
| Target framework | net9.0-windows | net10.0 |
| SDK selection | Unpinned | 10.0.402, stable servicing patches allowed |
| Avalonia / Desktop / Fluent | 11.3.4 | 12.1.4 |
| ColorPicker | 11.3.4 | Removed after upgrade; only the application's custom picker is used |
| DataGrid | 11.3.4 | 12.1.2 |
| ItemsRepeater | 11.1.5 | 12.0.0 |
| ReactiveUI integration | Avalonia.ReactiveUI 11.3.4 | ReactiveUI.Avalonia 12.1.6 |
| Direct collection/observable APIs | Formerly obtained transitively | DynamicData 9.4.33 and System.Reactive 7.0.0 explicitly referenced |
| Diagnostics | Avalonia.Diagnostics 11.3.4 | Removed; no attachment was used and Avalonia 12 removed the package |
| Initial tests | None | xunit.v3.mtp-v2 4.0.1; MTP coverage extension 18.11.2 |

The control packages release independently; their numbers are intentionally different. Versions and API replacements were checked against publisher documentation and NuGet metadata. After the user restored packages outside the restricted executor, the full solution restored successfully from the populated local cache and built with zero warnings/errors.

The portable framework target permits desktop builds without a Windows-only framework declaration. It is not a claim of validated cross-platform execution. Existing resource copy rules, executable/assembly name, and game-facing paths are retained. Version metadata now derives from one `1.1.0` prefix, with `alpha.local` for local builds and `alpha.<CI-run-number>` for prepared prereleases.

## Required API migration

- Preserve reflection bindings explicitly with `AvaloniaUseCompiledBindingsByDefault=false`; typed/compiled binding migration is deferred.
- Import the maintained ReactiveUI integration namespace and use its required registration callback without removing startup integration.
- Replace `SystemDecorations` with `WindowDecorations` in the same six windows.
- Replace clipboard text retrieval with `TryGetTextAsync`.
- Replace the removed text-box `Watermark` property with `PlaceholderText` while preserving its existing text.
- Replace `DataObject`, `DragEventArgs.Data`, and `DoDragDrop` with typed transfer APIs and `DoDragDropAsync`. Preserve the original pointer-press event required by the new API and clear drag state in `finally`. The payload remains the same integer item place and uses the same internal identifier within the process.

The legacy byte encodings of `FloatingLabelTextBox.axaml.cs` and `OptionsWindow.axaml.cs` were preserved when applying ASCII edits. No assets were renamed, moved, re-encoded, or modified. Application folders remain in place. The tested fixes below followed the stack migration.

## Regression fixes after executable tests

- Binary/JSON saves stage every payload and previous-generation backup before replacement, with a recovery journal and shared session lock. Serialization/replacement failures restore the previous generation; shorter saves leave no stale bytes. Interrupted-operation fixtures cover restart recovery, not physical power-loss certification (R02 recovery implemented; broader filesystem checks pending).
- Both exporters write and iterate the actual collection count. Mutable model count/history consistency remains broader follow-up work (R04 partly addressed).
- Item comparisons include IDs and sale values. Updates locate the original instance first, or a unique ID/place pair for detached snapshots, and reject ambiguous matches. Delete/selection identity remains pending (R07 partly addressed).
- New shops initialize item places, sale slots, and owner-NPC slots so that the existing writer can save them (R08 defaults regression covered).
- Image lookup uses the exact `Resources` case and OS separators while retaining game-facing paths and DDS-to-PNG mapping. Real Skia decoding is tested without changing game assets (R09 partly addressed).
- Parser counts use validated 64-bit offsets and remaining-file bounds. Fixed-width fields require all bytes; integer loop counters handle a valid 256-subcategory fixture without wrapping. Trailing-byte acceptance and legacy value normalization are unchanged (R06 core checks covered).
- Persisted undo/redo JSON retains its existing top-first array layout and property names; a property converter restores the correct stack order (R04 persistence ordering covered).
- Imports validate a private snapshot and commit original binary bytes, empty history, and updated settings through the recoverable session transaction. New shops also reset history/source while preserving preferences. Menu glue clears in-memory history after successful switching; native interaction checks remain pending (R05 data-layer preservation/reset covered).
- Client/server commands now request their matching file writer. Both reuse the same tested protected replacement path as saves; failures preserve previous exports. Native command execution and independently verified server fixtures remain pending (R01 routing addressed, compatibility validation pending).
- Fixed-width encoding methods now share one implementation, encode once, and preserve the tested null/padding/byte-truncation behavior (R15 duplication addressed; character-safe truncation remains a separate format decision).

See [persistence](persistence.md) for the recovery protocol, corrupted-metadata policy, lock behavior, and filesystem limits. Existing binary/JSON contracts and asset structure remain intact; recovery/lock files are new session metadata only.

Removing the unused Avalonia ColorPicker reference reduced resolved application packages from 44 to 43 (including transitive packages), with direct references reduced from 9 to 8. No package-size or runtime performance improvement is claimed. ReactiveUI and the directly used observable/collection libraries remain in place.

Each fix followed a failing regression scenario. Serialized widths/order/encodings and legacy normalization remain covered by characterization tests; independent game-file equivalence is still pending.

## Validation completed

- Source resource verifier: all 4,580 files match their original paths, case, sizes, and SHA-256 hashes.
- Full solution restore and Release build on .NET 10: zero warnings and zero errors with warnings treated as errors.
- All 149 tests reference the compiled application and pass, including actual Skia image decoding and synthetic binary persistence.
- Whole-application coverage: 711/3,861 lines (18.41%) and 289/1,396 branches (20.70%), including untested UI in the denominator. Initial floors are 18.0%/20.0%; targets remain 80%/70%. See [testing scope](testing.md).
- Local framework-dependent publish succeeded, includes the GPL license, and preserves all 4,554 game surfaces by exact path/case/size/hash. This is not a self-contained release.
- The actual published Windows executable passed `--verify-installation` from the OS temporary directory, without native windows or real user data. This check exercises resources, native raster decoding, synthetic save/load, and both file writers.
- Workflow/issue YAML and PowerShell scripts parse successfully. Version derivation produced `1.1.0-alpha.42` from run 42. Local runtime packs for self-contained builds are unavailable; the release matrix has not executed.

## Validation pending

The earlier NuGet socket blocker was resolved by the user's restore outside the restricted executor and subsequent cache-only restoration here. Compilation is now verified. Native window startup, pointer/clipboard behavior, UI bindings, and independent known-good game files still need validation. Raster tests do not start a desktop window.

Hosted builds and all 149 tests passed on Linux, Windows, and macOS in [CI run 38798147466](https://github.com/Stertz/Ultimate_Gshop/actions/runs/38798147466). Linux coverage matched the Windows baseline. The first run exposed a missing policy file: `coverage*.json` had ignored `tests/coverage-policy.json`. Commit `84a9520` tracks the policy and adds a narrow ignore exception; all six checks then passed. Dependabot configuration covers NuGet and GitHub Actions.

Normal checks retain read-only permissions and require no deployment server. A separate default-branch job is prepared to update only the README badge blocks after successful checks; it has scoped contents-write permission. Coverage and stack versions come from the report/project, while release/CI badges read GitHub automatically. See [badge automation](testing.md#readme-badges).

Reproduce the compiled-application checks:

```sh
dotnet restore Ultimate_Gshop.sln
dotnet build Ultimate_Gshop.sln --configuration Release --no-restore --warnaserror
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore
```

Continue with delete identity, independent game fixtures, recovery error presentation, and broader backup/filesystem validation before broader refactoring. Native UI tests are deferred beyond this first phase per the user's direction. Contribution materials and the prerelease pipeline are present in the public repository. The pipeline targets Windows x64, Linux x64, and macOS Intel/ARM64, with native package smoke checks and checksums; it is disabled until `ENABLE_PRERELEASES` is enabled. See [releasing](releasing.md). Self-contained package execution, final publication notices, private-reporting activation, and remote About settings remain pending.

## Publisher references

- [Avalonia 12.1.4](https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.4)
- [Avalonia 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [ColorPicker](https://www.nuget.org/packages/Avalonia.Controls.ColorPicker/12.1.3)
- [DataGrid](https://www.nuget.org/packages/Avalonia.Controls.DataGrid/12.1.2)
- [ItemsRepeater](https://www.nuget.org/packages/Avalonia.Controls.ItemsRepeater/12.0.0)
- [ReactiveUI.Avalonia](https://www.nuget.org/packages/ReactiveUI.Avalonia/12.1.6)
- [DynamicData](https://www.nuget.org/packages/DynamicData/9.4.33)
- [System.Reactive](https://www.nuget.org/packages/System.Reactive/7.0.0)
- [xUnit.net v3 setup](https://xunit.net/docs/getting-started/v3/getting-started)
- [MTP code coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage)
