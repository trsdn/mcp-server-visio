# Stencil Catalog

Stencils and masters **verified present** on a stock Visio install by opening each file and
enumerating its masters. Drop them with:

```
stencil(drop-master, stencil_path='BASFLO_M.VSSX', master_name='Process',
        x_position=..., y_position=...)
```

`stencil_path` accepts the bare file name — Visio resolves it against its own stencil folders. A
full path also works and is what to use for a custom stencil.

`stencil(list-masters, stencil_path='...')` enumerates any stencil, including ones not listed here.

## Installed

### `BASFLO_M.VSSX` — Basic Flowchart (15 masters)

`Process`, `Decision`, `Subprocess`, `Start/End`, `Document`, `Data`, `Database`,
`External Data`, `Custom 1`–`Custom 4`, `On-page reference`, `Off-page reference`,
`Dynamic connector`

The default choice for any process diagram.

### `BASIC_U.VSSX` — Basic Shapes (58 masters)

`Rectangle`, `Square`, `Circle`, `Ellipse`, `Right Triangle`, `Triangle`, `Pentagon`, `Hexagon`,
`Heptagon`, `Octagon`, `Decagon`, `Can`, `Parallelogram`, `Trapezoid`, `Diamond`, and further
shapes including stars, arrows and braces.

Use where no notation applies. Contains the novelty shapes — sun, heart, moon — which do not belong
in a business diagram.

### `ARROWS_M.VSSX` — Arrow Shapes (28 masters)

`Simple Arrow`, `Simple Double Arrow`, `Modern Arrow`, `Flexible Arrow`, `Bent Arrow`,
`U-Turn Arrow`, `Sharp Bent Arrow`, `Curved Right Arrow`, `Curved Left Arrow`, `Multi-Line`,
`Multi-Arrow`, `Striped Arrow`, `Notched Arrow`, `Block Arrow`, `Circular Arrow`

An arrow *master* is a shape used for emphasis. To join two shapes, use
`shape(connect-shapes)` instead — a connector reroutes and follows its endpoints; an arrow shape
does not.

### `BPMN_M.VSSX` — BPMN (21 masters)

`Task`, `Gateway`, `Start Event`, `Intermediate Event`, `End Event`, `Collapsed Sub-Process`,
`Expanded Sub-Process`, `Text Annotation`, `Sequence Flow`, `Association`, `Message Flow`,
`Message`, `Data Object`, `Data Store`, `Group`, `Pool / Lane`

### `ORGCH_M.VSSX` — Organisation Chart (14 masters)

`Executive`, `Manager`, `Position`, `Consultant`, `Vacancy`, `Assistant`, `Team frame`, `Staff`,
`Multiple shapes`, `Three positions`, `Title/Date`, `Title`, `Dynamic connector`,
`Dotted-line report`

### `PERIPH_M.VSSX` — Computers and Peripherals (29 masters)

`Server`, `Router`, `Switch`, `Firewall`, `Ethernet`, `Mainframe`, `Ring network`, `Comm-link`,
`Super computer`, `Printer`, `Plotter`, `Scanner`, `Copier`, `Fax`, `Multi-function device`,
`CRT projector`

### `NETLOC_M.VSSX` — Network Locations (8 masters)

`Cloud`, `City`, `Building`, `House`, `Government building`, `University building`, `Town`,
`Dynamic connector`

`Cloud` means "outside our control", not "cloud hosting".

### `DTLNET_M.VSSX` — Detailed Network (16 masters)

`Patch panel`, `PBX`, `Radio tower`, `Satellite`, `Satellite dish`, `Repeater`,
`Fiber optic transmitter`, `External hard drive`, `External media drive`, `Biometric reader`,
`Smartcard reader`, `A/B switchbox`, `Diagnostic device`, `Data`, `XML Web Service`

### `CALOUT_M.VSSX` — Callouts (38 masters)

`Annotation`, `Side box callout`, `Mid box callout`, `Side line callout`, `Side text callout`,
`Side elbow box`, `Mid elbow box`, `Bend callout`, `Centre text callout`, `Braces with text`

### `FAULT_M.VSSX` — Fault Tree (12 masters)

`AND gate`, `OR gate`, `Exclusive OR gate`, `Priority AND gate`, `Voting gate`, `Inhibit gate`,
`Basic event`, `Undeveloped event`, `Event`, `House event`, `Conditional event`, `Transfer symbol`

### `LEGEND_M.VSSX` — Legend (6 masters)

`Outer list`, `Field container`, `Inner list`, `CBV item`, `Icon item`, `Text item`

Use whenever colour or shape carries meaning that the diagram does not otherwise state.

## Not installed on a stock system

These are **absent** on a default install — opening them raises a `COMException`. Do not reference
them without checking, and give a fallback built from `BASIC_U.VSSX`:

`CROSFN_M.VSSX` (cross-functional lanes), `WORKFL_M.VSSX`, `BORDER_M.VSSX` (borders and titles),
`TIMEL_M.VSSX` (timeline), `CHEN_M.VSSX` (entity-relationship), `VALUE_M.VSSX` (value stream map),
`CONTAINER_U.VSSX`, `MIND_M.VSSX` (mind map), `SDLC_M.VSSX`

Check before relying on one:

```
stencil(list-masters, stencil_path='TIMEL_M.VSSX')
```

A failure here is far cheaper than discovering it after the page has been built.

## Choosing between a master and a drawn shape

Drop a master when the shape carries meaning — a `Decision`, a `Firewall`, an `AND gate`. The master
brings its notation, its connection points and its text behaviour.

Draw with `shape(add-shape)` only for backgrounds, lane bands and boundaries, where the rectangle is
scaffolding rather than content. Note that `add-shape` produces only a rectangle or, with
`auto_shape_type=9`, an ellipse — there is no auto-shape gallery in Visio.
