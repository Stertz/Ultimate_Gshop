# Ultimate Gshop Modernization and Open Source Plan

Status: Local validation now passes 380 tests with 86.91% line / 71.46% branch coverage; both final targets are met without excluding UI/XAML. Stage 3 includes reproducible before/after performance measurements, optimized load/search/grid projection and safe asynchronous editor saves. See [performance](performance.md), the [three-stage quality plan](quality-plan.md) and [current validation](testing.md). Hosted validation, native UI/game-fixture acceptance and the remaining profiling/distribution items are still tracked explicitly.
Updated: 2026-10-16.
Source baseline: `ef1aa7762b2c3bfcc2e0444bc214f76c7c2dea7f` from `joaocib/Ultimate_Gshop`.
Public destination created by the user: `Stertz/Ultimate_Gshop` (`main`). Local `origin` targets it; the original repository remains `upstream`. Original Git history and João's README attribution are retained. Initial publication and the six platform checks have completed.

## Intended outcome

Publish a new public repository containing a reviewed, maintainable .NET/Avalonia desktop editor for Perfect World Gshop files. Provide meaningful automated tests, visible measured coverage, automated prereleases for Windows/macOS/Linux, and clear English documentation that enables community contributions. The application requires no deployment server.

Code, comments, application text, technical messages, documentation, contribution materials, commits, and release notes should use English. Existing internal identifiers can be translated only where that is a small, tested change. Game asset names, paths, case, formats, serialized keys, and saved data contracts are compatibility requirements and are exempt from translation or cosmetic renaming.

The [source review](source-review.md) contains the inspected inventory, evidence, priorities, attempted checks, and limitations. Its findings R01–R18 drive this plan.

## Agreed compatibility boundaries

The bundled assets come from the games, as confirmed by the user. Their lookup and naming conventions are not fully documented. Preserve their existing directory structure, exact relative paths, filenames, letter case, non-ASCII characters, formats, and bytes. Do not rename, move, re-encode, resize, recompress, convert, replace, deduplicate, or delete them as part of modernization. Preserve any DDS-to-PNG lookup mapping until its behavior is characterized.

Keep the existing application folder layout and single-project structure by default. Add test projects, documentation, and automation alongside it. Do not introduce `src/`, split Core/Desktop projects, rename `Itens` or `Docs`, or move resources merely to match a convention. Reconsider only a narrowly justified change after tests show what must be preserved.

Preserve binary record order/width, encoding, padding, reserved values, output filenames, and persisted settings/history contracts. Any necessary OS adaptation belongs in lookup/platform code and must retain the game-facing values. Cross-platform packaging must not silently rewrite those contracts.

The user's latest execution direction is to update frameworks/packages first, preserving assets, then establish tests before improvements. Record any available original evidence without making a complete old-app executable baseline a prerequisite to preparing stack changes. The initial asset inventory was captured before edits; subsequent behavioral fixes follow executable regression tests. Whole-application old/new equivalence is still unverified; synthetic tests against the updated application alone cannot establish it.

The user subsequently deprioritized interface tests for the first phase. Prioritize data correctness, executable regression tests, persistence, and maintainability now. Native UI checks remain a later distribution-validation task and do not block covered data-layer improvements.

## Decisions grounded in the checkout

| Area | Current state | Intended direction |
| --- | --- | --- |
| Framework | .NET 9, Windows target | Supported .NET 10 LTS, portable `net10.0` target after API review; pin SDK |
| Avalonia | Mostly 11.3.4; ItemsRepeater 11.1.5 | Compatible stable release; 12.1.4 was the latest checked; migrate bindings/data transfer deliberately |
| License | GPLv3 text and GPL-3.0-or-later metadata already present | Preserve existing source license and both authors; audit asset redistribution separately |
| Structure | One project, UI-heavy models/services | Retain existing projects/folders; add tests alongside; prefer small extractions within existing boundaries |
| Dependencies | Eight direct references | Update compatible versions first; evaluate optional removals after regression coverage exists |
| Tests and coverage | No test projects or measurement | Unit, integration, and headless UI tests; real line/branch reports and enforced gates |
| Versioning | 1.0.0 metadata with tags through v.1.0.2 | One version source, preserve prior tags, choose next prerelease after compatibility review |
| Assets | 4,554 game surface files, about 57.33 MiB | Preserve exact files/names/paths/formats; characterize lookup; adapt platform code only with tests |
| Hosting | No existing workflows | Standard GitHub-hosted runners and GitHub Releases |

