# Initial Source Review

Reviewed on: 2026-10-15.
Source: `https://github.com/joaocib/Ultimate_Gshop`.
Commit: `ef1aa7762b2c3bfcc2e0444bc214f76c7c2dea7f`.

This review supplies evidence for [the modernization plan](modernization-plan.md). Findings are based on source inspection unless explicitly stated otherwise. At the time of this initial review, the application had not been successfully built or executed in this environment; current validation is recorded separately below.

This document describes the original commit, before implementation. The updated application now builds with zero warnings/errors, and 149 compiled-application compatibility tests pass. Current fixes, whole-application coverage, and remaining runtime limitations are recorded in [stack update status](stack-update.md) and [testing instructions](testing.md). Original findings remain pending unless those documents explicitly record a validated resolution.

## Checkout and environment

The supplied checkout was initially nested at `Documents/Ultimate_Gshop/Ultimate_Gshop`. Its files, including `.git`, were moved directly into `Documents/Ultimate_Gshop`. Git reported the same commit and a clean tracked working tree after the move. The original `origin` URL was preserved.

An attempted removal of the empty nested directory was rejected by the environment's automatic approval policy. A later filesystem check confirmed that the nested directory no longer exists; this review does not attribute that removal to the rejected command. The project is now directly in the intended directory.

No repository-specific `AGENTS.md` was found. No application source was changed during this review. The documentation added by this review is new and uncommitted, under the existing case-sensitive Git path `Docs/`, which will be preserved.

## User direction after the initial review

The user confirmed that assets come from the games and that their format/names may affect game behavior. The revised plan preserves asset bytes, formats, names, paths, case, Unicode, and directory structure. It also retains the current application structure by default. Existing game-facing identifiers are exempt from translation or cosmetic cleanup.

Framework/library updates come first after capturing the original baseline, with minimal required API changes. Establish executable characterization and regression coverage before broader fixes, dependency removal, or refactoring. The initial review's architectural suggestions are options to evaluate under these constraints, not instructions to move resources, split projects, or normalize game data immediately. Finding priorities describe eventual risk; they do not override the agreed execution order.

## Inventory

| Area | Observed state |
| --- | --- |
| Solution | One application project in `Ultimate_Gshop.sln` |
| Framework | `net9.0-windows`; nullable and implicit usings already enabled |
| UI dependencies | Eight direct package references; most Avalonia packages at 11.3.4; ItemsRepeater at 11.1.5 |
| UI organization | 45 C# files; main application behavior concentrated in code-behind |
| Largest files | `Views/MainWindow.axaml.cs`: 1,493 lines; `Views/UserControls/ItemsListControl.axaml.cs`: 575 lines |
| Image assets | 4,554 files under `Resources/surfaces`, approximately 57.33 MiB of uncompressed file contents |
| Tests | No test project found |
| Automation | No tracked GitHub Actions workflow or Dependabot configuration found |
| SDK/build conventions | No `global.json`, central package configuration, or `.editorconfig` found |
| License | GPLv3 text already present; project metadata declares `GPL-3.0-or-later` |
| Authors | Project metadata and README credit João Carlos Ilha Baierle and Lucas Fernando Padilha Stertz |
| Version history | Assembly/package metadata: 1.0.0; tags include `v.1.0.0`, `v.1.0.1`, and `v.1.0.2` |
| Documentation | README in English, but Avalonia badge/version is stale; UI text and comments mix English and Portuguese |
| About/community | No About implementation found; no contribution or security guidance found |

Image sizes above are not an executable package size or runtime memory measurement. Transitive dependencies and vulnerabilities could not be fully assessed without a successful restore.

## Validation attempted

| Check | Result |
| --- | --- |
| Git HEAD before and after relocation | Identical: `ef1aa7762b2c3bfcc2e0444bc214f76c7c2dea7f` |
| Git tracked status after relocation | Clean before adding review documentation |
| Source/project inventory | Completed |
| `dotnet build .\Ultimate_Gshop.sln --configuration Release --no-restore --nologo` | Blocked by missing restore output: NETSDK1004 |
| `dotnet restore .\Ultimate_Gshop.sln --disable-parallel --nologo` | Failed with NU1301: network socket access to `api.nuget.org:443` denied |
| Unit/integration/UI tests | Not run; no test project exists |
| Coverage | Not measured; no percentage is claimed |
| App startup/performance/platform checks | Not run |

