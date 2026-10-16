# Game compatibility validation

The maintainer confirmed on 2026-10-16 that the original game shop files are no longer available. No independent real-game reference is bundled. Synthetic fixtures, explicit field/byte expectations, parser failure cases and save/load/history regressions are the current automated evidence. Passing them does not certify a specific client/server build.

Keep the project in preview while community verification is collected. Preserve the characterized loader normalization and sale/time/status behavior until independent evidence supports changing those rules. The absence of original samples is recorded as external acceptance work, rather than an achievable coverage task or a reason to manufacture a compatibility claim.

Contributors can report validation using this checklist:

- Exact game/client/server edition and build, OS/architecture, application commit and package version.
- Original file size and SHA-256, whether it is client or server data, and expected category/item counts.
- Import the original, check category/subcategory placement, item identity, prices, gifts, sale dates/status/control and NPC owners against the known working version.
- Edit one known record, exercise undo/redo, save/close/reopen, and export the required client/server targets. Record the expected changes and any differences in unrelated fields.
- Confirm the exported shop is accepted and behaves as expected in that game build; an editor-only round trip is insufficient.
- Provide a minimal redistributable fixture with explicit expected bytes/behavior when possible, so a regression can be added. An issue with the observations above is also useful when a sample cannot be provided.

Native desktop acceptance also remains distinct from headless tests: opening the extracted application, actual file pickers, clipboard/keyboard, drag/drop after scrolling, and edit/import/export/close flows should be checked on each supported OS. Hosted package diagnostics validate managed/native dependencies, resources and synthetic persistence without opening interactive windows.
