# Archetype Registry

Nine diagram families. Choose the family first, then load its detail file for the stencil, the
masters to drop, layout conventions and the mistakes that make a diagram look plausible but wrong.

Every stencil and master named here was verified present on a stock Visio install. Where a family
would naturally use a stencil that is **not** installed by default, the detail file says so and
gives a fallback built from `BASIC_U.VSSX`.

## Decision tree

```
What are you drawing?
├─ A process with steps and decisions?
│  ├─ Who does each step matters?      → cross-functional-flowchart
│  ├─ Must follow BPMN notation?       → bpmn-process
│  └─ Otherwise                        → flowchart
├─ Reporting lines and roles?          → org-chart
├─ Servers, devices and links?         → network-diagram
├─ One system and what surrounds it?   → system-context
├─ How failures combine?               → fault-tree
├─ Explaining an existing diagram?     → annotated-diagram
└─ Components with no formal notation? → block-diagram
```

## Family index

| ID | Stencil | Key masters |
|----|---------|-------------|
| `flowchart` | `BASFLO_M.VSSX` | Start/End, Process, Decision, Subprocess |
| `cross-functional-flowchart` | `BASFLO_M.VSSX` | Process, Decision + drawn lane bands |
| `bpmn-process` | `BPMN_M.VSSX` | Start Event, Task, Gateway, End Event, Pool / Lane |
| `org-chart` | `ORGCH_M.VSSX` | Executive, Manager, Position, Assistant |
| `network-diagram` | `PERIPH_M.VSSX` | Server, Router, Switch, Firewall |
| `system-context` | `BASIC_U.VSSX` | Rectangle, Ellipse |
| `block-diagram` | `BASIC_U.VSSX` | Rectangle, Diamond, Hexagon |
| `fault-tree` | `FAULT_M.VSSX` | AND gate, OR gate, Basic event |
| `annotated-diagram` | `CALOUT_M.VSSX` | Annotation, Side box callout |

## Rules that apply to every family

**Connect shapes; do not merely place them.** A diagram whose boxes are positioned but never
joined is the most common way generated output is wrong while still looking right in a screenshot.
Use `shape(connect-shapes, shape_names='A,B,C')`, which chains in order and produces Visio dynamic
connectors that route around obstacles and follow the shapes when they move. Drawing a line between
two boxes is not a connector.

**Drop masters; do not draw primitives.** `stencil(drop-master)` gives a shape that carries its
notation, its connection points and its text behaviour. `shape(add-shape)` draws only a rectangle
or an ellipse — a hand-drawn diamond is not a Decision, and nothing downstream will treat it as one.

**Name what you place.** Every later operation addresses a shape by name. Set it once, at creation.

**One idea per page.** Visio pages are cheap: `page(create)`. A second page beats a crowded first
one. Where a process continues across pages, use the On-page/Off-page reference masters rather
than an arrow that stops at the margin.

**Restraint is not decoration.** Neutral fills, one accent, and semantic red/green only where the
content genuinely means risk or success. Novelty masters — sun, heart, cloud as ornament — say
something the content does not support.

**Page size before layout.** Set `PageWidth`/`PageHeight` through
`cell(set-formula, sheet_target='page')` before placing shapes; changing it afterwards does not
reflow anything.
