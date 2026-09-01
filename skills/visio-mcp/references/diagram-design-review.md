# Diagram Self-Review

After building a Visio page, run this checklist before handing the result back.
Fix anything that fails; do not report success on a page you have not inspected.

## Quick Check (Every Page)

Read the page back through the tools rather than trusting your own writes:

- `page(list)` - the page exists and is named meaningfully
- `shape(list)` - the expected shapes are present and nothing is duplicated
- `text(get)` - labels landed on the shapes you intended
- `shape(list-connections)` - connectors attach to the right shapes on the right ends

If any read disagrees with what you thought you created, fix the page before continuing.

## Structure

- The page communicates one thing: one process, one system view, one hierarchy, or one comparison.
- Reading order is obvious - usually left to right, or top to bottom.
- Related shapes are grouped or aligned so the grouping is visible without explanation.
- Nothing important is pushed outside the printable page area.

## Layout

- Sibling shapes share a common edge or centerline. Check `PinX`/`PinY` through the `cell` tool if in doubt.
- Spacing between siblings is consistent; use `shapealign(distribute)` rather than eyeballing coordinates.
- Shapes do not unintentionally overlap.
- There is enough whitespace that the page can be scanned quickly.

## Connectors

- Every connector represents a real relationship, not decoration.
- Flow direction is consistent across the page.
- Connectors are attached to shapes (glued), not merely drawn near them. `shape(list-connections)`
  returns the attached shape names; an empty result means the connector is floating.
- Crossings are minimized, and unavoidable crossings do not obscure a label.

## Text

- Labels are short and specific; the most important word comes first.
- Shape size fits the label instead of truncating it.
- Naming is consistent across pages.
- Font and size are uniform for shapes at the same level. Verify through the `cell` tool
  (`Char.Font`, `Char.Size`) if the page mixes stencil masters with drawn shapes.

## Shapes

- Standard diagram elements come from stencil masters via the `stencil` tool where one exists.
- Similar concepts look similar; different concepts look different.
- Fill and line usage is meaningful, not arbitrary. Check `FillForegnd` and `LineColor`
  through the `cell` tool when a color is supposed to carry meaning.

## Before Closing

- Page names are meaningful.
- The document saves without error.
- The page can be understood without any additional narration from you.
- Report honestly what you could not verify - for example, anything requiring visual judgement
  that the tools cannot read back.
