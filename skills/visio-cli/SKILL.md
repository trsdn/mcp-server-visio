---
name: visio-cli
description: >
  Automate Microsoft Visio on Windows via CLI. Use when creating, reading,
  or modifying Visio diagrams from scripts, CI/CD, or coding agents.
  Best current support: sessions, pages, shapes, text, ShapeSheet cells, and stencil masters.
  Triggers: Visio, vsdx, diagram, shape, page, stencil, ShapeSheet, visiocli.
---

# Visio Automation with visiocli

## Preconditions

- Windows host with Microsoft Visio installed
- Uses COM interop — does NOT work on macOS or Linux
- Install: `dotnet tool install --global VisioMcp.CLI`

## Workflow Checklist

| Step | Command | When |
|------|---------|------|
| 1. Session | `session create/open` | Always first |
| 2. Pages | `page create/list/read` | Navigate or add diagram pages |
| 3. Add content | `shape`, `text`, `stencil` | Draw shapes, set text, drop masters |
| 4. ShapeSheet | `cell read/write/list` | Inspect or edit core cells |
| 4. Save & close | `session close --save` | Always last |

> **10+ commands?** Use `visiocli -q batch --input commands.json` — sends all commands in one process with automatic session management. See Rule 8.

## CRITICAL RULES (MUST FOLLOW)

> **⚡ Building larger diagrams or bulk operations?** Skip to **Rule 8: Batch Mode** — it eliminates per-command process overhead and auto-manages session IDs.

### Rule 1: NEVER Ask Clarifying Questions

Execute commands to discover the answer instead:

| DON'T ASK | DO THIS INSTEAD |
|-----------|-----------------|
| "Which file should I use?" | `visiocli -q session list` |
| "Which page has the content?" | `visiocli -q page list --session <id>` |
| "What shapes are on this page?" | `visiocli -q shape list --session <id> --page-index 1` |

**You have commands to answer your own questions. USE THEM.**

### Rule 2: Always End With a Text Summary

**NEVER end your turn with only a command execution.** After completing all operations, always provide a brief text message confirming what was done. Silent command-only responses are incomplete.

### Rule 3: Session Lifecycle

**Creating vs Opening Files:**
```powershell
# NEW file - use session create
visiocli -q session create C:\path\newfile.vsdx  # Creates file + returns session ID

# EXISTING file - use session open
visiocli -q session open C:\path\existing.vsdx   # Opens file + returns session ID
```

**CRITICAL: Use `session create` for new files. `session open` on non-existent files will fail!**

**CRITICAL: ALWAYS use the session ID returned by `session create` or `session open` in subsequent commands. NEVER guess or hardcode session IDs. The session ID is in the JSON output (e.g., `{"sessionId":"abc123"}`). Parse it and use it.**

```powershell
# Example: capture session ID from output, then use it
visiocli -q session create C:\path\file.vsdx     # Returns JSON with sessionId
visiocli -q page list --session <returned-session-id>
visiocli -q session close --session <returned-session-id> --save
```

**Unclosed sessions leave Visio processes running, locking files.**

### Rule 4: Pages Before Shapes

Create or inspect the target page before placing shapes:

```powershell
visiocli -q page create --session <id> --name "Overview"
visiocli -q page list --session <id>
```

### Rule 5: Shape and Text Workflow

**BEST PRACTICE: Add shapes then set their text/properties**

```powershell
# Step 1: Add a shape to a page
visiocli -q shape add-shape --session <id> --page-index 1 --left 1 --top 1 --width 2 --height 1 --auto-shape-type 1

# Step 2: Set text content
visiocli -q text set --session <id> --page-index 1 --shape-name "Rectangle.1" --text "Hello World"

# Step 3: Inspect or adjust geometry
visiocli -q cell write --session <id> --page-index 1 --shape-name "Rectangle.1" --cell-name Width --value 3
```

### Rule 6: Report File Errors Immediately

If you see "File not found" or "Path not found" - STOP and report to user. Don't retry.

### Rule 7: Prefer Stencil Masters for Standard Shapes

When the diagram should use built-in Visio masters, list the stencil and drop the master instead of drawing ad-hoc shapes:

```powershell
visiocli -q stencil list-masters --session <id> --stencil-path "C:\Program Files\Microsoft Office\root\Office16\Visio Content\1031\BASIC_M.VSSX"
visiocli -q stencil drop-master --session <id> --stencil-path "C:\Program Files\Microsoft Office\root\Office16\Visio Content\1031\BASIC_M.VSSX" --page-index 1 --master-name Rectangle --x-position 2 --y-position 2
```

