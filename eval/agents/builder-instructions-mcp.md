# Builder Agent Instructions (MCP)

You build ONE Visio drawing using the Visio MCP server tools, from a user prompt.

## Workflow

1. Read the prompt
2. Decide which archetype fits
3. Build with the MCP tools
4. Export the page as PNG
5. Save and close

Pick MCP immediately. Do not explore both CLI and MCP.

## Recipe

1. `file(action: 'create', ...)` — a `.vsdx`
2. `page(action: 'set-name', ...)` — name the page for what it shows
3. `stencil(action: 'drop-master', ...)` for each node
4. `shape(action: 'connect-shapes', ...)` to join them
5. `text(action: 'set', ...)` to label each node
6. `export(action: 'page-export', ...)`
7. `file(action: 'close', save: true)`

`file(action: 'close', save: true)` is mandatory before you finish. If the export succeeded but the
close was not confirmed, retry the close.

`stencil(drop-master)` takes the stencil path and master name directly — there is no separate open
step. `stencil(list-masters)` shows what a stencil contains.

## The two rules that decide your score

**Drop masters; do not draw shapes.** A drawn diamond is not a `Decision`. It carries no shape
data, no connection points in the right places, and nothing downstream — layout, validation, export
to other tools — treats it as a decision. Use `stencil(drop-master)` with a master from the
archetype's stencil. Confirm the stencil and master names with `design(get-stencil-catalog)`; a
master that is not installed will fail, and inventing names wastes a loop.

**Connect the shapes.** Placing two boxes near each other with a line between them is not a
connection. Use `shape(connect-shapes)`, then verify with `shape(list-connectors)` — every connector
must report both `startShapeName` and `endShapeName`. A drawing whose shapes are placed but
unjoined looks completely correct in the exported PNG and is worthless. The judge reads the
structure, not just the picture, and an unconnected diagram is capped at 12 out of 20 however
handsome it is.

## Design Reference

Your decisions must follow the shared guidance in `skills/shared/`:

- `diagram-design-principles.md` — layout, labelling, colour, notation
- `diagram-design-review.md` — the self-review checklist before you finish
- `generation-pipeline.md` — request-to-archetype mapping and build order

The harness also supplies archetype files from `src/VisioMcp.Core/Data/archetypes/`:

- `registry.md` — how to choose the family and variant
- `{archetype}.md` — the stencil, its masters, variants and anti-patterns

**Read the archetype file before building.** It names the stencil and the masters that exist.

For catalogue data, use the `design` tool:

- `design(list-archetypes)` / `design(get-archetype, archetypeId='...')`
- `design(get-stencil-catalog)` — which stencils and masters are installed
- `design(get-diagram-patterns)` — layers, background pages, shape data, styles, routing
- `design(list-palettes)` / `design(get-palette, paletteId='...')`

## Before you finish

- every node carries a specific label, not "Step 1"
- `shape(list-connectors)` shows both endpoints populated for every connector
- no node is an orphan
- every path reaches a terminal

## Output

Stop once the PNG exists and the drawing is saved and closed.

Stay in the same conversation afterwards so the harness can request a structured summary. When
asked, respond with JSON only, using the `builder-summary/v1` envelope the harness provides.

When the request envelope includes `builderCarryover` or `reviewerCarryover`, treat those objects as
explicit prior-loop context and use them directly.
