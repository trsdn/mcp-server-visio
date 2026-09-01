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

Visio ShapeSheet cell operations for reading and writing shape-level cells.

**Actions:** `read`, `read-formula`, `write`, `set-formula`, `list`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required) |
| `--shape-name` | (required) |
| `--cell-name` | (required for: read, read-formula, write, set-formula) |
| `--value` | (required for: write) |
| `--formula` | (required for: set-formula) |



### page

Visio page lifecycle, guides, and routing commands.

**Actions:** `list`, `read`, `create`, `set-name`, `delete`, `list-guides`, `add-guide`, `set-guide-position`, `delete-guide`, `get-routing-settings`, `set-route-style`, `set-connector-routing-extension`, `set-line-jump-code`, `set-line-jump-style`, `set-walk-preference`, `set-place-style`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required for: read, set-name, delete, list-guides, add-guide, set-guide-position, delete-guide, get-routing-settings, set-route-style, set-connector-routing-extension, set-line-jump-code, set-line-jump-style, set-walk-preference, set-place-style) |
| `--position` | (required for: create) |
| `--name` | (required for: create, set-name) |
| `--guide-type` | (required for: add-guide) |
| `--x-position` | (required for: add-guide, set-guide-position) |
| `--y-position` | (required for: add-guide, set-guide-position) |
| `--guide-name` | (required for: set-guide-position, delete-guide) |
| `--route-style` | (required for: set-route-style) |
| `--connector-routing-extension` | (required for: set-connector-routing-extension) |
| `--line-jump-code` | (required for: set-line-jump-code) |
| `--line-jump-style` | (required for: set-line-jump-style) |
| `--walk-preference` | (required for: set-walk-preference) |
| `--place-style` | (required for: set-place-style) |



### shape

Shape management: list, read, create, move, resize, delete, z-order, group, connect.

**Actions:** `list`, `read`, `list-groups`, `read-group`, `list-selection`, `select-shapes`, `add-to-selection`, `remove-from-selection`, `clear-selection`, `list-properties`, `get-property`, `set-property`, `delete-property`, `list-connectors`, `read-connector`, `list-connections`, `disconnect-connector`, `reconnect-connector`, `add-textbox`, `add-shape`, `move-resize`, `delete`, `z-order`, `group`, `ungroup`, `add-connector`, `merge`, `duplicate`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required) |
| `--shape-name` | (required for: read, read-group, list-properties, get-property, set-property, delete-property, read-connector, list-connections, disconnect-connector, reconnect-connector, move-resize, delete, z-order, ungroup, duplicate) |
| `--shape-names` | (required for: select-shapes, add-to-selection, remove-from-selection, group, merge) |
| `--property-name` | Shape Data row name to read (required for this action) |
| `--property-value` | Value to store in the Shape Data row (required for this action) |
| `--connector-end` | Connector endpoint selector: start or end (required for: disconnect-connector, reconnect-connector) |
| `--target-shape-name` | Target shape name for the selected endpoint (required for: reconnect-connector) |
| `--left` | Position from left in points (required for: add-textbox, add-shape) |
| `--top` | Position from top in points (required for: add-textbox, add-shape) |
| `--width` | Width in points (required for: add-textbox, add-shape) |
| `--height` | Height in points (required for: add-textbox, add-shape) |
| `--text` | Initial text content (required for: add-textbox) |
| `--auto-shape-type` | MsoAutoShapeType integer (1=Rectangle, 9=Oval, etc.) (required for: add-shape) |
| `--z-order-cmd` | 1=BringToFront, 2=SendToBack, 3=BringForward, 4=SendBackward (required for: z-order) |
| `--connector-type` | 1=Straight, 2=Elbow, 3=Curve (required for: add-connector) |
| `--start-shape-name` | Starting shape name (required for: add-connector) |
| `--end-shape-name` | Ending shape name (required for: add-connector) |
| `--merge-type` | 1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract (required for: merge) |



### stencil

Visio stencil operations for listing masters and dropping them onto pages.

**Actions:** `list-masters`, `drop-master`

| Parameter | Description |
|-----------|-------------|
| `--stencil-path` | (required) |
| `--page-index` | (required for: drop-master) |
| `--master-name` | (required for: drop-master) |
| `--x-position` | (required for: drop-master) |
| `--y-position` | (required for: drop-master) |



### text

Text operations within shapes: get, set, find, replace, word count.

**Actions:** `get`, `set`, `find`, `replace`, `word-count`

| Parameter | Description |
|-----------|-------------|
| `--page-index` | (required) |
| `--shape-name` | (required for: get, set) |
| `--text` | (required for: set) |
| `--search-text` | Text to find (required for: find, replace) |
| `--replace-text` | Replacement text (required for: replace) |




## Common Pitfalls

### Page Indices Are 1-Based

Page indices start at 1, not 0. `--page-index 0` is invalid and will error.

### --timeout Must Be Greater Than Zero

When using `--timeout`, the value must be a positive integer (seconds). `--timeout 0` is invalid and will error. Omit `--timeout` entirely to use the default (300 seconds for most operations).

### Shape Names Must Be Exact

Shape names are case-sensitive and must match exactly. Use `shape list` to discover correct names before targeting shapes.

### JSON Values Format

`--values` takes a JSON string wrapped in single quotes:
```powershell
# CORRECT: JSON with single-quote wrapper
--values '{"text": "Hello World", "fontSize": 24}'

# WRONG: Missing quotes
--values {text: Hello World}
```

### List Parameters Use JSON Arrays

Parameters that accept lists require JSON array format:
```powershell
# CORRECT: JSON array with single-quote wrapper
--shape-names '["Start","End"]'

# WRONG: Comma-separated string (not valid)
--shape-names "Start,End"
```

## Reference Documentation

- [Core execution rules and LLM guidelines](./references/behavioral-rules.md)
- [Generation pipeline](./references/generation-pipeline.md)
- [Agent mode patterns](./references/visio_agent_mode.md)
- [Diagram design principles](./references/diagram-design-principles.md)
- [Diagram self-review checklist](./references/diagram-design-review.md)