The first build attempt also encountered an SDK first-run write restriction in the user's `.dotnet` directory. Repeating with `DOTNET_CLI_HOME` in the writable workspace resolved that restriction. NuGet packages/cache were redirected into `Documents/.ultimate-gshop-audit` for the restore attempt. This did not change the project's target framework or package references. Failed build/restore output under `obj` remains ignored by Git.

## Prioritized findings

Priorities: P0 = protect file correctness before distributing a preview; P1 = required for dependable editing, migration, or public packaging; P2 = maintainability and measured optimization.

### R01 — Server export invokes the client writer (P0)

Evidence: `Views/MenuWindow.axaml.cs:50` handles server export, constructs `gshopsev.data`, then calls `ExportGshopClient.Handle` at line 74. A distinct server writer exists in `Gshop/Services/ExportGshopServer.cs`, with a different record layout.

Action: verify the intended server format against a known-good fixture, route each command to its matching writer, and test the actual export command as well as the standalone writer. Check cancellation and overwrite behavior before creating or replacing destination files.

Acceptance: client and server exports produce independently verified bytes, filenames, and layouts from the same shop fixture.

### R02 — Save does not truncate existing output or protect partial writes (P0)

Evidence: both overloads in `Gshop/Services/SaveGshop.cs:8` and `:16` use `File.OpenWrite`. A shorter new payload can leave old trailing data. The binary file, tracking JSON, and settings JSON are written independently.

Action: use validated, truncating output through a temporary file with a defined replacement/recovery strategy. Define consistency expectations for the associated JSON files and prevent overlapping autosave/manual save operations. Do not describe three independent replacements as a fully atomic transaction.

Acceptance: saving a smaller shop yields the exact expected length; validation or write failure preserves the previous usable file; interrupted writes and recovery have tested behavior.

### R03 — Loading silently changes binary values (P0)

Evidence: `Itens/Services/GetItens.cs:41` resets some status values; line 44 overwrites each loaded sale's control field; line 77 replaces the loaded item place with the loop index.

Action: distinguish lossless parsing from explicitly requested normalization. Confirm the meaning of status, control, and place against actual supported formats. Preserve unknown/reserved values unless a documented format rule requires otherwise.

Acceptance: an unchanged supported fixture can be read and written without unexplained binary differences. Any intentional normalization is documented and tested separately.

### R04 — Item count can disagree with collection contents (P0)

Evidence: `Gshop/Shop.cs:9` stores a mutable `ItemAmount` independently of `Itens`. Export loops trust that count. `ShopTracking.Undo` and `Redo` change the collection without updating it. Single-item deletion does not update the count in the service, while some UI paths maintain it manually.

Action: establish one authoritative count and enforce invariants at edit/export boundaries. Keep the count parsed from a header separate where needed for validation. Check undo/redo, single and bulk deletion, cloning, insertion, and direct grid edits together.

Acceptance: all supported edit sequences keep header count, serialized records, and category offsets consistent; export rejects invalid state without damaging files.

### R05 — Import replaces working files before validating input (P0)

Evidence: `Gshop/Services/ImportGshop.cs:20` and `:21` overwrite the backup and working file, then invoke `LoadGshop`.

Action: parse and validate the candidate first, then commit a new editing session and backup according to a tested recovery policy. Resolve how tracking/settings should reset or migrate when switching shops.

Acceptance: importing malformed or unsupported input leaves the current session, working file, and recoverable backup usable.

### R06 — Parser trusts counts and fixed-width reads (P1)

Evidence: `Categories/Services/GetCategories.cs:14` calculates an offset from an unvalidated signed item count; its subcategory loop uses a `byte` counter against an `int` count at line 29. A count above 255 can wrap that counter. Fixed-width strings use `ReadBytes` without consistently requiring the requested length.

Action: validate header, counts, checked offsets, remaining stream length, category limits, and record widths. Use suitable loop types and exact reads. Define how unsupported variants, negative counts, truncated files, and extra bytes are handled.

Acceptance: malformed files fail with a useful bounded error, without unbounded loops/allocation or partially replacing a session. Document precisely supported format variants; the README's broad `>= v1.7.4` statement is not yet verified.

### R07 — Identity and change comparison are inconsistent (P1)

