# BPMN Process

**When:** A process that must follow BPMN notation — a technical, compliance or integration
audience who will read the symbols as specification rather than illustration.

If the audience will not read BPMN, `flowchart` communicates more with less.

**Stencil:** `BPMN_M.VSSX` (installed by default — 21 masters)

## Masters

| Master | Meaning |
|---|---|
| `Start Event` | Thin circle. Exactly one per pool, unless modelling multiple triggers deliberately |
| `Task` | Rounded rectangle. An atomic unit of work |
| `Gateway` | Diamond. A split or merge — the diamond decides nothing by itself |
| `Intermediate Event` | Double circle. Something that happens during the flow |
| `End Event` | Thick circle. Every path terminates in one |
| `Collapsed Sub-Process` | Task with a `+`. Detail lives elsewhere |
| `Expanded Sub-Process` | Detail shown inline |
| `Pool / Lane` | A participant. Sequence flow may not cross a pool boundary |
| `Data Object` | Data produced or consumed |
| `Data Store` | Persistent data |
| `Text Annotation` | Commentary, attached with `Association` |
| `Sequence Flow` | Order within one pool |
| `Message Flow` | Communication **between** pools |
| `Association` | Links an annotation or data object to a node |

## The rule that separates BPMN from a flowchart

**Sequence flow never crosses a pool boundary. Message flow always does.**

Two participants exchanging work is a message flow between pools, not an arrow from one task to
another. Getting this wrong makes the diagram wrong as a specification even when it looks correct.

`shape(connect-shapes)` produces a Visio dynamic connector, which is a sequence flow by default.
For a message flow, connect and then change the connector's appearance:

```
shape(connect-shapes, shape_names='Send request,Receive request')
cell(set-formula, sheet_target='shape', shape_name='Dynamic connector',
     cell_name='LinePattern', formula='2')          dashed, as BPMN requires
cell(set-formula, sheet_target='shape', shape_name='Dynamic connector',
     cell_name='BeginArrow', formula='10')          open circle at the source
```

## Layout

```
1.40 in   task width
0.90 in   task height
0.60 in   gap between sequenced tasks
2.20 in   pool height (single lane)
0.40 in   gateway size
```

Left-to-right within a pool. Pools stacked vertically. A start event at the far left of its pool,
end events at the right — a BPMN reader looks there first.

## Anti-patterns

**A gateway with one incoming and one outgoing flow.** It decides nothing; delete it.

**An unlabelled gateway.** A diverging gateway needs its condition on each outgoing flow, or the
diagram does not say when each path is taken.

**A task with no end event downstream.** Every path terminates. An unterminated path is
unimplementable.

**Sequence flow between pools.** The single most common BPMN error. Use message flow.

**Mixing BPMN masters with `BASFLO_M` masters.** A `Process` from the flowchart stencil looks
similar and means something different. Stay within `BPMN_M.VSSX`.

**Annotations as floating text boxes.** Use `Text Annotation` plus `Association`, so the note moves
with what it explains.
