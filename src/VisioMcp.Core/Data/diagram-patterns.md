# Diagram Patterns

Techniques that apply across archetypes. Each is something Visio does well and a generated diagram
usually does not do at all.

## Layers, not deletion

A layer makes a category of content removable without rebuilding the diagram:

```
layer(create, layer_name='Annotations')
layer(add-shape, layer_name='Annotations', shape_name='Callout1')
layer(set-visible, layer_name='Annotations', visible=false)
layer(set-print,   layer_name='Annotations', print=false)
```

Worth a layer: annotations, alternative future-state shapes, per-audience detail, background
banding. Add a shape to its layer as it is created — a later sweep always misses one.

## Background pages for shared furniture

A title block or logo repeated on every page belongs on a background page, drawn once:

```
page(create, name='Frame')
page(set-background, page_index=<Frame>, is_background=true)
page(set-back-page,  page_index=1, back_page_name='Frame')
```

Marking a page as a background **moves it** — Visio orders background pages after normal ones — so
use the index `set-background` returns rather than the one passed in.

## Shape data instead of longer labels

A label the reader must scan is worse than a short label plus queryable data:

```
shape(set-property, shape_name='web-01', property_name='IPAddress', property_value='10.0.1.15')
shape(set-property, shape_name='web-01', property_name='Owner',     property_value='Platform')
```

Anything the diagram must *hold* but need not *show* — IP addresses, cost, owner, SLA. It survives
export to Excel and is searchable; a label is neither.

## Masters for anything repeated

Placing the same composite shape more than about three times means it should be a master:

```
master(create-from-shape, page_index=1, shape_name='ServiceBox', master_name='Service')
```

Every later instance then shares one definition, so restyling is one edit rather than twenty. Note
that promoting a shape leaves the original as an ordinary shape — it does not become an instance.

## Styles for anything formatted the same way

Where several shapes share formatting, a style makes it one change instead of many:

```
style(create, style_name='Deprecated', based_on='Normal')
style(set-formula, style_name='Deprecated', cell_name='LinePattern', formula='2')
style(apply, page_index=1, shape_name='LegacyAPI', style_name='Deprecated')
```

`Styles.Add` takes its flags in the order text, line, fill; the `style` tool handles that, but a
style only accepts writes for aspects it carries.

## Connect, then lay out

`shape(connect-shapes)` produces dynamic connectors that route around obstacles and follow their
endpoints. Connecting first and positioning afterwards is therefore fine, and often easier than
computing a layout in advance.

Page-level routing is a set of PageSheet cells:

```
cell(set-formula, sheet_target='page', cell_name='RouteStyle',   formula='1')   right-angle
cell(set-formula, sheet_target='page', cell_name='AvenueSizeX',  formula='0.5 in')
```

## Alignment is one call

```
shapealign(align,      page_index=1, shape_names='A,B,C', align_type=0)
shapealign(distribute, page_index=1, shape_names='A,B,C', distribute_type=0)
```

Nearly-aligned shapes read as a mistake rather than as a distinction. There is no reason to leave
them nearly aligned.

## Page size before layout

```
cell(set-formula, sheet_target='page', cell_name='PageWidth',  formula='11 in')
cell(set-formula, sheet_target='page', cell_name='PageHeight', formula='8.5 in')
```

Changing page size afterwards does not reflow anything already placed.

## A second page beats a crowded one

Pages cost nothing. Where a diagram approaches the point of needing explanation to be read, split
it and connect the parts with `On-page reference` / `Off-page reference`, or a hyperlink:

```
hyperlink(add, page_index=1, shape_name='Subprocess A', sub_address='Page-3')
```

## Verify by reading back

A diagram that was written is not necessarily a diagram that exists. `shape(list)` returns names,
text and geometry; `shape(list-connectors)` returns what is actually joined. Shapes present with no
connectors is the most common way generated output is wrong while looking right.
