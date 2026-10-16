# Persistence and recovery

## Session directory

The application stores sessions in `ultimate_gshop` under the directory returned by .NET's `Environment.SpecialFolder.ApplicationData`.

| File | Purpose |
| --- | --- |
| `gshop_temp.data` | Current shop in client binary format |
| `gshop_before.data` | Initial bytes when a session is created or imported |
| `tracking.json` | Undo and redo history |
| `settings.json` | Autosave preferences and source/backup paths |
| `.session.lock` | Exclusive file handle used while accessing the session |
| `.session-transaction/` | Staged payloads and rollback files |

Backups are written beneath the session's `backups` directory in timestamped folders with a random suffix. A backup contains `gshop.data`, `tracking.json` and `settings.json`.

## Saving

Editor autosave captures a detached copy of the shop, categories, item sales/owners, both history stacks and settings on the UI thread. Serialization and filesystem writes then run on a worker. A second save request is rejected while one is outstanding.

Closing pauses autosave, waits for any outstanding save, captures the latest committed state and saves it before closing. A save failure displays an error and keeps the editor open for retry. Opening the menu also pauses autosave and waits for its completion before allowing session operations. Autosave is reconfigured when the menu closes.

The autosave interval is at least 30 seconds. Settings use the JSON properties `AutoSave`, `SaveFromSeconds`, `LastGshopLoadedFrom` and `LastBackupFrom`. Options edit a separate draft; the live preferences change after settings persistence succeeds.

## Transaction and recovery

Session reads and writes share an in-process lock and an exclusive `.session.lock` file handle. Another process attempting access while the handle is held receives an I/O error. The lock file can remain after the handle is released.

A multi-file write follows these steps:

1. Write and flush new payloads as `*.next` and existing-file backups as `*.previous` inside `.session-transaction`.
2. Write `prepared.json`, then rename it to `pending.json` before replacing destinations.
3. Replace each destination with its staged payload.
4. Write `resolved` and remove the journal files.

Before reading or writing a session, recovery processes any existing journal. An unresolved pending operation restores prior files and removes destinations that did not previously exist. A resolved operation retains the committed files and finishes cleanup. Preparation without a pending manifest is discarded. Invalid manifests or missing recovery backups raise an error and leave the journal available for recovery.

The transaction recognizes only the session binary and JSON filenames. Standalone settings/history writes use protected replacement under the same lock/recovery boundary. Cooperating application readers use that boundary; destination files are replaced individually.

## Import and new shops

Import copies the selected file into a private temporary snapshot and parses it before replacing the session. The menu backs up the current shop after validation and before replacement. The imported snapshot's original bytes initialize `gshop_before.data` and `gshop_temp.data`; subsequent saves serialize the in-memory shop.

Creating a shop writes the generated client binary to both session files. Creating/importing a shop writes empty history, preserves autosave preferences and backup metadata, and updates the source path. The menu clears the live history after session creation succeeds. Canceling file selection leaves the session unchanged.

## Undo and redo

`tracking.json` stores both history stacks with the top entry first. Item insert/update/delete entries retain item snapshots. Reordering stores changed records by index; category resets retain a complete item-list snapshot. Category metadata changes retain category and affected-item snapshots. Undo/redo restores these values and recalculates the item count.

## Export

Client export writes `gshop.data`; server export writes `gshopsev.data`. The menu confirms replacement of an existing destination, saves the current session and writes the selected format. Session persistence and destination export are separate protected operations.