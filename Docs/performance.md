# Performance measurements

Stage 3 started on 2026-10-16. The standalone console harness references the compiled application and exercises its actual services. It has no benchmark package dependency and stays outside the solution's normal test/coverage run. The application continues to exclude all `tests/**` sources; its coverage scope is unchanged.

## Reproduce

From the repository root, with the .NET SDK pinned in `global.json`:

```powershell
./scripts/Measure-Performance.ps1
# Optional separate output folder; runs execute sequentially.
./scripts/Measure-Performance.ps1 -Runs 2 -OutputDirectory TestResults/performance/comparison
```

The script restores/builds Release with warnings as errors, then starts a fresh process for each run. Tiered compilation is disabled to reduce JIT transitions during short measurements. Each scenario has three warmups, a full GC before sampling, and fifteen timed samples. No forced GC occurs between samples. JSON reports contain environment details, every sample, median duration, nearest-rank p95, and median allocated bytes on the executing thread. With fifteen samples p95 is the maximum; use larger samples before setting tail-latency gates.

Synthetic shops contain 1,000, 10,000 and 50,000 items, eight categories, four sale slots and eight owner IDs per item, using the shared serialization test fixture. These are scaling probes, not verified real-world shop distributions. Scenarios cover unrestricted/name/numeric search, two-click full-range selection, the deep-cloned read-only grid projection, transactional session save with empty/100-entry history, and session reload. Result counts and snapshot independence are checked outside timing; saves also validate persisted item/history counts. Saving measures steady-state replacement after warmup, including transaction backups and flushes, on a unique OS-temporary directory; it never opens the user's session. Temporary files are cleaned on success/failure. Only explicitly requested report files remain.

## Initial local baseline

Measured 2026-10-16 on Windows 10.0.26300, x64, .NET 10.0.13, 20 logical processors, using the services committed in `b82060e`. Two sequential fresh-process runs completed with all scenario validations passing. Exact reports are generated at `TestResults/performance/run-1.json` and `run-2.json` (ignored build artifacts). The ranges below span the two runs' medians; they are not confidence intervals.

| Operation, 50,000 items | Median range (ms) | Allocated per operation (decimal MB) |
| --- | ---: | ---: |
| Search, no query | 1.05–1.08 | 4.80 |
| Search, name terms | 3.45–4.05 | 4.40 |
| Search, numeric ID/name | 3.72–3.96 | 6.00 |
| Two-click full-range selection | 5.46–6.02 | 11.05 |
| Grid snapshot projection | 59.41–64.27 | 30.40 |
| Transactional save, empty history | 259.96–268.33 | 73.57 |
| Transactional save, 100 history entries | 293.15–295.26 | 73.57 |
| Session load | 292.93–329.52 | 224.75 |

Scaling of the main I/O operations:

| Items | Save, empty history (ms) | Load (ms) |
| ---: | ---: | ---: |
| 1,000 | 18.81–19.03 | 3.93–3.96 |
| 10,000 | 56.95–58.62 | 61.45–75.26 |
| 50,000 | 259.96–268.33 | 292.93–329.52 |

At baseline, saves ran on the UI thread, and the grid cloned complete editing data. The following increment addresses these costs; actual native input/render latency is still unmeasured.

## Post-change measurements

Two sequential fresh-process runs on the same environment are retained in `TestResults/performance/final/run-1.json` and `run-2.json`. Each run now also measures snapshot capture with 100 history entries. Allocation figures are decimal MB.

| Operation, 50,000 items | Baseline median (ms) | New median (ms) | Baseline/new allocated MB |
| --- | ---: | ---: | ---: |
| Grid projection | 59.41–64.27 | 1.62–1.76 | 30.40 / 2.80 |
| Session load | 292.93–329.52 | 214.84–216.40 | 224.75 / 37.46 |
| Search, no query | 1.05–1.08 | 0.43–0.97 | 4.80 / 0.40 |
| Search, name terms | 3.45–4.05 | 2.85–4.51 | 4.40 / 0.00114 |
| Search, numeric ID/name | 3.72–3.96 | 2.29–2.37 | 6.00 / 1.60 |

Grid rows now contain only six immutable displayed values; no sale/owner snapshots are allocated for this read-only view. Item parsing decodes into reused stack buffers before creating the final strings. Search uses a loop for name terms instead of a captured lambda for every item. No assets, encoding rules or load normalization policies were changed. Search timing varies across runs, so the robust finding for name queries is lower allocation rather than a universal speedup.

Transactional writes retain their durability and still take roughly 293–331 ms at 50k in these runs. Editor saves move this work onto a worker using a private snapshot. Capture with 100 history entries costs about 0.20 ms/0.68 MB at 1k, 1.93–2.35 ms/6.15 MB at 10k, and 76.33–94.86 ms/30.47 MB at 50k. Thus background I/O does not make large-shop saves stall-free: capture remains an explicit profiling item. Grid p95 is about 24 ms at 50k despite the much lower median, also requiring native/GC profiling before strict frame budgets.

## Interpretation and next work

Allocated bytes measure allocation traffic, not retained heap, peak RAM or native bitmap memory. Grid measurements cover model construction, not layout/rendering. File I/O includes the local filesystem/cache/antivirus behavior and does not claim cold-disk throughput. UI responsiveness, first startup, bitmap cache lifecycle, virtualization and native platform behavior need separate measurements.

Safe asynchronous editor saves are implemented and covered by snapshot-isolation, failure and close/navigation ordering regressions. Continue profiling snapshot capture/history scaling and native rendering/cache ownership; do not move mutable session reads to a worker or remove transaction durability to improve benchmark numbers.

For comparisons, use two sequential fresh-process runs on the same machine/runtime/storage with no coverage collector, debugger or other benchmark running. Investigate repeated median regressions above 25% or allocation growth above 10% against this baseline; these are review triggers, not portable CI pass/fail budgets. Set release performance budgets after real fixtures and native UI profiling. Do not trade transactional save durability for lower latency.

Still pending: representative real fixtures, history scaling including full-shop reset snapshots, retained/peak memory and bitmap profiling, UI render/input latency, first-save/cold-load/startup measurements, native Windows/Linux/macOS package and interaction acceptance. The initial before/after service optimization measurements above are complete.