Evidence: `Item.Equals` compares only game ID. `Itens/Services/UpdateItem.cs:8` finds an entry by the incoming game ID, even though cloning permits duplicate IDs and the ID itself can be edited. Removal uses the ID-based equality. `ItemComparer.Handle` omits ID and uses `Sales.SequenceEqual`; `Sale` has no value equality, while `Item.Clone` creates new sale objects.

Action: define stable editing identity separately from game ID and ordering. Ensure selection, update, deletion, drag/drop, and undo target the intended record. Define deliberate value comparisons for change detection.

Acceptance: editing a game ID or the second record sharing an ID affects only that record; an unchanged deep clone is not reported as a change; sale changes are detected; history preserves the intended target.

### R08 — New-shop defaults do not satisfy writer assumptions (P1)

Evidence: `Gshop/Services/NewEmptyGshop.cs` constructs items with no sales or owner NPC slots. The writers index four sales and eight owner NPC values per item.

Action: define valid construction defaults and validate cardinality centrally, rather than relying on the detail editor to fill them later.

Acceptance: creating a new shop and immediately saving/exporting it succeeds with a format-valid file; invalid programmatically constructed objects fail safely.

### R09 — Cross-platform targeting and resource lookup need explicit work (P1)

Evidence: `Ultimate_Gshop.csproj:6` targets Windows. Assets are copied under `Resources`, while `Misc/ImageUtils.cs:22` looks under lowercase `resources`. Imported icon names can carry Windows path conventions. `Misc/Variables.cs` also contains hardcoded Windows paths, with no usages of those two properties found in the reviewed source.

Action: test a portable `net10.0` target after the framework upgrade and compatibility coverage. Adapt lookup/platform code for case-sensitive filesystems, separators, and non-ASCII paths while retaining actual asset names and game-facing path values. Do not rename directories or rewrite imported paths. Review containment and remove demonstrably unused development paths only as separately covered changes. Verify asset copying during publish, not only build.

Acceptance: packaged apps load real icons on a case-sensitive Linux filesystem as well as Windows/macOS, from a working directory unrelated to the installation directory.

### R10 — Avalonia 12 requires code changes beyond package versions (P1)

Evidence: drag/drop uses `DataObject` and `DragDrop.DoDragDrop` in `ItemsListControl.axaml.cs:259`; clipboard paste uses `GetTextAsync` in `FloatingLabelTextBox.axaml.cs:123`. Most views lack typed binding contexts. The project references `Avalonia.Diagnostics`.

Action: follow the official migration guide for data transfer, clipboard, compiled bindings, diagnostics, third-party controls, and pointer behavior. Test the custom selection, drag/drop, cursor, transparent windows, and keyboard interactions.

Acceptance: the upgraded app compiles and the critical UI flows pass on all advertised targets. See [Avalonia 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes).

### R11 — Several dependencies deserve removal or a narrower role (P2)

Evidence: eight direct references are present. No Avalonia ColorPicker controls or developer-tools attachment were found; the color dialog is custom. ReactiveUI is configured in startup, but no `ReactiveObject`, `ReactiveCommand`, or `WhenAny` use was found. `DynamicData` is used for collection extensions, and observable subscriptions exist in the item control.

Action: evaluate removing ColorPicker and unused diagnostics first. Evaluate replacing small reactive/collection conveniences with existing Avalonia/.NET facilities before removing ReactiveUI and its transitive graph. Keep DataGrid and ItemsRepeater only if their actual feature roles remain useful; do not remove them solely to reduce a number.

Acceptance: record direct/transitive package changes and preserved behavior after restore/build; do not claim specific size savings before measurement.

### R12 — UI code owns application rules and disk operations (P2)

Evidence: `MainWindow.axaml.cs` contains initialization, item/category edits, bulk operations, search, history, save coordination, and input normalization. `DeleteItem` opens a confirmation dialog. `Item` depends on Avalonia bitmap loading. Settings/tracking mix model state with global filesystem persistence.

Action: after characterization tests, make small extractions within existing folders where they improve reuse or isolate a real testing boundary. Preserve the application layout. A Core/Desktop project split is deferred and is not part of the current default plan. Consider separating presentation concerns only when a concrete covered change requires it.

Acceptance: core editing and serialization tests run without an Avalonia application or global user data directories; views delegate complex rules to tested code.

### R13 — Search and list refresh perform avoidable repeated work (P2)