Existing nullable analysis is already enabled. Do not present enabling it as new work. Broad format compatibility and cross-platform execution remain unverified.

## Stage 0 — Baseline and publication readiness

- [x] Locate the supplied checkout and flatten the duplicated directory while preserving Git metadata and commit.
- [x] Read source/project configuration, inventory dependencies and assets, and identify existing license/version history.
- [x] Review Odemu's local reporting conventions: Cobertura/HTML, artifacts, PR summary, README coverage badge, and automated releases.
- [x] Attempt the baseline build and restore; record NETSDK1004 and network-blocked NU1301 without claiming a successful build.
- [x] Verify the original nested folder is absent on the final filesystem check.
- [ ] Restore and build the original checkout in an environment with NuGet access; record SDK, warnings, failures, and startup behavior.
- [ ] Obtain known-good, redistributable client/server fixtures for explicitly supported game-format versions; include non-ASCII content and unknown/reserved fields.
- [ ] Record original-revision outputs and workflows before stack upgrades: import, unchanged save, client/server export, editing, undo/redo, search, and real icon lookup. Retain the producing commit, parameters, and expectations with each fixture.
- [x] Record exact relative paths, sizes, and SHA-256 hashes of all 4,580 resource files before stack edits; verify source files and all 4,554 game surfaces in local publish output. Native packaged execution remains pending.
- [ ] Measure startup, representative file load/save/search, memory use, and package size under recorded conditions.
- [ ] Review the exact history, credentials/private configuration, authorship, and asset provenance before public publication. Preserve `origin` as the source reference until the new destination is decided.
- [ ] Turn the review findings into small implementation tasks with their acceptance criteria and regression scenarios.

Exit: executable baseline or precisely recorded blockers; authoritative fixture expectations; known publication constraints. Source inventory is complete; runtime and publication clearance are pending.

## Stage 1 — Update framework, libraries, and build tooling

Limit this stage to version/build changes and the minimum API adaptations required by the update. Keep bug fixes, broad refactoring, optional dependency removal, and format normalization in later stages. Distinguish prepared changes from validated full-application behavior while package restoration is unavailable.

- [x] Pin .NET SDK 10.0.402 and use the portable `net10.0` target. Full solution Release build passes with warnings treated as errors; native startup remains pending (R09).
- [x] Update Avalonia and compile required API migrations for clipboard, drag/drop, diagnostics, bindings, and pointer events (R10). Native interactions remain a separate validation gate.
- Package restoration and compilation pass. Actual DataGrid/ItemsRepeater behavior and native UI workflows remain unvalidated.
- [ ] Verify DataGrid and ItemsRepeater compatibility before committing to the final package set. Do not assume every separate control package shares Avalonia's core version.
- [ ] Use reproducible NuGet restoration and pinned local tools without reorganizing the application to introduce a new package-management convention.
- [ ] Keep the current dependency roles where compatible. Handle a removed upstream package/API as an explicit migration task; postpone optional removals until tests cover their callers (R11).
- [x] Preserve resource include/copy patterns and compare source and local published game assets with the original baseline.
- [ ] Check build/startup and the recorded workflows after each framework or compatible package-group increment. Compare resulting file bytes and asset lookup with the original revision, without inventing new expected output.
- [x] Remove the unused, discontinued diagnostics package without introducing a paid tooling requirement.

First-phase stack evidence: successful full compilation, package restoration, executable data tests, and unchanged asset inventory. Native startup and interaction equivalence remain recorded later validation gates before claiming desktop compatibility.

## Stage 2 — Comprehensive automated tests and coverage

Prefer a compatible xUnit v3 setup for new tests, with Avalonia headless support for control behavior. Verify the test runner and coverage collector together; choose compatible Microsoft.Testing.Platform or VSTest tooling and document the actual commands.

