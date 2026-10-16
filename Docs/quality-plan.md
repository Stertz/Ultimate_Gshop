# Code Quality and Test Coverage Plan

Updated: 2026-10-16. Local coverage targets are met: 375 tests, 86.93% lines / 71.37% branches, with enforced floors of 80%/70%. Stage 3 now includes measured search/load/grid optimizations and asynchronous editor saves with detached snapshots. Hosted package execution, native UI/real-game acceptance and the profiling items listed below still determine completion of the full plan.

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

Completed in the third increment (the requested 40% milestone):

- Audited exclusions and retained the existing whole-application scope, including mapped XAML, all code-behind, entry points and diagnostics. Existing test/obj exclusions remain sufficient; see [the scope audit](coverage-scope.md).
- Added a test-only headless backend and dedicated UI dispatcher, retaining real Skia raster decoding and the compiled application reference. Tests now verify controls, bindings, rendered sale rows, dialog responses/cancellation and sale form operations.
- Fixed date-property synchronization with the inner DatePicker, including SetValue/binding changes and date clearing. Label visibility now follows blank text in date and combo controls.
- Fixed custom dialog tasks that remained pending when a window closed without a button. Closing completes each mode as cancellation; task continuations run asynchronously. Clone/delete confirmations now require an explicit positive result.
- Verified actual form price conversion, duration splitting and advanced operations, preserving the current date representation contract without claiming game-specific semantic correctness.

Validation: 290 tests pass; 1,784/3,997 lines (44.63%) and 719/1,542 branches (46.63%) are covered. Release build passes with warnings as errors, and all 4,580 asset paths/hashes are preserved. CI floors now enforce 40% lines / 45% branches. The collector scope is unchanged.

Remaining functional work from stage 1:

- Broader delete/move sequences and combinations of bulk operations with persisted history.
- Confirmed game-specific sale/time/status boundaries and additional malformed parser/recovery cases.
- The proposed 35%/30% milestone and the additional 40% line goal are reached. Initial MainWindow pointer selection, edit/create/delete, search and history now have headless coverage; native drag/drop, OS key/clipboard delivery and complete navigation remain unvalidated.

## Stage 2: Organization and quality checks

Proposed milestone: 60% lines and 50% branches. Extract small editing/validation operations from code-behind within the existing folders. Add headless binding/command tests, consistent interface error handling, timer/subscription lifecycle checks, .editorconfig and focused analyzers. Decide whether to complete or remove the experimental grid. Preserve assets, public data contracts, and the single application project.

Completed in the first stage 2 increment:

- MainWindow accepts explicit shop/history/settings/session-directory dependencies through an internal constructor. The default constructor retains the existing startup flow. Headless tests use real controls/events and temporary session files, without touching the user's session.
- Search broadening, item creation, pointer selection, edit/discard/delete with duplicate IDs, undo/redo, and session save/reload now have window-level assertions. Individual deletion synchronizes ItemAmount in both UI entry points.
- Autosave and close share one guarded save path. Recoverable I/O/serialization failures show an error and retain the window/session for retry; overlapping saves and close attempts cannot open duplicate error dialogs. Unopened or canceled startup cannot create empty session files. Default startup also presents recoverable session loading errors, although the real startup/menu path remains unvalidated.
- Closing only commits the closed state after a successful save. Canceled discard leaves the editor open. Save timers stop/unsubscribe on close, external history notifications unsubscribe, and item auto-scroll stops/unsubscribes on visual detachment. Self-owned property subscriptions remain active for a reusable control's lifetime rather than being disposed on temporary detachment.
- Autosave respects the existing minimum thirty-second interval and is reconfigured after returning from the menu. Disk I/O still runs synchronously; asynchronous snapshot saving and timing measurements remain stage 3 work.
- The experimental grid is now a labeled read-only snapshot with six explicit columns and no prototype button. It clones item/sale/owner data, so inspecting it cannot mutate the shop or bypass undo history.
- Added .editorconfig with focused correctness rules CA2012/CA2013/CA2016/CA2200 and pinned built-in .NET analyzer level 10.0. No repository-wide reformat or source encoding conversion was performed.

Validation: 300 tests pass, Release build with warnings as errors passes, and all 4,580 asset paths/hashes are unchanged. Coverage is 2,924/4,060 lines (72.02%) and 944/1,582 branches (59.67%) with the unchanged whole-application scope. CI floors now enforce 60%/50%; the 80%/70% final targets remain pending.

Remaining in stage 2:

- Native OS picker behavior and platform error presentation; headless import/export/menu/options cancellation/failure paths now have regression tests.
- More category/advanced-price interactions through the actual MainWindow, smaller extractions of remaining duplicated editor payload rules, and deeper timer/reattachment validation.
- Hosted Windows/Linux/macOS execution of the expanded headless suite and raised floors.

Completed in the second stage 2 increment:

