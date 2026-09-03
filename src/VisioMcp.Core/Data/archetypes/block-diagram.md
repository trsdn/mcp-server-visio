# Block Diagram

**When:** Components and relationships where no formal notation applies — architecture layers,
capability maps, a concept sketch. The fallback when no other archetype fits.

Prefer a specific archetype where one exists. `flowchart` for a process, `bpmn-process` for
notation, `system-context` for scope. A block diagram carries no shared vocabulary, so every
meaning must be stated on the page.

**Stencils:** `BASIC_U.VSSX` for blocks, `ARROWS_M.VSSX` for emphasis on a relationship.

## Because notation is absent, consistency is the notation

Decide once, then hold it:

| Distinction | Encode as |
|---|---|
| Layer or tier | Vertical position |
| Grouping | Enclosing rectangle, sent to back |
| Kind of component | Shape: `Rectangle`, `Hexagon`, `Circle` |
| Status or ownership | Fill colour — with a legend |
| Relationship | Connector, labelled |

Two encodings for one distinction is redundancy; one encoding for two distinctions is ambiguity.
Both mislead.

## Layout

```
2.00 in   block width
0.80 in   block height
0.30 in   gap within a group
0.90 in   gap between groups
0.50 in   padding inside a group rectangle
```

Align to a grid. Blocks that are almost aligned read as an error rather than as a distinction — and
`shapealign(align)` costs one call.

Layered architectures read top-down with the user-facing layer at the top. Pipelines read
left-to-right. Do not mix on one page.

## Groups

```
stencil(drop-master, stencil_path='BASIC_U.VSSX', master_name='Rectangle', ...)
cell(set-formula, sheet_target='shape', shape_name='DataLayer', cell_name='FillForegnd', formula='RGB(248,248,248)')
cell(set-formula, sheet_target='shape', shape_name='DataLayer', cell_name='LinePattern',  formula='2')
shape(z-order, shape_name='DataLayer', z_order_cmd=2)
```

Send the group rectangle to back **before** placing its members, or it covers them.

## Arrows

A connector shows a relationship. An `ARROWS_M.VSSX` master shows a relationship you want the
reader to notice first — a primary data flow, the path being changed. Use at most one or two per
page; when everything is emphasised, nothing is.

## Anti-patterns

**Unexplained colour.** The most common failure in this archetype, because nothing constrains it.
Add a legend (`LEGEND_M.VSSX`) or drop the colour.

**Blocks at arbitrary sizes.** Size reads as importance. If it does not mean that, keep sizes equal.

**Nested groups more than two deep.** Split across pages instead; the third level stops being
readable.

**Arrows in both directions between the same pair.** Two relationships, two labels, two connectors.

**Using this archetype for a process.** If the blocks have an order and a decision, it is a
flowchart, and a `Decision` master will say so far better than a labelled rectangle.