| Test group | Required scenarios |
| --- | --- |
| Binary unit/characterization | Header and record widths; CP936/UTF-16/null padding; unknown fields; exact expected client/server bytes; bounded malformed/truncated input |
| Asset compatibility | Exact paths, case, filenames, Unicode, formats, hashes, original lookup mapping, fallback, and packaged file availability; no asset transformation |
| Editing unit tests | Duplicate game IDs; changing IDs; unchanged deep clones; sales comparison; create/edit/delete/clone/reorder; single and bulk operations |
| History tests | Undo/redo sequences; collection counts and identity; redo invalidation; saved/reloaded stack order; shop switching |
| Time/price tests | Injectable clock; start/end boundaries; confirmed weekday/status rules; UTC policy; currency rounding; overflow and cultures |
| Persistence integration | Shorter-file overwrite; failed/interrupted save; validated import; corrupt settings/tracking JSON; recovery; temporary isolated directories |
| Headless UI | Binding validity; commands; discard prompts; navigation; search broadening; selection; clipboard validation; reorder interactions |
| Packaged smoke tests | Startup, real asset lookup, basic edit/save and correct client/server export on every advertised target |

- [x] Add a test project alongside the existing application and reference the compiled application, excluding test sources/resources from the root application project.
- [x] Run 149 compatibility tests covering synthetic serialization, persistence, editing, history, assets, and raster decoding. This replaces the earlier source-linked bootstrap suite; native UI coverage remains pending.
- [ ] Turn original-revision fixtures into golden-file/differential tests. Require exact bytes where the format is deterministic. If a field varies, document and test that field separately rather than broadly masking differences.
- [ ] Keep characterization of current behavior separate from correctness expectations. Known defects remain documented; add a failing regression scenario before fixing one, then make only its intended expectation change.
- [ ] Include regression coverage for R01–R10 and R16, then broaden to other important workflows.
- [ ] Use synthetic or cleared fixtures; run deterministically without production services, developer paths, or required credentials.
- [x] Generate Cobertura and standalone HTML locally, with test counts and whole-application line/branch percentages. Hosted publication remains pending.
- [ ] Report global and core/module coverage separately. Define the denominator, include handwritten UI logic where measurable, and narrowly exclude generated code and test assemblies.
- [x] Establish whole-application baseline: 711/3,861 lines (18.41%), 289/1,396 branches (20.70%). Enforce initial floors of 18.0%/20.0%, retaining untested UI in the denominator; end-of-stage targets remain 80%/70% and have not been reached.
- [x] Implement checks rejecting missing, empty, inconsistent, wrong-assembly, and below-floor coverage reports. Required hosted check settings remain pending; a percentage alone is not proof of correct behavior.
- [ ] Publish the report in workflow summaries and artifacts, and derive the README coverage badge from actual successful default-branch measurements.

Exit: documented local/CI commands execute meaningful tests; original supported behavior and asset contracts are covered; reports show real measurements; coverage gates demonstrably work. Initial measured coverage is available, but the stage remains incomplete. Unit tests do not replace independently verified game-format fixtures and packaged compatibility checks.

## Stage 3 — Fix and simplify behind regression tests

Only start a behavioral change after its relevant characterization/regression checks are executable. Preserve the current folder and asset structure throughout.

Implemented locally after failing regression tests: recoverable binary/JSON saves (R02), actual export collection counts and persisted stack order (R04 partial), value-based comparison and unambiguous update identity (R07 partial), valid new-shop slots (R08), and portable asset lookup with real raster tests (R09 partial). Full model/count consistency, delete/selection identity, physical interruption scenarios, and native OS execution remain pending. See [migration status](stack-update.md).

Further implemented: bounded parser counts/exact fields and nonwrapping subcategory loops (R06), validation before import replacement and preference-preserving history reset (R05), protected client/server file routing (R01; native command/independent fixtures pending), shared fixed-width encoding without contract changes (R15), and removal of the unused ColorPicker dependency (R11 partial). Resolved package count is 43, down from 44 immediately before removal. Remaining reactive dependencies are retained pending caller coverage.

Recovery and session creation have failure rollback/restart fixtures, lock/concurrent-write checks, and published-executable installation diagnostics. See [the persistence protocol](persistence.md) for the exact guarantees; physical power-loss/full-disk and native UI validation remain pending.