### Rule 8: Use Batch Mode for Bulk Operations (10+ commands)

When executing 10+ commands on the same file, use `visiocli batch` to send all commands in a single process launch. This avoids per-process startup overhead and terminal buffer saturation.

```powershell
# Create a JSON file with all commands
@'
[
  {"command": "session.open", "args": {"filePath": "C:\\path\\diagram.vsdx"}},
  {"command": "page.create", "args": {"name": "Overview"}},
  {"command": "shape.add-shape", "args": {"pageIndex": 1, "left": 1, "top": 1, "width": 2, "height": 1, "autoShapeType": 1}},
  {"command": "session.close", "args": {"save": true}}
]
'@ | Set-Content commands.json

# Execute all commands at once
visiocli -q batch --input commands.json
```

**Key features:**
- **Session auto-capture**: `session.open`/`create` result sessionId auto-injected into subsequent commands — no need to parse and pass session IDs
- **NDJSON output**: One JSON result per line: `{"index": 0, "command": "...", "success": true, "result": {...}}`
- **`--stop-on-error`**: Exit on first failure (default: continue all)
- **`--session <id>`**: Pre-set session ID for all commands (skip session.open)

**Input formats:**
- JSON array from file: `visiocli -q batch --input commands.json`
- NDJSON from stdin: `Get-Content commands.ndjson | visiocli -q batch`

## CLI Command Reference

> Auto-generated from `visiocli --help`. Use these exact parameter names.


### cell

Visio ShapeSheet cell operations for reading and writing cells on a shape, a page or the document.

**Actions:** `read`, `read-formula`, `write`, `set-formula`, `list`, `list-sections`, `list-rows`, `add-row`, `delete-row`, `read-src`, `write-src`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | 1-based page index (required) |
| `--shape-name` | Shape name, exactly as reported by shape(list). Required when sheet_target is 'shape'; ignored otherwise |
| `--cell-name` | ShapeSheet cell name, for example Width, PinX, FillForegnd, Char.Size or Prop.Cost. Section cells use the Section.Row.Cell form (required for: read, read-formula, write, set-formula) |
| `--sheet-target` | Which ShapeSheet to address: 'shape' (the default), 'page' for the page's own ShapeSheet where page size and drawing scale live, or 'document' for the document's. shapeName is not needed for page or document |
| `--value` | Literal value. Distance cells need explicit units such as '3 in' or '12 pt', and angles need ' deg'; a bare number is read in internal units (inches, radians) (required for: write) |
| `--formula` | ShapeSheet expression, for example 'Width*0.5' or 'GUARD(2 in)'. A formula recalculates when its inputs change; a literal value does not (required for: set-formula) |
| `--section` | Section name such as Prop, User, Connections, Actions or Hyperlink, or a numeric section index. Use list-sections to see what a shape has |
| `--row-name` | Optional row name. Named rows become addressable cells such as Prop.Cost or User.Origin; omit it for sections whose rows are positional, such as Connections |
| `--row-index` | 0-based row index within the section. Rows below it shift up, so delete from the highest index first when removing several |
| `--column-index` | 0-based column index within the row. This is the only way to reach a row that has no name |



### page

Visio page lifecycle, guides, and routing commands.

