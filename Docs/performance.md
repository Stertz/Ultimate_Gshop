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

Saving/loading and large grid projection warrant investigation before micro-optimizing search. Since saves currently run on the UI thread, these synchronous service durations indicate a responsiveness risk; actual native input/render latency is still unmeasured. The grid jump from roughly 2 ms at 10k to 60 ms at 50k warrants allocation/GC profiling. Search also allocates several MB per query at 50k and remains a secondary candidate.

## Interpretation and next work

Allocated bytes measure allocation traffic, not retained heap, peak RAM or native bitmap memory. Grid measurements cover model construction, not layout/rendering. File I/O includes the local filesystem/cache/antivirus behavior and does not claim cold-disk throughput. UI responsiveness, first startup, bitmap cache lifecycle, virtualization and native platform behavior need separate measurements.

Prioritize safe asynchronous saves after defining immutable snapshots for shop/history/settings and ordering edits, import, close and overlapping saves. Merely putting the existing mutable session in `Task.Run` is insufficient. Inspect grid snapshot ownership/materialization and loader allocation before changing behavior; preserve independent editing data and format compatibility.

For comparisons, use two sequential fresh-process runs on the same machine/runtime/storage with no coverage collector, debugger or other benchmark running. Investigate repeated median regressions above 25% or allocation growth above 10% against this baseline; these are review triggers, not portable CI pass/fail budgets. Set release performance budgets after real fixtures and native UI profiling. Do not trade transactional save durability for lower latency.

Still pending: representative real fixtures, history scaling including full-shop reset snapshots, retained/peak memory and bitmap profiling, UI render/input latency, first-save/cold-load/startup measurements, native Windows/Linux/macOS packages and flows, and controlled before/after optimization results.
