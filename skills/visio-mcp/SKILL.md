---
name: visio-mcp
description: >
  Automate Microsoft Visio on Windows via COM interop. Use when creating, reading,
  or modifying Visio diagrams. Best current support covers sessions, pages, shapes,
  text, ShapeSheet cells, and stencil masters.
  Triggers: Visio, vsdx, diagram, page, shape, stencil, ShapeSheet.
---

# Visio MCP Server Skill

Provides 166 generated operations via Model Context Protocol. The current Visio MVP is centered on document sessions, pages, shapes, text, ShapeSheet cells, and stencil masters. The MCP Server forwards all requests to the shared VisioMcp Service, enabling session sharing with CLI.

## Workflow Checklist

| Step | Tool | Action | When |
|------|------|--------|------|
| 1. Open file | `file` | `open` or `create` | Always first |
| 2. Manage pages | `page` | `list`, `create`, `read` | Choose the working page |
| 3. Add shapes | `shape` or `stencil` | `add-shape`, `add-textbox`, `drop-master` | Add diagram elements |
| 4. Edit text | `text` | `get`, `set`, `find`, `replace` | Update labels |
| 5. Edit cells | `cell` | `read`, `write`, `set-formula` | Adjust ShapeSheet-backed geometry |
| 6. Save & close | `file` | `close` with `save: true` | Always last |

## Preconditions

- Windows host with Microsoft Visio installed
- Use full Windows paths: `C:\Users\Name\Documents\Diagram.vsdx`
- Visio files should not be open in another Visio instance during automation

## Large Diagram Pattern

For larger builds, use a **single controlling client** with these logical phases:

1. Plan
2. Execute
3. Verify
4. Improve

Important:

- Keep this orchestration in the client, not in the MCP server
- Use normal sequential MCP calls
- Do **not** assume MCP batch execution or subagents are available

## Page and Shape Workflow

Use `page`, `shape`, `text`, `cell`, and `stencil` together for the current Visio MVP:

```
1. page(action: 'create', pageName: 'Overview')  → Add a page if needed
2. shape(action: 'add-shape', pageIndex: 1, ...)  → Draw a basic shape
3. text(action: 'set', pageIndex: 1, shapeName: 'Rectangle.1', text: 'Start')  → Label it
4. cell(action: 'write', pageIndex: 1, shapeName: 'Rectangle.1', cellName: 'Width', value: '3')  → Refine geometry
5. stencil(action: 'drop-master', pageIndex: 1, stencilPath: '...', masterName: 'Rectangle', xPosition: 2, yPosition: 2)  → Use a built-in master
```

**Note:** Shape names should be discovered with `shape(list)` before later operations target them.

## CRITICAL: Execution Rules (MUST FOLLOW)

### Rule 1: NEVER Ask Clarifying Questions

**STOP.** If you're about to ask "Which file?", "What table?", "Where should I put this?" - DON'T.

| Bad (Asking) | Good (Discovering) |
|--------------|-------------------|
| "Which Visio file should I use?" | `file(list)` → use the open session |
| "What shapes are on this page?" | `shape(list)` → discover shapes |
| "Which page has the content?" | `page(list)` → inspect pages |
| "Which master should I drop?" | `stencil(list-masters)` → inspect the stencil |

**You have tools to answer your own questions. USE THEM.**

### Rule 2: Always End With a Text Summary

**NEVER end your turn with only a tool call.** After completing all operations, always provide a brief text message confirming what was done. Silent tool-call-only responses are incomplete.

### Rule 3: Build Pages Incrementally

Build the diagram in inspectable steps:

| Element | Property | Example |
|---------|----------|---------|
| Page | Name | `Overview` |
| Shape | Geometry | `PinX`, `PinY`, `Width`, `Height` |
| Text | Label | task or connector text |
| Master | Stencil source | built-in Visio stencil |

**Workflow:**
```
1. page create/list (select target page)
2. shape or stencil drop (add visual elements)
3. text set (add content)
4. cell write/set-formula (fine tune geometry)
```

### Rule 4: Prefer Pages and Stencils Over Slide-era Concepts

Prefer Visio-native pages, masters, and ShapeSheet cells. Avoid slide-era assumptions when a page, stencil, or cell workflow exists.

### Rule 5: Session Lifecycle

```
1. file(action: 'open', path: '...')  → sessionId
2. All operations use sessionId
3. file(action: 'close', save: true)  → saves and closes
```

**Unclosed sessions leave Visio processes running, locking files.**

### Rule 6: Use Stencil Masters When Available

Diagram shapes often come from stencils:

```
Step 1: stencil(action: 'list-masters', stencilPath: '...')  → Discover master names
Step 2: stencil(action: 'drop-master', pageIndex: 1, masterName: '...')  → Drop the master
Step 3: text(action: 'set', ...) or cell(action: 'write', ...)  → Customize it
```

### Rule 7: Use ShapeSheet Cells Deliberately

Use `cell` for core geometry and formula-backed editing:

```
1. cell(action: 'read', cellName: 'Width')  → Inspect the current value
2. cell(action: 'write', cellName: 'Width', value: '3')  → Set a literal value
3. cell(action: 'set-formula', cellName: 'PinX', formula: 'GUARD(Width*0.5)')  → Set a formula
```

Use universal-style cell names where possible and verify with a follow-up `cell(read)` or `cell(list)`.

### Rule 8: Targeted Updates Over Delete-Rebuild

- **Prefer**: Modifying page, shape, text, or cell properties directly
- **Avoid**: Deleting and recreating whole pages or shapes unless necessary

**Why:** Preserves names, formulas, and downstream references.

### Rule 9: Follow suggestedNextActions

Error responses include actionable hints:
```json
{
  "success": false,
  "errorMessage": "Shape 'Rectangle.1' not found on page 1",
  "suggestedNextActions": ["shape(action: 'list', pageIndex: 1)"]
}
```

### Rule 10: Verify Actual Visio State After Changes

After writes, read back the page, shape, text, or cell you changed:

```
1. shape(list) or shape(read) after creating shapes
2. text(get) after writing labels
3. cell(read) after geometry or formula changes
```

**When NOT needed:** trivial read-only inspection workflows.

## Tool Selection Quick Reference

| Task | Tool | Key Action |
|------|------|------------|
| Create/open/save diagrams | `file` | open, create, close |
| Create/manage pages | `page` | create, list, read |
| Add/modify shapes | `shape` | add-shape, add-textbox, move-resize, delete |
| Set text content | `text` | set, get |
| Edit ShapeSheet cells | `cell` | read, write, set-formula |
| Use stencil masters | `stencil` | list-masters, drop-master |

## Reference Documentation

See `references/` for detailed guidance:

- [Core execution rules and LLM guidelines](./references/behavioral-rules.md)
- [Generation pipeline](./references/generation-pipeline.md)
- [Visible session mode](./references/visible-session-mode.md)

For design catalog data (archetypes, palettes, grids, and styles), use the `design` tool:
- `design(list-archetypes)` / `design(get-archetype)` — Primary unified archetype surface with curated layout guidance plus learned subtypes and concrete sanitized example details when local reference data is available
- `design(list-palettes)` / `design(get-palette)` — Color palettes with hex values
- `design(list-layout-grids)` / `design(get-layout-grid)` — Exact positioning coordinates
- `design(list-style-profiles)` / `design(get-style-profile)` — Style configurations
- `design(get-context-model)` — Context → density mapping
- `design(get-icon-shapes)` — Native shape icon catalog

Reference examples are exposed only as sanitized ids/details embedded in `design(get-archetype)`. Raw filenames and source provenance remain in local gitignored reference data and never appear in MCP responses.