**Actions:** `list`, `read`, `create`, `set-name`, `delete`, `list-guides`, `add-guide`, `set-guide-position`, `delete-guide`, `get-routing-settings`, `set-route-style`, `set-connector-routing-extension`, `set-line-jump-code`, `set-line-jump-style`, `set-walk-preference`, `set-place-style`, `read-background`, `set-background`, `set-back-page`, `clear-back-page`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | 1-based page index (required for: read, set-name, delete, list-guides, add-guide, set-guide-position, delete-guide, get-routing-settings, set-route-style, set-connector-routing-extension, set-line-jump-code, set-line-jump-style, set-walk-preference, set-place-style, read-background, set-background, set-back-page, clear-back-page) |
| `--position` | 1-based insertion position. Pass 0 to append the page at the end (required for: create) |
| `--name` | Page name. Must be unique within the document (required for: create, set-name) |
| `--guide-type` | 1 = point, 2 = horizontal guide line, 3 = vertical guide line (required for: add-guide) |
| `--x-position` | Guide X position in points. Ignored for a horizontal guide (required for: add-guide, set-guide-position) |
| `--y-position` | Guide Y position in points. Ignored for a vertical guide. Visio measures Y upward from the bottom of the page (required for: add-guide, set-guide-position) |
| `--guide-name` | Guide shape name, as reported by list-guides (required for: set-guide-position, delete-guide) |
| `--route-style` | Connector routing style, written to the page RouteStyle cell. Controls whether connectors run at right angles, straight, or along a tree or flowchart layout. Read the current value with get-routing-settings before changing it (required for: set-route-style) |
| `--connector-routing-extension` | Routing extension, written to the page ConLineRouteExt cell. Selects whether connectors are drawn with straight or curved segments (required for: set-connector-routing-extension) |
| `--line-jump-code` | Which connectors draw a jump where lines cross, written to the page LineJumpCode cell (required for: set-line-jump-code) |
| `--line-jump-style` | Shape of the jump drawn where connectors cross, written to the page LineJumpStyle cell (required for: set-line-jump-style) |
| `--walk-preference` | Which side of a shape a connector prefers to leave from, written to the page WalkPreference cell (required for: set-walk-preference) |
| `--place-style` | Automatic layout style used when shapes are placed, written to the page PlaceStyle cell (required for: set-place-style) |
| `--is-background` | True to make it a background page, false to turn it back into a normal page (required for: set-background) |
| `--back-page-name` | Name of the background page to show behind it, exactly as reported by list (required for: set-back-page) |



### shape

Shape management: list, read, create, move, resize, delete, z-order.

