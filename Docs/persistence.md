# Session Persistence and Recovery

The existing binary filenames and JSON property names are retained. Resource files are never part of a session transaction.

## Save and session switching

Normal saves stage the client binary, tracking JSON, and settings JSON together. Explicit backup saves retain the `gshop.data` filename; the current session retains `gshop_temp.data`.

Import first copies the source into a private snapshot and validates that snapshot. Starting the imported session writes the original snapshot bytes to `gshop_before.data` and `gshop_temp.data`, resets tracking, and records the source path. It preserves autosave preferences and the last backup location. The original bytes are kept even where the legacy loader normalizes values in memory.

Starting a new shop similarly writes both session binaries and empty history, clears the source path, and retains user preferences. The menu clears both in-memory history stacks only after successful session creation. Closing/canceling a menu does not substitute a null shop; changing shops clears the previous item selection and edit state. Native dialog interactions remain untested in this phase.

## Recovery protocol

`SessionTransaction` serializes in-process access and holds a `.session.lock` file handle with exclusive sharing. A competing process fails with an I/O error rather than replacing files concurrently. The lock file itself may remain after the handle is released; its existence does not mean the session is locked.

The `.session-transaction` directory contains only the recovery protocol's staged payloads and backups:

1. Write and flush all `*.next` payloads and `*.previous` backups.
2. Write the complete manifest to `prepared.json`, then rename it to `pending.json` before changing destinations.
3. Replace the destination files individually.
4. Write a `resolved` marker after all replacements succeed, then remove recovery files.

Before a session read or write, an unresolved pending manifest restores every previous file and removes destinations that did not exist before the operation. Backups are checked before restoring any destination and retained until recovery completes. Recovery also writes `resolved` before cleanup, allowing cleanup to be interrupted and retried safely.

An incomplete preparation without a pending manifest never changed destination files and is discarded. A resolved operation keeps its final files and only finishes cleanup. Missing backups, malformed manifests, and unsupported filenames produce an error and retain recovery evidence. Recovery names are restricted to the known session binary/JSON filenames; computed directories are not recursively deleted.

Standalone settings/history writes use protected replacement and the same session lock/recovery boundary. Unrelated corrupt JSON is reported and left intact; it is not silently overwritten with defaults. A complete recovery journal can restore the prior metadata generation, but there is no guaranteed repair for arbitrary preexisting corruption without a backup.

## Verification and limits

Reordering records now adds one `Reorder` history action, storing independent item snapshots in `ItemsBackup` keyed by list index. Existing Insert/Update/Delete enum values and history properties retain their meanings; older history files remain readable. The new property is omitted for other actions. Reorder undo/redo restores both record order and positions, including after saving and reloading. Only changed records are backed up. Older application versions do not implement this new action and must not be used to undo sessions containing it.

Filtered moves reuse the visible records' existing position slots and leave hidden records unchanged. The binary record order changes with the move because the legacy loader derives in-memory positions from record order. Loader normalization of unrelated game fields is unchanged.

Category resets add a `ResetItems` action with `ItemListBackup`, a deep snapshot of the complete item list. Reset undo restores deleted and surviving records, their original positions, and prior index-based history; redo restores the inverse snapshot. The list object is retained and ItemAmount is synchronized. Full snapshots deliberately trade memory for correctness during destructive category resets; benchmark their memory cost on large shops before optimizing them.

Category rename, subcategory add/rename, and empty-subcategory deletion add a `Category` action with `CategoryBackup` and optional indexed `ItemsBackup`. Deletion adjusts later subcategory indices only in the affected category. Undo/redo restores metadata and affected records together. The window refreshes category sources afterward. Clone operations retain the established per-item Insert history behavior. Existing enum values remain unchanged; these actions are appended, and old histories remain readable by the updated application. Older versions do not support the appended actions.

Save/reload tests preserve the legacy loader contract: surviving reset records may have their Place normalized on load. Undo of a reset restores the original full snapshot; its inverse then captures the actual reloaded state. This work does not introduce lossless parsing of arbitrary game files.

Tests inject failures during staging and after individual replacements, verify first-save rollback, validate concurrent saves and lock contention, and construct interrupted-preparation/replacement/cleanup fixtures. They also cover invalid journal paths, missing backups, corrupt metadata, preference preservation, and history reset.

This is recoverable multi-file persistence, not an instantaneous filesystem transaction visible to arbitrary external readers. Application readers cooperate with the lock; other tools can observe intermediate replacements. File payloads are flushed, but physical power-loss behavior, directory-entry durability, full-disk failures, and all filesystem implementations have not been certified. No destructive process-kill or power-loss test was performed.

If recovery fails, retain the entire session folder and `.session-transaction` evidence before attempting manual repair. Native UI error presentation and broader backup retention remain follow-up work.