- Menu operations share guarded error handling with the editor. Import/export/new/continue/options actions use the supplied session directory, preventing navigation from silently reverting to the default session root.
- Real menu button tests verify distinct client/server bytes, session saves, overwrite cancellation, picker/I/O failures and retry, valid/malformed imports, backup failure preservation, new/continue sessions, and startup options with no active shop. Picker functions are injected in these tests; actual OS picker windows remain unvalidated.
- Canceling import selection no longer creates a backup or changes preferences. Imports validate the private snapshot before invoking the backup callback and committing the new session. A failed backup prevents replacement, preserves history/preferences, and cleans the import snapshot. Backup folders use unique names to avoid same-second collisions.
- Closed menus ignore late picker results; concurrent menu actions cannot duplicate dialogs. The native file helper returns cancellation directly instead of opening a recursive retry prompt.
- Options edit a separate draft. Invalid intervals and I/O failure retain the dialog and live preferences; only successful protected persistence copies the chosen preferences to the live settings. Closing without save discards the draft. Options opened from the menu are owned/modal.
- Real bulk forms now cover amount, period, frequency, price, clone/delete and category moves with duplicate game IDs, hidden records, counts and undo/redo. All bulk entry points request discard confirmation before abandoning an edited draft. Closing advanced prices returns without an apply confirmation or changing sales.
- Common recoverable-error classification/presentation moved into OperationErrors rather than being duplicated in each window.

Validation: 331 tests pass, Release build with warnings as errors passes, and all 4,580 asset paths/hashes are preserved. Coverage is 3,385/4,115 lines (82.26%) and 1,085/1,622 branches (66.89%) with the unchanged whole-application scope. CI floors now enforce 80% lines / 65% branches. The final line target is reached; the 70% branch target and stage 3 performance/distribution work are not complete.

## Stage 3: Measured performance and delivery validation

Final coverage target: 80% lines and 70% branches. Establish repeatable measurements of search, selection, list projection, save latency and memory on representative shop sizes. Use results to guide materialization, image cache ownership, virtualization and asynchronous save changes with safe snapshots/concurrency. Validate packaged desktop startup and edit/import/export flows on Windows, Linux and macOS, including independently verified game fixtures. Set performance budgets from measured baselines.

Service tests do not establish native UI correctness, complete game compatibility, or a validated distributable release. See [persistence.md](persistence.md) for session format compatibility and [releasing.md](releasing.md) for packaging requirements.

Started locally on 2026-10-16 after commit `b82060e`: a standalone Release performance harness and sequential measurement script now exercise actual search, selection, grid projection, transactional save and reload with 1k/10k/50k synthetic shops. Reports retain individual timing/allocation samples and environment metadata. See [performance.md](performance.md) for methodology, baseline findings and limits. This starts measurement work; native rendering, retained memory, optimizations and packaged platform validation remain pending.

Delivered in the stage 3 implementation increment:

- Editor autosave captures detached items, categories, both history stacks and settings on the owning UI thread; serialization, flushing and transactional replacement then run on a worker. Concurrent saves are rejected. Close waits for an active autosave and writes a fresh generation; failure keeps the editor open. Navigation pauses autosave and waits before opening import/export/options, preventing an older snapshot from overwriting a new session.
- Snapshot tests mutate all nested live data after capture and verify exact saved bytes, stack order and persisted undo/redo. Controlled outstanding writers verify dispatcher delivery, duplicate requests, close retry and navigation ordering.
- Fixed-width item text decoding reuses stack buffers, preserving CP936/UTF-16 fallback, null handling and truncated-field rejection. Search avoids creating a name-match closure per item. The grid projects immutable six-column rows instead of cloning unused sale/owner data.
- Real category forms cover rename, no-op/duplicate/blank/cancel, subcategory limits/deletion and reset, with persisted undo/redo. Advanced-price confirmation/rejection now runs through the actual sale form and editor; common four-slot sale normalization is extracted and tested.
- Two sequential post-change performance runs quantify the improvements and the remaining UI snapshot cost; see [performance.md](performance.md). Release packaging now verifies the archive checksum, extracts it, checks build metadata/assets and executes the extracted installation diagnostic on the matching native runner.

Local acceptance: 375/375 tests pass, 3,633/4,179 lines (86.93%) and 1,189/1,666 branches (71.37%) are covered, Release builds with warnings as errors pass, and all 4,580 resource paths/hashes are unchanged. Collector scope is unchanged. CI now enforces both final coverage targets.

Remaining completion criteria (do not infer completion from coverage alone): hosted expanded-suite/archive runs on all four desktop runtime targets; native picker/clipboard/drag/drop and installed edit/import/export acceptance; independent game fixtures and sale/time/status semantics; retained/peak/native-bitmap memory, cache ownership, virtualization/render and startup profiling; large-history/full-reset scaling and large-shop snapshot latency. Asset redistribution confirmation and signing/notarization are distribution decisions, not established by synthetic tests.
