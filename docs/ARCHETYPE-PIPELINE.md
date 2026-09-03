# Archetype Pipeline

How diagram-design knowledge reaches the runtime design surface.

## What this is now

This document previously described a two-layer pipeline: curated guidance alongside a "learned
reference catalog" built by triaging slide screenshots, classifying them in batches, and
regenerating a sanitized catalog.

**Every stage of that pipeline has been removed** (#74). The batch classifier and evaluator were
invoked by nothing; the reference catalog fixture was referenced by nothing and pointed at images
in a directory that never existed; the triage tool curated into that same directory. None of it had
a live consumer.

What remains is the layer that was actually load-bearing: **curated, verified archetype guidance**.

## The single knowledge layer

Human-authored guidance lives under `src\VisioMcp.Core\Data\archetypes\`:

- `registry.md` — how to choose a family and a variant
- `{archetype}.md` — one file per family

and the machine-readable index beside it, `src\VisioMcp.Core\Data\archetypes.json`.

Each family names:

- when to use it
- the stencil it draws from
- the masters within that stencil
- its variants
- anti-patterns

## Nine families, and why the number is exact

| Family | Stencil |
|---|---|
| `flowchart` | `BASFLO_M.VSSX` |
| `cross-functional-flowchart` | `BASFLO_M.VSSX` |
| `bpmn-process` | `BPMN_M.VSSX` |
| `org-chart` | `ORGCH_M.VSSX` |
| `network-diagram` | `PERIPH_M.VSSX` |
| `system-context` | `BASIC_U.VSSX` |
| `block-diagram` | `BASIC_U.VSSX` |
| `fault-tree` | `FAULT_M.VSSX` |
| `annotated-diagram` | `CALOUT_M.VSSX` |

The catalogue is limited to families whose stencil **is installed on the machine**, and
`DesignCatalogStencilTests` opens each stencil and asserts every master named in the catalogue
exists. A family whose masters cannot be dropped is guidance that fails at the moment of use, which
is worse than no guidance: the agent follows it, the drop fails, and it improvises.

Stencils checked and found absent on this installation — and therefore deliberately not catalogued
— include `CROSFN_M`, `WORKFL_M`, `BORDER_M`, `TIMEL_M`, `CHEN_M`, `VALUE_M` and `CONTAINER_U`.
`design(get-stencil-catalog)` is the authoritative list.

## Runtime Surface

```powershell
visiocli design list-archetypes
visiocli design get-archetype --archetype-id flowchart
visiocli design get-stencil-catalog
visiocli design get-diagram-patterns
visiocli design list-palettes
visiocli design get-palette --palette-id <id>
```

The same six actions are available as the `design` MCP tool.

## Evaluating and improving the guidance

`eval/` runs the loop that improves these files: a builder produces a drawing, a judge scores it
against `eval/criteria.md`, and an improver edits the guidance the score points at. The judge reads
the drawing's **structure** — pages, shapes, connectors — not only the exported image, because an
unconnected diagram renders as a perfectly plausible picture.

See [Eval Framework](../eval/README.md).

## Asset roots

Large or private eval assets live under `eval\input\`, `eval\output\`, `eval\results\` and
`eval\data\`. To keep them in a separate private clone, set `VISIOMCP_EVAL_ASSET_REPO_ROOT` to that
repository's root; `eval\lib\runtime\environment.mjs` reads it.

## Refresh Checklist

When changing archetype guidance:

1. Confirm the stencil and its masters are installed — `design(get-stencil-catalog)`
2. Update both `archetypes.json` and the family's `.md` file
3. Run `dotnet test --filter "FullyQualifiedName~DesignCatalogStencilTests"`, which opens each
   stencil and verifies every master
4. Add or update the matching config under `eval\configs\`
5. Update docs when the taxonomy changes

## Relationship to Other Docs

- [Eval Framework](../eval/README.md) — how the harness and its run artifacts work
- [Archetype Registry](../src/VisioMcp.Core/Data/archetypes/registry.md) — family selection
- [Agent Client Architecture](AGENT-CLIENT.md) — how the runtime agent consumes this knowledge
