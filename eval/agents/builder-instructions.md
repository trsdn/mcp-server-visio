# Builder Agent Instructions (CLI)

You build ONE Visio drawing using the `visiocli` command-line tool, from a user prompt.

Use the CLI, not the MCP server. Do not explore both.

## Workflow

```powershell
visiocli session create "<path>.vsdx"          # returns a sessionId
visiocli page set-name -s <id> --page-index 1 --page-name "..."
visiocli stencil drop-master -s <id> --page-index 1 --stencil-path "BASFLO_M.VSSX" --master-name "Process" ...
visiocli shape connect-shapes -s <id> --page-index 1 --shape-names "A,B"
visiocli text set -s <id> --page-index 1 --shape-name "..." --text "..."
visiocli export page-export -s <id> --page-index 1 --destination-path "<path>.png"
visiocli session close -s <id> --save true
```

Every command after `session create` takes `-s <sessionId>`.

## Syntax that bites

- **`session close` takes an explicit value**: `--save true` or `--save false`. There is no
  `--no-save`.
- **`export page-export` has no width or height.** It exports the page as it is; set the page size
  through `cell` if the drawing needs a different one.
- **Only one session can be open at a time.** `session create` fails while another is open —
  close it first.
- **Options are ignored silently if the action does not take them.** Passing `--text` to
  `shape add-shape` returns `success: true` and produces an unlabelled shape (#103). Label with
  `text set`.
- **Page and shape indices are 1-based.**

## The two rules that decide your score

**Drop masters; do not draw shapes.** `stencil drop-master` places a real `Process` or `Decision`.
`shape add-shape` draws a rectangle that merely resembles one — it carries no shape data and
nothing downstream treats it as a decision. Check the stencil and master names first with
`visiocli design get-stencil-catalog`; a master that is not installed will fail.

**Connect the shapes.** `shape connect-shapes` joins them; two boxes with a line between them does
not. Verify with:

```powershell
visiocli shape list-connectors -s <id> --page-index 1
```

Every connector must report both `startShapeName` and `endShapeName`. An unconnected diagram
renders as a perfectly plausible PNG and is worthless — the judge reads the structure, and caps
such a drawing at 12 out of 20 however handsome it is.

## Design Reference

Follow `skills/shared/diagram-design-principles.md`, `diagram-design-review.md` and
`generation-pipeline.md`. Per-family detail — stencil, masters, variants, anti-patterns — is in
`src/VisioMcp.Core/Data/archetypes/`, starting from `registry.md`.

Catalogue data is available through the CLI too:

```powershell
visiocli design list-archetypes
visiocli design get-archetype --archetype-id flowchart
visiocli design get-stencil-catalog
visiocli design get-diagram-patterns
visiocli design get-palette --palette-id <id>
```

Every command supports `--help`; use it rather than guessing an option name.

## Before you finish

- every node carries a specific label, not "Step 1"
- `shape list-connectors` shows both endpoints for every connector
- no orphans; every path reaches a terminal
- the session is closed with `--save true`

## Output

Stop once the PNG exists and the session is closed. Stay in the conversation so the harness can
request a structured summary; answer with JSON only, using the `builder-summary/v1` envelope it
provides.

When the envelope includes `builderCarryover` or `reviewerCarryover`, use those objects directly as
prior-loop context.
