# Coverage Scope Audit

Reviewed on 2026-10-16 for the requested 40% line coverage milestone, with 80% remaining the final line coverage target.

## Decision

Keep the current whole-application measurement. No new exclusions were necessary or added to [application-coverage.config](../tests/application-coverage.config). The application assembly is measured directly through the test project's ProjectReference.

| Source | Decision | Reason |
| --- | --- | --- |
| Tests | Already excluded | Test code is not application behavior; the assembly include rule also restricts measurement to Ultimate_Gshop.dll. |
| Build output under obj | Already excluded | Generated build glue and generated regex implementation are not handwritten application logic. |
| Application C#, including Views/*.axaml.cs | Included | Editing rules, validation, event handlers, bindings, persistence and UI behavior need regression coverage. |
| Authored .axaml mapped by the compiler | Included | The compiler maps !XamlIlPopulate and template Build methods to the project's markup. These declarations contain bindings, templates and resources that can fail at runtime and can be exercised headlessly. Keeping them also preserves the earlier measurement contract. |
| Program, App and InstallationCheck | Included | They are executable production entry points and diagnostics, even where untested. |
| Async state machines and closures mapped to application C# | Included | They implement handwritten async handlers and rules. Blanket CompilerGenerated exclusions would hide real application behavior. |
| PNG assets, documentation and PowerShell scripts | Outside the measured assembly | They have no C# executable sequence points in this report. Removing them would not increase application coverage. Asset integrity is checked separately against the SHA-256 baseline. |
| External dependencies, including the test-only headless backend | Outside the measured assembly | The module filter includes only Ultimate_Gshop.dll. |

The pre-change 256-test report measured 969/3,986 lines (24.31%) and 512/1,524 branches (33.60%). Of those lines, 947 were mapped to XAML and only 12 were covered. Although the generated mapping could justify a separate handwritten-C# metric, removing it would change the denominator and would not establish UI correctness. It remains in the primary metric.

## How the milestone was reached

Headless tests exercise the real compiled application controls, templates and bindings on a dedicated Avalonia UI thread. A test-only Avalonia.Headless dependency replaces native windowing; Skia is retained for the existing actual raster decoding checks. This follows [Avalonia's manual headless setup](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform) and uses the existing xUnit v3/Microsoft.Testing.Platform runner.

Tests assert behavior: both binding directions, text filtering, label/date synchronization, tagged explanation round trips, dialog results and cancellation, cascading selections, sale creation and advanced price form operations. They reproduced date/label defects and incomplete dialog tasks before the corresponding fixes. They do not merely construct controls to raise a percentage.

The 40% milestone run passed all 290 tests and measured **1,784/3,997 lines (44.63%)** and **719/1,542 branches (46.63%)**, using the unchanged collector scope. Its CI floors were 40% lines and 45% branches.

The first stage 2 increment passed all 300 tests and measured **2,924/4,060 lines (72.02%)** and **944/1,582 branches (59.67%)**. It tests initial MainWindow editing/search/history/session failure behavior and the read-only grid, including their real markup/bindings.

The second stage 2 increment passed all 331 tests and measured **3,385/4,115 lines (82.26%)** and **1,085/1,622 branches (66.89%)**. Menu/options and real bulk workflows add assertions for cancellation, errors, backups, retry, draft preservation and history. Current CI floors are **80% lines and 65% branches**. The 80% line target is reached; the 70% branch target remains pending. Small denominator changes reflect added production fixes. Collector exclusions are unchanged.

The stage 3 increment passes **375 tests** and measures **3,633/4,179 lines (86.93%)** and **1,189/1,666 branches (71.37%)**, including category/advanced-price forms, async-save ordering, full snapshot isolation and text-reader regressions. Both final targets are reached locally, and the current floors are now **80% lines / 70% branches**. No collector exclusions were added. Denominator changes also reflect extraction of duplicated sale normalization and the smaller immutable grid projection.

After the image-cache/virtualization follow-up, **380 tests** pass with **3,653/4,203 lines (86.91%)** and **1,202/1,682 branches (71.46%)**. The same whole-application scope and 80%/70% floors apply; the added production lifecycle branches remain in the denominator.

Native desktop startup, OS clipboard/file dialogs, real drag/drop, complete menu/navigation and bulk workflows, packaged execution and independent game-format compatibility remain separate validation work. MainWindow, menu, options, and diagnostics stay in the denominator even where untested. MainWindow tests use an injected session in a private temporary directory and do not write application preferences/history to the user's session directory. Hosted Linux/macOS verification is pending.
