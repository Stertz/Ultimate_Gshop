# Using Ultimate Gshop

## Sessions

Use the menu to create a shop, import a client Gshop file, or continue the current session. Creating or importing a shop starts a new undo/redo history. The application stores the active session separately from exported game files; see [persistence](persistence.md).

## Editing

Select a category/subcategory to scope the item list. Search matches name terms without case sensitivity; numeric queries also match part of an item ID. Clearing or shortening the query refreshes the results from the current shop.

Select an item to inspect its details, then use the edit action to change it. Confirm the item save to apply the draft. New-item and copy actions prepare a draft before inserting a record. Undo and redo apply to committed editing operations.

Ctrl toggles individual selections and Shift selects a range in displayed order. The item context menu provides bulk quantity, price, period, frequency, category, clone and delete operations. Advanced prices open the sale editor and require confirmation before applying to the selected items.

The shop overview displays a read-only snapshot of position, ID, name, quantity, category and subcategory.

## Save and export

Options control autosave and its interval; the minimum interval is 30 seconds. Closing the editor saves the committed session after any discard confirmation. An unsaved item draft must be saved in the editor to become part of the shop.

Client export writes `gshop.data`; server export writes `gshopsev.data` in the selected folder. Existing destination files require overwrite confirmation. Export saves the current session before writing the selected target.

## Contribute

Pull requests adding screenshots, worked examples, descriptions of sale fields and game-specific workflows are welcome. Include the application version and relevant game build in examples; use [compatibility reports](compatibility.md) for game-build validation.
