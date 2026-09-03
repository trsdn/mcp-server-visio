# Annotated Diagram

**When:** An existing diagram needs commentary — a review markup, an explanation for a newcomer, or
the three places where the current architecture will break.

This archetype adds a layer; it does not replace one. The underlying diagram keeps its own
archetype, and the annotations must be removable without disturbing it.

**Stencil:** `CALOUT_M.VSSX` (installed by default — 38 masters)

## Masters

| Master | Use for |
|---|---|
| `Annotation` | A plain note pointing at a shape. The default choice |
| `Side box callout` | A boxed note beside its target |
| `Mid box callout` | A boxed note with the pointer from its middle |
| `Bend callout` | When the note must sit away from its target and the line has to turn |
| `Centre text callout` | Text centred over a region rather than pointing at one shape |
| `Braces with text` | Commentary spanning several shapes |

## Put annotations on their own layer

This is what makes them removable, and it is the whole reason the archetype exists:

```
layer(create, layer_name='Annotations')
layer(add-shape, layer_name='Annotations', shape_name='Callout1')
layer(set-visible, layer_name='Annotations', visible=false)     hide for a clean export
```

An annotation that is not on the layer will survive a "hide comments" request and confuse whoever
asked. Add every callout as it is created, not in a sweep afterwards where one will be missed.

Consider `layer(set-print, layer_name='Annotations', print=false)` so a printed copy is clean while
the on-screen version keeps the commentary.

## Attach, do not float

A callout must be glued to what it explains, or it stops pointing at anything as soon as the
diagram is rearranged:

```
shape(connect-shapes, shape_names='Callout1,Database')
```

A text box placed near a shape is not an annotation. It is a text box that happens to be near a
shape, and it will drift.

## Visual separation

Annotations must read as commentary rather than as content:

```
cell(set-formula, sheet_target='shape', shape_name='Callout1', cell_name='FillForegnd', formula='RGB(255,249,196)')
cell(set-formula, sheet_target='shape', shape_name='Callout1', cell_name='Char.Size',   formula='9 pt')
```

Smaller text, a distinct fill, and a colour the base diagram does not use. If the diagram already
uses amber for a status, pick something else — otherwise the annotation reads as a status.

## Anti-patterns

**Annotations not on a layer.** Then they cannot be hidden, and the archetype has achieved nothing.

**Floating text boxes.** They point at nothing and move independently.

**Annotating everything.** Three notes are read; fifteen are skipped. If most of the diagram needs
explanation, the diagram is wrong — fix it instead.

**Annotations in the base palette.** They then read as part of the system being described.

**Covering the diagram.** A callout placed over a shape hides the thing it explains. Use
`Bend callout` and put the note in the margin.

**Leaving review markup in a shipped diagram.** Hide the layer, or delete it, before export.
