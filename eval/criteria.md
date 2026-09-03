# Evaluation Scoring Criteria

Score each generated Visio drawing on these 10 dimensions (0–2 each, max total 20).

## Two kinds of evidence

The harness gives the judge **two** artefacts, and they are not interchangeable:

- a **PNG** of the page, exported via `export page-export`
- a **structural read** of the drawing: pages, shapes, and the connectors between them, taken
  through `page list`, `shape list` and `shape list-connectors`

Dimensions 1–4 are **structural** and must be scored from the structural read. Do not score them by
looking at the picture. A diagram whose boxes are placed but never joined renders as an entirely
plausible image — nothing in a PNG distinguishes a connected flowchart from a tidy scattering of
rectangles with lines near them. This is the single most common way generated output looks right
and is useless.

Dimensions 5–10 are **visual** and are scored from the PNG.

## Scoring Scale

| Score | Meaning |
|---|---|
| 0 | Missing or fundamentally wrong |
| 1 | Present but weak or inconsistent |
| 2 | Strong, professional quality |

## Structural dimensions — score from the structural read

### 1. Connectivity (0–2)
Every node that should be joined is joined, in the `connectors` array.
- **0**: No connectors, or fewer than half the nodes participate in any connection
- **1**: Most nodes connected, but orphans remain or a connector has a null endpoint
- **2**: Every node participates; `startShapeName` and `endShapeName` are populated throughout

### 2. Completeness (0–2)
The diagram terminates and nothing dangles.
- **0**: Paths run off into nothing; no start or no end
- **1**: Terminates, but a branch is unresolved or a decision has a single exit
- **2**: Every path reaches a terminal; each decision has all its branches

### 3. Notation Correctness (0–2)
Shapes carry meaning, and the meaning is right.
- **0**: Everything is a drawn rectangle — a drawn diamond is not a `Decision`, and nothing
  downstream treats it as one
- **1**: Stencil masters used, but the wrong ones (a `Process` where a `Decision` belongs)
- **2**: Correct masters throughout, drawn from the archetype's stencil

Count nodes using `shapeType`, which distinguishes `Shape` from `Connector`. Counting raw shapes
counts the lines between them as nodes.

### 4. Labelling (0–2)
- **0**: Shapes have empty `text`
- **1**: Most labelled, but some blank or placeholder
- **2**: Every node labelled, and labels are specific rather than "Step 1"

## Visual dimensions — score from the PNG

### 5. Layout (0–2)
- **0**: Overlapping shapes, connectors crossing everywhere, no discernible flow direction
- **1**: Readable, but uneven spacing or avoidable crossings
- **2**: Consistent flow direction, aligned ranks, minimal crossings

### 6. Colour Discipline (0–2)
- **0**: Arbitrary colours with no scheme
- **1**: Mostly consistent, with off-palette exceptions
- **2**: One palette, applied so colour carries meaning rather than decoration

### 7. Page & Scale (0–2)
- **0**: Content overflows the page, or occupies a small fraction of it
- **1**: Fits, but with lopsided margins
- **2**: Fits the page with even margins; text legible at page scale

### 8. Visio Structure (0–2)
Does the drawing use Visio as Visio, rather than as a drawing canvas?
- **0**: One flat page of loose shapes
- **1**: Some structure — a named page, or shape data on a few shapes
- **2**: Named pages; layers, background pages or shape data used where they earn their place

### 9. Archetype Fit (0–2)
- **0**: Wrong archetype for the request (a block diagram where a process was asked for)
- **1**: Defensible, but a better archetype was available
- **2**: The archetype matches the request, and its variant fits too

### 10. Professionalism (0–2)
- **0**: Would not be shown outside the team
- **1**: Acceptable internally
- **2**: Would go into a design review or a customer-facing document unchanged

## Interpreting Scores

| Total Score | Quality Level | Action |
|---|---|---|
| 0–8 | Poor | Fundamental gaps; the guidance is not landing |
| 9–12 | Acceptable | Specific areas need improvement |
| 13–16 | Good | Minor polish needed |
| 17–20 | Excellent | Ship-quality output |

A drawing scoring 0 or 1 on **dimension 1** cannot exceed 12 overall, whatever it looks like. An
unconnected diagram is not a diagram.

## Gap Categories

When scoring reveals a pattern, classify the gap and name where the fix belongs. Every location
below exists — `EvalCriteriaFixLocationTests` asserts it, because a rubric that points at a tool
action nobody implemented sends every improvement round to a dead end.

| Gap Type | Example | Fix Location |
|---|---|---|
| Missing guidance | Agent does not know to connect shapes | `skills/shared/diagram-design-principles.md` |
| Wrong archetype | Block diagram for a process request | `design(get-archetype)` |
| Unknown stencil | Invents a master that is not installed | `design(get-stencil-catalog)` |
| Missing technique | Does not use layers, background pages or shape data | `design(get-diagram-patterns)` |
| Poor colours | Arbitrary fills, no scheme | `design(get-palette)` |
| No self-review | Obvious defects not caught before finishing | `skills/shared/diagram-design-review.md` |
| Wrong workflow | Builds in the wrong order, or leaves the file open | `skills/shared/generation-pipeline.md` |