- [ ] Fix client/server command routing (R01), truncation/recovery (R02), unintended load normalization (R03), and count/history consistency (R04), one issue per reviewable change. Confirm intended game behavior with independent fixtures before changing serialized output.
- [ ] Protect imports/session state (R05); validate parser bounds/exact reads (R06); resolve editing identity/comparison (R07); establish valid new-shop defaults (R08); specify time/price rules (R16).
- [ ] For each fix: reproduce the issue, establish the expected game-compatible result, add the regression test, implement the smallest fix, and rerun affected characterization tests. Record any intentional behavior difference explicitly.
- [ ] Extract small reusable methods in existing folders when they remove actual duplicated rules. Reuse the current code before introducing interfaces, new layers, or a project split (R12).
- [ ] Consolidate fixed-width encoding only after tests define CP936/UTF-16, padding, truncation, and null behavior; preserve game record sizes (R15).
- [ ] Reduce repeated list refresh/materialization and fix search behavior using tested scenarios; measure responsiveness on representative data (R13).
- [ ] Review bitmap cache ownership and subscription/timer cleanup without changing source image files or their lookup identifiers (R14).
- [ ] Evaluate optional dependency removal after coverage exists and rerun all affected flows (R11).
- [ ] Apply modern .NET APIs and performance improvements only with before/after results. Defer trimming, AOT, and repackaging that would change resource layout unless separately validated.
- [ ] Add `.editorconfig`, relevant analyzers, and English comments explaining intent. Preserve existing folder names; make any internal symbol cleanup small and separate from behavior changes. Resolve unfinished grid scope based on its actual behavior and tests (R18).
- [ ] Introduce portable `net10.0` targeting and the minimum OS-specific path adaptations as separate changes. Fix lookup code rather than renaming or moving assets; retain imported game-facing path values (R09).

Exit: intended fixes have reproducing regression tests; existing supported workflows still pass; game-format and asset contracts remain intact; claimed improvements have evidence. A compatibility failure pauses the affected change rather than being accepted by updating a snapshot to match it.

## Stage 4 — GitHub-hosted CI and automatic prereleases

### Pull requests and dependency maintenance

- [ ] Validate restore, Release builds, formatting, analyzers, tests, and coverage with required checks.
- [ ] Run appropriate platform checks on standard hosted Windows/macOS/Linux runners. Pin supported OS labels/toolchains deliberately; separate fast core checks from package validation.
- [ ] Configure cache keys, bounded retention, timeouts, and concurrency cancellation.
- [ ] Run normal community fork checks without secrets or write permissions. Use summaries/artifacts for reports; optional PR comments must not require granting privilege to untrusted code.
- [x] Prepare Dependabot for NuGet and GitHub Actions with weekly scheduling, PR limits, compatible groups, and separate major updates. Activation awaits publication.
- [ ] Enable applicable vulnerability alerts, secret scanning, and supported code scanning. Require checks for dependency updates and repository merges.
- [ ] Keep workflow permissions minimal; isolate release writes; pin external actions to reviewed versions/commit SHAs and update them through Dependabot.

### Versions and release artifacts

- [x] Prepare `1.1.0-alpha.<successful-CI-run-number>` after existing tags through `v.1.0.2`; local builds are `alpha.local`. Existing history is preserved and no stable release is claimed.
- [x] Use one project version prefix for assemblies, tags, package names, metadata, and notes. Prerelease identity is derived from the successful CI run; commit-convention release-note generation remains optional follow-up.
- [x] Prepare release jobs gated on successful default-branch push CI and checkout the exact checked source commit for every package. Actual hosted execution remains pending.
- [x] Connect the CI `workflow_run` trigger, versioning, packaging, and final prerelease publication explicitly; tag creation is not used as an implicit workflow trigger.
- [ ] Start with Windows x64, Linux x64, and macOS ARM64/x64 where native dependencies and runnable hosted validation support them. Add Windows/Linux ARM64 after actual execution coverage is available.
- [x] Prepare self-contained Windows ZIP, macOS `.app` tarballs, and Linux tarball scripts/workflow. Native hosted execution and archive/download verification remain pending; installers/AppImage/DMG are deferred.
- [ ] Validate startup and a critical file workflow from packaged output on every advertised OS/architecture; distinguish tested execution from cross-compilation.
- [ ] Stage all required assets and publish a prerelease only after successful checks. Upload checksums, source/build instructions, license and asset notices, supported platform prerequisites, commit/version metadata, and known limitations.
- [ ] Evaluate Windows signing and macOS signing/notarization separately. Document unsigned preview installation if deferred; certificates/accounts can have costs.
- [ ] Execute the workflow on the actual new GitHub repository and verify the published downloads. Keep stable release promotion separate until readiness criteria are met.

