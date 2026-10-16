# Code Quality and Test Coverage Plan

Updated: 2026-10-16. Two implementation increments of stage 1 are complete locally. The 30% branch milestone is reached; the 35% line milestone and later stages remain pending. No hosted CI run or native UI validation is claimed for these changes.

## Stage 1: Critical editing regressions

Proposed milestone: 35% application line coverage and 30% branch coverage, keeping UI and mapped XAML in the denominator. Raise enforced floors from actual validated measurements rather than setting unreachable gates immediately.

Completed in the first increment:

- Search starts from the shop on every refresh and combines category/subcategory scope with name terms or partial numeric IDs. Shortening/replacing a query restores matching items; edits and history refreshes retain the current search filter.
- Reorder uses the visible list's insertion boundaries, preserves hidden records and existing position slots, and stores a single undo action. Tests verify byte-for-byte undo/redo, actual save/reload, duplicate IDs, invalid persisted indices, and interaction with subsequent edits. Both record order and Place change so the existing loader retains a saved move.
- Bulk updates select records by reference, stage deep clones before modifying the shop, skip duplicate selections and stale references, and retain the existing per-record undo behavior. Failed transformations preserve all records and history.
- Deletion shares the tested ID-plus-Place fallback for detached snapshots, rejects ambiguity, and backs up the actual record. Bulk deletion derives the displayed count from the remaining records.
- Price entry shares bounded conversion and rounding across bulk price changes, gift price, and sale creation. Percentage/multiply/divide operations clamp decimal arithmetic overflow. Gift prices now round halves away from zero rather than truncating. Invalid input still becomes zero; division by zero retains the existing price.
- The advanced bulk sale editor works on a clone until confirmation, and each edited item receives independent sales. The sale table converts stored cents to display units rather than multiplying them again.
- Grid refresh relies on the existing ItemsSource change subscription instead of explicitly rebuilding the table twice.

Validation: Release build with warnings as errors, expanded application tests with coverage, coverage policy enforcement, and exact resource hashes. Current numbers are recorded in [testing.md](testing.md).

Completed in the second increment:

- Cloning prepares independent copies, deduplicates selected references, ignores stale records, allocates unique positions once, and rejects exhausted integer positions before any insertion. Tests include duplicate game IDs, cloning the live list, and actual session save/reload with undo/redo.
- Selection uses record references and displayed order for Shift/Ctrl/Ctrl+Shift, maintains a stable range anchor, removes duplicate selections, and resets stale references when the source refreshes. The control sends selection snapshots to asynchronous bulk handlers and raises one item event per click. Repeated ToList/ID-based lookup inside range loops is removed; native pointer delivery and visual behavior remain unvalidated.
- Category/subcategory reset is one reversible action and synchronizes ItemAmount. A complete item snapshot preserves record positions and prior index-based history, including after session reload; its memory cost grows with shop size and remains a performance measurement item.
- Deleting an empty subcategory shifts later subcategory indices in the same category. Metadata and affected items are restored together by undo/redo; nonempty and invalid scopes cannot change data or history.
- Category rename and subcategory add/rename now participate in persisted history, preventing later metadata edits from bypassing it. No-op/invalid edits preserve redo. Existing name handling and the eleven-subcategory UI limit are retained.
- History undo/redo recalculates item counts, and the window rebinds category sources after history changes.

Local Windows validation after this increment: 256 tests pass; 969/3,986 lines (24.31%) and 512/1,524 branches (33.60%) are covered. All 4,580 resource paths/hashes remain unchanged. The CI floors are raised to 24%/33%; hosted platform validation remains pending.

Remaining before declaring stage 1 complete:

- Broader delete/move sequences and combinations of bulk operations with persisted history.
- Confirmed game-specific sale/time/status boundaries and additional malformed parser/recovery cases.
- Progress toward the proposed 35% line milestone; the proposed 30% branch milestone is reached. Native drag/drop, key event delivery, dialogs, and visual selection have not been exercised by these service tests.

## Stage 2: Organization and quality checks

Proposed milestone: 60% lines and 50% branches. Extract small editing/validation operations from code-behind within the existing folders. Add headless binding/command tests, consistent interface error handling, timer/subscription lifecycle checks, .editorconfig and focused analyzers. Decide whether to complete or remove the experimental grid. Preserve assets, public data contracts, and the single application project.

## Stage 3: Measured performance and delivery validation

Final coverage target: 80% lines and 70% branches. Establish repeatable measurements of search, selection, list projection, save latency and memory on representative shop sizes. Use results to guide materialization, image cache ownership, virtualization and asynchronous save changes with safe snapshots/concurrency. Validate packaged desktop startup and edit/import/export flows on Windows, Linux and macOS, including independently verified game fixtures. Set performance budgets from measured baselines.

Service tests do not establish native UI correctness, complete game compatibility, or a validated distributable release. See [persistence.md](persistence.md) for session format compatibility and [releasing.md](releasing.md) for packaging requirements.
