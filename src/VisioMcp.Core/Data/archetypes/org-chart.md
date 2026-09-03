# Organisation Chart

**When:** Reporting lines and roles. Who reports to whom, where the gaps are, and which
relationships are advisory rather than managerial.

**Stencil:** `ORGCH_M.VSSX` (installed by default — 14 masters)

## Masters

| Master | Use for |
|---|---|
| `Executive` | The root, or a distinct leadership tier |
| `Manager` | Anyone with direct reports below them |
| `Position` | An individual contributor |
| `Consultant` | External or contract |
| `Assistant` | Reports to a role but sits outside its reporting line |
| `Vacancy` | An open role — draw it; an org chart that hides gaps misleads |
| `Staff` | A support function attached to a manager |
| `Team frame` | A boundary around a group that works together across reporting lines |
| `Dotted-line report` | An advisory or matrix relationship |

Choosing `Manager` versus `Position` is not cosmetic: a reader infers span of control from it.

## Layout

```
1.80 in   box width
0.70 in   box height
0.40 in   horizontal gap between siblings
1.00 in   vertical gap between tiers
```

Top-down, siblings on one horizontal line. Centre a parent over the span of its children — an
off-centre parent reads as belonging to the nearest child.

Beyond roughly 20 boxes, split by division across pages and put a `Subprocess`-style pointer on the
summary page. A wall of boxes communicates less than three readable pages.

## Dotted-line reporting

A matrix relationship is a different kind of edge and must look different:

```
shape(connect-shapes, shape_names='Engineer,Product lead')
cell(set-formula, sheet_target='shape', shape_name='Dynamic connector',
     cell_name='LinePattern', formula='2')
```

Two solid managers for one person is an error. A solid and a dotted line is a matrix. The diagram
should be able to tell them apart at a glance.

## Build order

1. Root first, at the top centre.
2. Each tier left to right, spacing siblings evenly before descending.
3. Connect each parent to its children in one call:
   `shape(connect-shapes, shape_names='VP,Manager A')` — one call per parent-child pair keeps the
   chaining honest, since `connect-shapes` chains sequentially rather than fanning out.
4. Dotted lines last, so they are visibly secondary.

Put names and titles in the shape text, one per line:
`text(set, shape_name='...', text='Alex Chen\nHead of Platform')`.

## Anti-patterns

**Chaining a whole tier in one `connect-shapes` call.** `shape_names='VP,A,B,C'` produces
VP→A→B→C — a chain, not a hierarchy. Call once per parent-child pair.

**Hiding vacancies.** A chart drawn only from filled roles overstates capacity. `Vacancy` exists
for this.

**Photographs.** They double the box size and halve the information density; the chart stops fitting
on a page.

**Colour as unexplained meaning.** If boxes are coloured by department, say so with a legend
(`LEGEND_M.VSSX`) or in a caption. Unexplained colour invites the wrong inference.

**Reporting lines drawn as plain lines.** Use connectors, so the chart survives a reorganisation
without being redrawn.