Exit: hosted required checks run successfully; Dependabot is active; a real automatic prerelease provides validated Windows/macOS/Linux downloads and corresponding source. No deployment server is needed.

Standard GitHub-hosted runner execution is currently free for public repositories. Larger runners, artifact/cache storage, and optional signing/services have separate limits or costs. Use bounded retention and avoid unnecessary external infrastructure.

## Stage 5 — English documentation, About, and community

- [ ] Rewrite README around actual verified behavior: purpose, preview status, supported formats/platforms, screenshots, downloads, installation, OS prerequisites, source builds, tests, coverage, and known limitations.
- [ ] Add development, architecture, testing, release, and roadmap documentation; validate commands using a clean checkout.
- [ ] Preserve GPL-3.0-or-later and both original authors. Add third-party asset/dependency notices based on confirmed rights; do not assume source licensing automatically covers bundled game art (R17).
- [x] Add `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, preview `SECURITY.md`, issue templates, and a focused PR template. Repository activation and a confirmed private reporting channel remain pending.
- [ ] Configure GitHub About with an English description, relevant topics, useful link, license, and release information.
- [ ] Add/review the application About dialog: title, version, preview status, source link, authors, license/notices, issue reporting, and release notes.
- [ ] Replace stale badges and placeholders with data generated from actual CI and release results. Keep coverage updates from causing recursive release/CI loops.
- [ ] Present unresolved work as a public roadmap and label contribution opportunities where useful.

Exit: a new contributor can build/test/propose a change from the documentation, and a user can choose and run the correct preview download.

## Folder organization: preserve the existing layout

```text
Ultimate_Gshop.csproj
Ultimate_Gshop.sln
Categories/
Gshop/
Itens/                            # Preserve existing folder/namespace initially
Misc/
Resources/                        # Preserve assets and every existing relative path
ViewModels/
Views/
tests/
  Ultimate_Gshop.Format.Tests/   # Existing compiled-application compatibility suite
  UltimateGshop.Desktop.Tests/   # Add only when needed for native/headless UI tests
Docs/                            # Preserve current case and screenshot paths
  source-review.md
  modernization-plan.md
  architecture.md
  development.md
  testing.md
  releasing.md
  roadmap.md
.github/
  workflows/
  ISSUE_TEMPLATE/
  dependabot.yml
  pull_request_template.md
```

Only add test projects that are needed. Because the application project remains at the repository root, explicitly exclude `tests/**` from its default source/resource/XAML discovery as appropriate; otherwise test code can be compiled into the application. Verify solution membership and output contents. The example adds supporting material without moving current application files or assets.

## Implementation sequence and completion evidence

Use reviewable increments following the latest direction: update .NET and compatible package groups with minimum API migration while preserving asset structure; establish characterization/unit/integration/headless tests; then fix one confirmed issue or simplify one covered area at a time; validate portable execution/lookup; establish complete hosted CI/coverage and package automatic prereleases; complete community documentation and public settings. Original evidence remains useful where available. Initial CI and Dependabot files are prepared; actual hosted execution remains pending.

Documentation and minimal CI should evolve alongside the code; the stages describe priorities, not a requirement to postpone all automation. Before the first public push, review the exact content/history/assets to be published. Preserve the source remote as upstream when the new public destination is configured.

Final completion requires the resolved/deferred finding list, successful build/test commands, real line/branch percentages, justified dependency/performance results, actual hosted CI links, and verified release download links. Implementation alone does not establish validation. Until those steps run, report the remaining limitations explicitly.

## References

- [Source review and local check results](source-review.md)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Avalonia release checked: 12.1.4](https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.4)
- [Avalonia 12 migration requirements](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
- [Avalonia headless testing](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform)
- [GPLv3 terms](https://choosealicense.com/licenses/gpl-3.0/)
- [GitHub Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- Local reference: Odemu CI/dependency configuration was inspected, not executed or copied with its server deployment infrastructure.