Evidence: `MainWindow.ExecuteSearchAsync` filters the previously filtered `_currentItems`, so broadening a nonempty query may not restore earlier matches. `UpdateGrid` assigns `ItemsSource` and explicitly calls `FillTable`, while an ItemsSource subscription already refreshes. `ItemsListControl.FillTable` sorts and recreates rows, prices, and image references for the entire source. Some selection loops repeatedly call `ItemsSource.ToList()`.

Action: search from a stable category/filter source; define query semantics; consolidate refresh triggers; materialize collections once where necessary; measure responsiveness before deciding on debounce, incremental updates, or a different item control.

Acceptance: narrowing and broadening searches returns correct results; list refresh happens once per logical update; benchmarks use representative shop sizes and record actual improvements.

### R14 — Bitmap lifetime and cache growth have no explicit policy (P2)

Evidence: `Misc/ImageUtils.cs` retains original and scaled bitmaps in static dictionaries without a bound or disposal strategy. Other UI paths create bitmap instances directly. Observable subscriptions and timers also require lifecycle review.

Action: define bitmap ownership, bounded caching, shutdown/session cleanup, and control subscription/timer disposal. Avoid evicting or disposing images while views still use them. Consider lazy decoding only after profiling.

Acceptance: repeated navigation/import/reopen scenarios do not produce unexplained persistent memory growth, and image rendering remains correct after cleanup.

### R15 — Binary helpers duplicate behavior and can split encoded characters (P2)

Evidence: `Gshop/Misc/BytesHelper.cs` has two near-identical fixed-width encoding methods. Both truncate an encoded byte array at an arbitrary boundary.

Action: consolidate the implementation after characterizing the current format. Specify byte limits, null padding, embedded nulls, and truncation/error policy for CP936 and UTF-16, including multibyte characters and surrogate pairs. Reuse modern APIs where their benefits are measured or their validation is clearer.

Acceptance: exact output bytes and widths are covered by boundary tests; documented text handling preserves supported non-ASCII content.

### R16 — Time and price rules need a specified contract (P1)

Evidence: `Item.GetValidPrice` reads the current clock directly and infers weekday/status rules; comments acknowledge uncertainty about `Day`. Header conversion uses an unspecified-kind epoch and a signed 32-bit Unix value. Price conversion/rounding is repeated across editors.

Action: confirm game-format semantics, use an injectable clock such as `TimeProvider` in reusable logic, centralize conversions, and specify UTC/local handling, start/end boundaries, weekday meanings, currency rounding, and overflow behavior.

Acceptance: deterministic tests cover boundary times, inactive/future/expired sales, Sunday/day sentinel rules, rounding, culture, and out-of-range timestamps without silently changing the file contract.

### R17 — Public asset provenance and existing license must be preserved (P1)

Evidence: source already declares GPL-3.0-or-later and credits both authors. The user confirmed that the bundled assets come from the games. No asset-specific redistribution permission record was found in the source inspection; their names/formats/lookup structure must remain intact for compatibility.

Action: preserve source license and attribution, and preserve all existing asset files and lookup contracts during modernization. Establish redistribution rights before publishing the asset payload; treat any distribution alternative as a separate decision rather than replacing, removing, renaming, or converting game assets as incidental cleanup. Review Git history separately before publishing it.

Acceptance: every bundled asset group has a documented source/license or redistribution permission; public release contents match that inventory. No infringement or permission is inferred merely from a filename.

### R18 — Readiness, versioning, and contribution documentation are incomplete (P2)

Evidence: README advertises Avalonia 11.3.3 while references use 11.3.4; version metadata is 1.0.0 despite later tags. The latest commit calls the grid unfinished, and `GridViewModel.Teste` displays `Hello Word` in a largely automatic DataGrid. No tests, workflows, About dialog, or community guidance were found.

Action: decide whether to complete or remove the unfinished grid, remove placeholder content, describe preview status and supported formats honestly, preserve release history, and generate versions consistently. Add the CI, coverage, releases, About, and community documentation described in the plan.

Acceptance: a clean checkout can be built/tested using documented commands; downloads and badges reflect actual validated results; prerelease numbering does not restart blindly at 0.1.0.

## Limits of this review

This is an initial source review, not a completed security audit, licensing clearance, dependency vulnerability assessment, compatibility certification, or performance benchmark. Git history was sampled for context but not exhaustively reviewed for publication. Supported game-format fixtures and executable validation are required before resolving the high-priority findings.
