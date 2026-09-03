# Builder Agent Instructions (MCP, catalogue-first)

You build ONE Visio drawing using the Visio MCP server tools. This variant differs from
`builder-instructions-mcp.md` in one respect: **you consult the design catalogue before you build,
not after you get stuck.**

## Workflow

1. Read the prompt
2. `design(list-archetypes)` — pick the family
3. `design(get-archetype, archetypeId='...')` — read its stencil, masters and variants
4. `design(get-stencil-catalog)` — confirm the stencil and masters are installed
5. `design(get-palette, paletteId='...')` — take the colours from the palette, do not invent them
6. Build
7. Export the page as PNG
8. Save and close

Steps 2–5 cost one round trip each and remove the most expensive failure mode: inventing a master
that is not installed, discovering it at drop time, and burning the loop.

## Recipe

1. `file(action: 'create', ...)` — a `.vsdx`
2. `page(action: 'set-name', ...)`
3. `stencil(action: 'drop-master', ...)` per node — no separate open step
4. `shape(action: 'connect-shapes', ...)`
5. `text(action: 'set', ...)` per node
6. `export(action: 'page-export', ...)`
7. `file(action: 'close', save: true)` — mandatory

## The two rules that decide your score

**Drop masters; do not draw shapes.** A drawn diamond is not a `Decision`. It carries no shape data
and no meaningful connection points, and nothing downstream treats it as a decision. The catalogue
exists so you never have to guess a master name.

**Connect the shapes.** Two boxes with a line near them is not a connection. Use
`shape(connect-shapes)` and verify with `shape(list-connectors)` — both `startShapeName` and
`endShapeName` must be populated. An unconnected diagram renders as a perfectly plausible PNG and is
worthless; the judge reads the structure and caps it at 12 out of 20.

## Applying the catalogue

- **Archetype** — `design(get-archetype)` gives the stencil, its masters, and the variants. Choose
  the variant deliberately; "linear" and "branching" are not the same diagram.
- **Stencils** — `design(get-stencil-catalog)` is the authoritative list of what is installed on
  this machine. Anything absent from it will fail.
- **Patterns** — `design(get-diagram-patterns)` covers layers, background pages, shape data, named
  styles and connector routing. Reach for these when the drawing has repeated formatting, a shared
  title block, or metadata worth carrying.
- **Palette** — `design(get-palette)` returns hex values. Use colour to carry meaning; decorative
  colour scores worse than none.

## Design Reference

`skills/shared/diagram-design-principles.md`, `diagram-design-review.md` and
`generation-pipeline.md` remain authoritative for layout, labelling and build order. The archetype
files in `src/VisioMcp.Core/Data/archetypes/` carry the per-family detail.

## Before you finish

- every node carries a specific label
- every connector reports both endpoints
- no orphans; every path reaches a terminal
- colours came from the palette you fetched

## Output

Stop once the PNG exists and the drawing is saved and closed. Stay in the conversation so the
harness can request a structured summary; answer with JSON only, using the `builder-summary/v1`
envelope it provides.

When the envelope includes `builderCarryover` or `reviewerCarryover`, use those objects directly as
prior-loop context.
