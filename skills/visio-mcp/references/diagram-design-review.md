# Diagram Design Self-Review

Run these checks before you close a Visio document. They are ordered so the cheap, mechanical
checks come first and the judgement calls come last.

## Quick Check (Every Page)

| # | Check | Pass condition | Fix |
|---|---|---|---|
| 1 | One purpose? | The page shows one process, one system view, one hierarchy, or one comparison | Split into separate pages |
| 2 | Readable labels? | Every shape's text fits inside it without clipping | Resize the shape, or shorten the label |
| 3 | No accidental overlap? | Shapes touch only where the diagram means them to | Move or re-space; check `PinX`/`PinY` |
| 4 | Connectors attached? | Connectors are glued to shapes, not merely drawn near them | Reconnect; a dragged shape reveals unglued ends |
| 5 | Consistent spacing? | Sibling shapes share a spacing value | Align and distribute with `shapealign` |
| 6 | Flow direction? | One dominant direction — left-to-right or top-to-bottom | Re-route the outliers |
| 7 | Crossings minimised? | No connector crosses another where a simple re-route avoids it | Reorder shapes, or use a different connector type |
| 8 | Named pages? | Page names describe content, not `Page-1`, `Page-2` | Rename |
| 9 | Named shapes? | Shapes you will reference later have meaningful names | Set the shape name at creation |
| 10 | Text verified? | You read text back after setting it | `text(get)` — confirms it landed on the shape you meant |

Check 10 earns its place: setting text on the wrong shape produces a diagram that looks plausible
and is wrong, and it is the single most common silent failure in generated diagrams.

## Document-Level Check

| # | Check | Pass condition | Fix |
|---|---|---|---|
| 11 | Consistent visual language? | The same concept looks the same on every page | Reuse stencil masters instead of redrawing |
| 12 | Page order? | Pages read in a sensible sequence | Reorder |
| 13 | Scale coherent? | Related pages share a drawing scale | Check `DrawingScale` / `PageScale` |
| 14 | Layers used deliberately? | If layers exist, each has a purpose and a name | Merge or delete incidental layers |
| 15 | No orphans? | No stray shapes outside the page area or hidden behind others | List shapes and compare against intent |

## Common Failures and Fixes

### Failure: Connectors that look attached but are not

**Symptom:** The diagram renders correctly, but moving a shape leaves connectors behind.

**Cause:** The connector was drawn between two points rather than glued to the shapes.

**Fix:** Connect shape-to-shape so Visio creates the glue. Verify by reading the connector's
`BeginX`/`EndX` cells — a glued end shows a formula referencing the target shape, an unglued end
shows a literal coordinate. This is the check that distinguishes a real diagram from a picture of
one.

### Failure: Labels clipped inside shapes

**Symptom:** Text is set successfully but displays truncated.

**Fix:** Size the shape to the text rather than the reverse. Widen before you shrink the font —
below about 8pt a diagram stops being readable at normal zoom.

### Failure: Everything is a rectangle

**Symptom:** A process, a decision and a data store are visually identical.

**Cause:** `shape(add-shape)` draws only rectangles and ellipses.

**Fix:** Drop stencil masters with `stencil(drop-master)`. Shape vocabulary is most of what makes a
diagram readable at a glance, and it is the part `add-shape` cannot provide.

### Failure: Grid drift

**Symptom:** Shapes are *almost* aligned, which reads as sloppier than obviously unaligned.

**Fix:** Align and distribute explicitly rather than positioning by arithmetic. Small floating-point
differences in computed positions are visible.

### Failure: Colour without meaning

**Symptom:** Shapes are coloured, but the colours encode nothing.

**Fix:** Either make colour carry information — status, ownership, layer — and say so in a legend,
or remove it. Decorative colour on a diagram costs attention and returns nothing.

### Failure: One enormous page

**Symptom:** A page that only reads when zoomed out past legibility.

**Fix:** Split by subsystem, and use a page-level overview that links to detail pages. Visio
navigates between pages well; use that instead of scaling down.

## Verify-Fix Loop

1. List pages; for each page list shapes.
2. Read back the text of every shape you set.
3. Check connector endpoints are glued, not coordinates.
4. Run the Quick Check table.
5. Fix the highest-impact single issue, then re-check — batching fixes hides which one worked.

## Quality Scorecard

Score each dimension 0–2 (0 = fails, 1 = acceptable, 2 = good). Below 9/14, revise before
delivering.

| Dimension | 0 | 1 | 2 |
|---|---|---|---|
| Purpose clarity | Page mixes several ideas | One idea, weakly expressed | One idea, immediately legible |
| Label quality | Clipped or generic | Readable | Short, specific, consistent |
| Connector integrity | Unglued or wrong targets | Glued, some clutter | Glued, clean routes |
| Alignment | Visibly irregular | Mostly aligned | Deliberate grid |
| Shape vocabulary | All identical | Some differentiation | Meaningful, from stencils |
| Whitespace | Crowded | Adequate | Comfortable, scannable |
| Naming | Defaults throughout | Pages named | Pages and key shapes named |

## Auto-Reject Triggers

Do not deliver a diagram with any of these:

- a connector whose endpoints are coordinates where they should be glued
- text set on the wrong shape
- labels clipped by their shape
- a page still called `Page-1` in delivered work
- shapes overlapping in a way that hides content