**Actions:** `list`, `read`, `list-groups`, `read-group`, `list-selection`, `select-shapes`, `add-to-selection`, `remove-from-selection`, `clear-selection`, `list-properties`, `get-property`, `set-property`, `delete-property`, `list-connectors`, `read-connector`, `list-connections`, `list-connection-points`, `add-connection-point`, `set-connection-point`, `delete-connection-point`, `disconnect-connector`, `reconnect-connector`, `add-textbox`, `add-shape`, `move-resize`, `delete`, `z-order`, `set-fill`, `set-line`, `set-rotation`, `group`, `ungroup`, `set-alt-text`, `copy-to-page`, `set-shadow`, `add-connector`, `connect-shapes`, `merge`, `duplicate`, `flip`, `set-text-frame`, `set-gradient-fill`, `set-glow`, `set-reflection`, `set-opacity`, `read-fill`, `read-line`, `find-by-type`, `copy-formatting`, `scale`, `lock-aspect-ratio`, `set-soft-edge`, `read-shadow`, `add-text-effect`, `set-3d`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required) |
| `--shape-name` | (required for: read, read-group, list-properties, get-property, set-property, delete-property, read-connector, list-connections, list-connection-points, add-connection-point, set-connection-point, delete-connection-point, disconnect-connector, reconnect-connector, move-resize, delete, z-order, set-fill, set-line, set-rotation, ungroup, set-alt-text, copy-to-page, set-shadow, duplicate, flip, set-text-frame, set-gradient-fill, set-glow, set-reflection, set-opacity, read-fill, read-line, scale, lock-aspect-ratio, set-soft-edge, read-shadow, set-3d) |
| `--shape-names` | (required for: select-shapes, add-to-selection, remove-from-selection, group, connect-shapes, merge) |
| `--property-name` | Shape Data property name, matched against the row label or the underlying Prop.<row> name, case-insensitively (required for: get-property, set-property, delete-property) |
| `--property-value` | Property value, stored as a string. Omit to store an empty value |
| `--connection-point-x` | X position as a ShapeSheet formula. Prefer a relative expression such as 'Width*0.5' so the point stays put when the shape is resized; a literal needs units, for example '1 in' |
| `--connection-point-y` | Y position as a ShapeSheet formula, for example 'Height*1' for the top edge. Visio measures Y upward |
| `--connection-point-name` | Optional name. A named point becomes the glue target 'Connections.<name>', which survives rows being added or deleted; an unnamed point is addressed only by index |
| `--connection-point-index` | 0-based index in the shape's connection point list, as reported by list-connection-points (required for: delete-connection-point) |
| `--connector-end` | Connector endpoint selector: start or end (required for: disconnect-connector, reconnect-connector) |
| `--target-shape-name` | Target shape name for the selected endpoint (required for: reconnect-connector, copy-formatting) |
| `--left` | Position from left in points (required for: add-textbox, add-shape, add-text-effect) |
| `--top` | Position from top in points (required for: add-textbox, add-shape, add-text-effect) |
| `--width` | Width in points (required for: add-textbox, add-shape) |
| `--height` | Height in points (required for: add-textbox, add-shape) |
| `--text` | Initial text content (required for: add-textbox, add-text-effect) |
| `--auto-shape-type` | Shape to draw: 9 draws an ellipse, any other value draws a rectangle. Only these two primitives are supported — Visio has no auto-shape gallery, so richer shapes come from stencil masters via the 'stencil' tool. (required for: add-shape) |
| `--z-order-cmd` | 1=BringToFront, 2=SendToBack, 3=BringForward, 4=SendBackward (required for: z-order) |
| `--color-hex` | Hex color string like #FF0000 for red, or 'none' for no fill (required for: set-fill, set-line, set-glow) |
| `--line-width` | Line width in points (default 0.75) (required for: set-line) |
| `--degrees` | Rotation in degrees, clockwise. Visio's underlying Angle cell measures anticlockwise in radians; this parameter is converted for you (required for: set-rotation) |
| `--alt-text` | Alternative text describing the shape for assistive technology. Stored in the shape's Comment ShapeSheet cell, which is where Visio keeps alt text (required for: set-alt-text) |
| `--target-page-index` | 1-based target page index (required for: copy-to-page) |
| `--visible` | Show or hide shadow (required for: set-shadow) |
| `--offset-x` | Shadow offset X in points (required for: set-shadow) |
| `--offset-y` | Shadow offset Y in points (required for: set-shadow) |
| `--connector-type` | Routing style: 1=Straight, 2=Elbow (right-angle), 3=Curved (required for: add-connector) |
| `--start-shape-name` | Starting shape name (required for: add-connector) |
| `--end-shape-name` | Ending shape name (required for: add-connector) |
| `--merge-type` | 1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract (required for: merge) |
| `--flip-type` | 0=Horizontal, 1=Vertical (required for: flip) |
| `--margin-left` | Left margin in points (null = don't change) |
| `--margin-right` | Right margin in points (null = don't change) |
| `--margin-top` | Top margin in points (null = don't change) |
| `--margin-bottom` | Bottom margin in points (null = don't change) |
| `--word-wrap` | Enable/disable word wrap (null = don't change) |
| `--auto-size` | 0=None, 1=ShapeToFitText, 2=TextToFitShape (null = don't change) |
| `--color1` | First gradient color as hex (#RRGGBB) (required for: set-gradient-fill) |
| `--color2` | Second gradient color as hex (#RRGGBB) (required for: set-gradient-fill) |
| `--gradient-style` | 1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown, 5=FromCorner, 6=FromCenter (required for: set-gradient-fill) |
| `--radius` | Glow radius in points (0 = remove glow) (required for: set-glow, set-soft-edge) |
| `--reflection-type` | 0=None, 1-9=msoReflectionType1 through msoReflectionType9 (required for: set-reflection) |
| `--opacity` | Opacity value from 0.0 (fully transparent) to 1.0 (fully opaque) (required for: set-opacity) |
| `--shape-type` | Visio VisShapeTypes integer: 1=Page, 2=Group, 3=Shape, 4=ForeignObject (images and OLE), 5=Guide, 6=Document. An ordinary drawn or dropped shape is 3. (required for: find-by-type) |
| `--source-shape-name` | Name of the shape to copy formatting from (required for: copy-formatting) |
| `--scale-x` | Width scale factor (e.g. 1.5 = 150%) (required for: scale) |
| `--scale-y` | Height scale factor (e.g. 1.5 = 150%) (required for: scale) |
| `--locked` | True to lock aspect ratio, false to unlock (required for: lock-aspect-ratio) |
| `--preset-effect` | MsoPresetTextEffect integer (0-based preset index) (required for: add-text-effect) |
| `--font-name` | Font name (e.g. "Arial") (required for: add-text-effect) |
| `--font-size` | Font size in points (required for: add-text-effect) |
| `--rotation-x` | X-axis rotation in degrees (null = don't change) |
| `--rotation-y` | Y-axis rotation in degrees (null = don't change) |
| `--rotation-z` | Z-axis rotation in degrees (null = don't change) |
| `--bevel-type` | Bevel top type: 0=None, 1=Circle, 2=RelaxedInset, 3=Cross, 4=Angle, etc. (null = don't change) |
| `--bevel-depth` | Bevel top depth in points (null = don't change) |



### stencil

Visio stencil operations for listing masters and dropping them onto pages.

**Actions:** `list-masters`, `drop-master`

| Parameter | Description |
|-----------|-------------|
| `--stencil-path` | Full path to a stencil file (.vssx or .vss), for example the installed BASIC_M.VSSX (required) |
| `--page-index` | 1-based page index (required for: drop-master) |
| `--master-name` | Master name, exactly as reported by list-masters (required for: drop-master) |
| `--x-position` | Drop position X in points, measured from the left of the page (required for: drop-master) |
| `--y-position` | Drop position Y in points. Visio measures Y upward from the bottom of the page, unlike most drawing APIs (required for: drop-master) |



### text

Text operations within shapes: get, set, format, find, replace.

**Actions:** `get`, `set`, `find`, `replace`, `format`, `format-advanced`, `word-count`, `alt-text-audit`, `empty-placeholder-audit`, `set-spacing`, `set-bullets`, `insert-link`, `change-case`, `read-spacing`, `read-bullets`, `insert-symbol`, `insert-datetime`, `insert-page-number`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required) |
| `--shape-name` | (required for: get, set, format, format-advanced, set-spacing, set-bullets, insert-link, change-case, read-spacing, read-bullets, insert-symbol, insert-datetime, insert-page-number) |
| `--text` | Replacement text for the whole shape. Existing text and its run formatting are discarded (required for: set) |
| `--search-text` | Text to find (required for: find, replace) |
| `--replace-text` | Replacement text (required for: replace) |
| `--font-name` | Font family name or null to keep the current font (required for: insert-symbol) |
| `--font-size` | Font size in points or null to keep the current size |
| `--bold` | Set bold or null to leave unchanged |
| `--italic` | Set italic or null to leave unchanged |
| `--color` | Hex color string (#RRGGBB) or null to keep the current color |
| `--alignment` | Horizontal alignment: left, center, right, justify; null leaves unchanged |
| `--vertical-alignment` | Vertical alignment: top, middle, bottom; null leaves unchanged |
| `--underline` | Set underline (null = don't change) |
| `--strikethrough` | Set strikethrough (null = don't change) |
| `--subscript` | Set subscript (null = don't change) |
| `--superscript` | Set superscript (null = don't change) |
| `--line-spacing` | Line spacing in points (null = don't change) |
| `--space-before` | Space before paragraph in points (null = don't change) |
| `--space-after` | Space after paragraph in points (null = don't change) |
| `--character-spacing` | Character spacing in points (null = don't change) |
| `--bullet-type` | 0=None, 1=Unnumbered (bullets), 2=Numbered (required for: set-bullets) |
| `--bullet-character` | Custom bullet character (e.g. "•", "→") - only used when bulletType is 1 |
| `--indent-level` | Indent level 0-4 (required for: set-bullets) |
| `--link-text` | Text to find and make into a hyperlink (required for: insert-link) |
| `--url` | URL for the hyperlink (required for: insert-link) |
| `--case-type` | 1=Sentence, 2=Lower, 3=Upper, 4=Title, 5=Toggle (required for: change-case) |
| `--char-number` | Unicode/character code of the symbol (required for: insert-symbol) |
| `--date-time-format` | Date/time format selector (1-13) (required for: insert-datetime) |





### design

Diagram design guidance: which diagram to draw, which stencil and masters to drop, and the Visio techniques that separate a usable drawing from a plausible-looking one

**Actions:** `list-archetypes`, `get-archetype`, `get-stencil-catalog`, `get-diagram-patterns`, `list-palettes`, `get-palette`

| Parameter | Description |
|-----------|-------------|
| `--archetype-id` | Archetype id, exactly as reported by list-archetypes (required for: get-archetype) |
| `--palette-id` | Palette id, exactly as reported by list-palettes (required for: get-palette) |


### docproperty

Document property management: read and write Visio document metadata like title, creator, subject, and keywords

**Actions:** `get`, `set`, `get-custom`, `set-custom`

| Parameter | Description |
|-----------|-------------|
| `--title` | Document title (required for: set) |
| `--subject` | Subject or topic (required for: set) |
| `--author` | Creator/author name (required for: set) |
| `--keywords` | Keywords for search (comma-separated) (required for: set) |
| `--comments` | Description or comments (required for: set) |
| `--company` | Company or organization name (required for: set) |
| `--category` | Category (required for: set) |
| `--property-name` | Custom property name. Stored as document Shape Data when written through this API. (required for: get-custom, set-custom) |
| `--property-value` | Property value (string) (required for: set-custom) |


### export

Export Visio documents and pages to portable output formats

**Actions:** `to-pdf`, `to-xps`, `page-export`, `print`, `save-copy`

| Parameter | Description |
|-----------|-------------|
| `--destination-path` | Output PDF path (required for: to-pdf, to-xps, page-export, save-copy) |
| `--from-page` | Optional 1-based start page for ranged export |
| `--to-page` | Optional 1-based end page for ranged export |
| `--page-index` | 1-based page index (required for: page-export) |
| `--copies` | Number of copies (default 1) (required for: print) |


### file

File management commands for Visio documents. Handles file validation and metadata retrieval

**Actions:** `test`

| Parameter | Description |
|-----------|-------------|
| `--file-path` | Path to the .vsdx, .vsdm or .vssx file (required) |


### hyperlink

Hyperlinks attached to shapes

**Actions:** `list`, `list-for-shape`, `read`, `add`, `update`, `delete`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | 1-based page index (required for: list-for-shape, read, add, update, delete) |
| `--shape-name` | Shape name, exactly as reported by shape(list) (required for: list-for-shape, read, add, update, delete) |
| `--hyperlink-name` | Hyperlink row name, exactly as reported by list (required for: read, update, delete) |
| `--address` | External target such as 'https://example.com', 'mailto:a@b.com' or a file path. Leave empty for a link that only navigates inside the document |
| `--sub-address` | Target inside the document: a page name such as 'Page-2', or 'Page-2/Rectangle' for a shape |
| `--description` | Text Visio displays for the link, shown on hover |
| `--new-window` | Open the target in a new window |


### layer

Visio layer management for page-level organization, visibility, and shape membership

**Actions:** `list`, `read`, `create`, `delete`, `add-shape`, `remove-shape`, `set-visibility`, `set-print`, `set-lock`, `set-color`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | 1-based page index (required) |
| `--layer-name` | Layer name, as reported by list (required for: read, create, delete, add-shape, remove-shape, set-visibility, set-print, set-lock, set-color) |
| `--color-index` | Visio layer palette index. 255 is the default, meaning shapes keep their own colours rather than taking the layer colour (required for: set-color) |
| `--visible` | True to show the layer in the drawing window (required for: set-visibility) |
| `--printable` | True to include the layer when the page is printed or exported (required for: set-print) |
| `--locked` | True to prevent shapes on the layer from being selected or edited (required for: set-lock) |
| `--shape-name` | Shape to add, as reported by shape(list) (required for: add-shape, remove-shape) |
| `--preserve-membership` | True keeps the shape's existing layer memberships; false makes this its only layer. A Visio shape may belong to several layers at once |


### master

Masters held inside the working document

**Actions:** `list`, `read`, `create-from-shape`, `rename`, `delete`, `list-instances`

| Parameter | Description |
|-----------|-------------|
| `--master-name` | Master name, exactly as reported by list (required for: read, rename, delete, list-instances) |
| `--page-index` | 1-based page index holding the shape to promote (required for: create-from-shape) |
| `--shape-name` | Shape to copy into the document's masters. The shape itself is left where it is (required for: create-from-shape) |
| `--new-name` | New name for the master (required for: rename) |


### shapealign

Shape alignment and distribution operations

**Actions:** `align`, `distribute`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | 1-based page index (required) |
| `--shape-names` | Comma-separated shape names (required) |
| `--align-type` | Alignment type (0-5) (required for: align) |
| `--distribute-type` | 0=Horizontally, 1=Vertically (required for: distribute) |


### style

Named formatting held by the document

**Actions:** `list`, `read`, `create`, `rename`, `delete`, `read-formula`, `set-formula`, `apply`

| Parameter | Description |
|-----------|-------------|
| `--style-name` | Style name, exactly as reported by list (required for: read, create, rename, delete, read-formula, set-formula, apply) |
| `--based-on` | Style to inherit from, such as 'Normal'. Omit for a style that inherits nothing |
| `--includes-fill` | Whether the style carries fill formatting |
| `--includes-line` | Whether the style carries line formatting |
| `--includes-text` | Whether the style carries text formatting |
| `--new-name` | New name for the style (required for: rename) |
| `--cell-name` | ShapeSheet cell name, for example FillForegnd, LineWeight or Char.Size (required for: read-formula, set-formula) |
| `--formula` | ShapeSheet expression, for example 'RGB(200,30,30)' or '3 pt' (required for: set-formula) |
| `--page-index` | 1-based page index (required for: apply) |
| `--shape-name` | Shape name, exactly as reported by shape(list) (required for: apply) |
| `--aspect` | Which part to apply: 'all' (default), or 'fill', 'line' or 'text' to apply only that one |


### window

Control the active Visio drawing window: zoom, viewport bounds, page navigation, visual aids, and snap strengths.

**Actions:** `set-zoom`, `get-zoom`, `get-viewport`, `fit-page`, `fit-selection`, `pan-to-shape`, `pan-by-offset`, `get-visual-aids`, `set-grid-visible`, `set-guides-visible`, `set-rulers-visible`, `set-drawing-aids`, `get-snap-settings`, `set-grid-snap-strength`, `set-guides-snap-strength`, `set-points-snap-strength`, `set-ruler-snap-strength`, `set-geometry-snap-strength`, `set-extensions-snap-strength`

| Parameter | Description |
|-----------|-------------|
| `--zoom-percent` | Zoom percentage (e.g. 100 for 100%) (required for: set-zoom) |
| `--page-index` | 1-based page index (required for: get-zoom, get-viewport, fit-page, fit-selection, pan-to-shape, pan-by-offset, get-visual-aids, set-grid-visible, set-guides-visible, set-rulers-visible) |
| `--shape-name` | Shape to centre in the drawing window, as reported by shape(list) (required for: pan-to-shape) |
| `--offset-x` | Horizontal pan distance in points. Positive moves the view right (required for: pan-by-offset) |
| `--offset-y` | Vertical pan distance in points. Positive moves the view up (required for: pan-by-offset) |
| `--visible` | True to show the grid (required for: set-grid-visible, set-guides-visible, set-rulers-visible) |
| `--enabled` | True to enable snap and glue (required for: set-drawing-aids) |
| `--strength` | Snap strength from 1 to 999. Higher values pull shapes to the grid from further away (required for: set-grid-snap-strength, set-guides-snap-strength, set-points-snap-strength, set-ruler-snap-strength, set-geometry-snap-strength, set-extensions-snap-strength) |

## Common Pitfalls

### Page Indices Are 1-Based

Page indices start at 1, not 0. `--page-index 0` is invalid and will error.

### --timeout Must Be Greater Than Zero

When using `--timeout`, the value must be a positive integer (seconds). `--timeout 0` is invalid and will error. Omit `--timeout` entirely to use the default (300 seconds for most operations).

### Shape Names Must Be Exact

Shape names are case-sensitive and must match exactly. Use `shape list` to discover correct names before targeting shapes.

### List Parameters Are Comma-Separated

`--shape-names` takes a comma-separated list, not a JSON array. Quote the whole value so the
shell keeps it as one argument:
```powershell
# CORRECT
--shape-names "Rectangle,Rectangle.5,Ellipse.7"

# WRONG: a JSON array is not parsed
--shape-names '["Rectangle","Ellipse.7"]'
```

## Reference Documentation

- [Core execution rules and LLM guidelines](./references/behavioral-rules.md)
- [Generation pipeline](./references/generation-pipeline.md)
- [Visible session mode](./references/visible-session-mode.md)

For design catalog data (archetypes, palettes, grids, and styles), use the `design` CLI command:
- `visiocli design list-archetypes` / `visiocli design get-archetype --archetype-id ...` — primary unified archetype surface; includes learned subtypes and concrete sanitized example details when local reference data is available
- `visiocli design list-palettes` / `visiocli design get-palette --palette-id ...`
- `visiocli design list-layout-grids` / `visiocli design get-layout-grid --grid-id ...`
- `visiocli design list-style-profiles` / `visiocli design get-style-profile --profile-id ...`
- `visiocli design get-context-model` — context → density mapping

Reference examples are exposed only as sanitized ids/details embedded in `get-archetype`. Raw filenames and provenance metadata remain in local gitignored reference catalog data and are not part of the public CLI surface.
