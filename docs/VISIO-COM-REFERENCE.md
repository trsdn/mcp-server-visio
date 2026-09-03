# VISIO COM REFERENCE

## Purpose

This document captures the Visio COM automation surface that matters most for building `mcp-server-visio`.

It is not a line-by-line dump of the full Visio object model. Instead, it is a practical reference for the parts of the model that are most relevant to a .NET automation server:

- application and document lifecycle
- pages, shapes, masters, and selections
- ShapeSheet access through cells and formulas
- connectivity, layers, styles, and data linking
- implementation implications for a future MCP and CLI surface

For the complete API surface, the canonical reference is the official Visio VBA object model on Microsoft Learn.

## Official automation surfaces

Visio exposes automation in three closely related forms:

1. COM Automation
   - External programs can create or attach to a Visio instance and drive it through COM.
   - Typical entry points are `CreateObject("Visio.Application")`, `GetObject`, or the `InvisibleApp` object described in the Application documentation.

2. VBA object model
   - Microsoft documents the Visio automation model primarily through the Visio VBA reference.
   - Even if we build in C#, this is the main API reference to read.

3. .NET Primary Interop Assembly (PIA)
   - The PIA lives in the `Microsoft.Office.Interop.Visio` namespace.
   - Microsoft explicitly states that the VBA object model corresponds to the types exposed by the Visio PIA.
   - Microsoft also notes that there is no separate, full reference dedicated only to the PIA; the VBA docs are the authoritative map.

In practice, that means:

- the VBA docs tell us what objects exist
- the PIA tells us what strongly typed .NET wrappers exist
- late-bound COM can still be a good fit if we want to keep the existing VisioMcp architecture style

## Platform and runtime assumptions

Visio automation has the same high-level constraints as Office COM automation in general:

- Windows only
- a desktop Visio installation is required
- an interactive desktop session is strongly preferred
- automation controls the real Visio process, not just `.vsdx` files on disk

This is important for architecture decisions. A future Visio MCP server should assume "live application automation" rather than "pure file transformation."

## Mental model: Visio is a document plus ShapeSheet platform

Visio is slide-centric. Visio is much more shape-centric and formula-centric.

Two ideas dominate the model:

1. Everything starts at the `Application` object and flows down through documents, pages, and shapes.
2. Almost everything meaningful in Visio is ultimately backed by a ShapeSheet cell or formula.

That second point is the biggest conceptual shift for this repo. In Visio, automation is not only about creating and moving shapes. It is also about reading and writing the formulas that define behavior, layout, data, and connectivity.

## High-level object hierarchy

The automation model broadly looks like this:

```text
Application
|- Documents
|  |- Document (drawing, stencil, or template)
|     |- Pages
|     |  |- Page
|     |     |- Shapes
|     |     |  |- Shape
|     |     |     |- Shapes (group contents)
|     |     |     |- Cells / Connects / text / geometry
|     |     |- Layers
|     |     |- Connects
|     |- Masters
|     |  |- Master
|     |- Styles
|     |  |- Style
|     |- DataRecordsets
|        |- DataRecordset
|- Windows
|  |- Window
|     |- Selection
```

That tree is the core automation path for a Visio server.

## Core automation objects at a glance

| Object | What it represents | Typical access path | Why it matters for MCP |
| --- | --- | --- | --- |
| `Application` | Running Visio instance | COM root object | Session lifecycle, active document, windows, events, undo scopes |
| `Documents` / `Document` | Open drawing, stencil, or template files | `Application.Documents`, `Application.ActiveDocument` | Open, create, save, export, print, enumerate pages and masters |
| `Pages` / `Page` | Drawing pages | `Document.Pages` | Draw, drop masters, enumerate page shapes, handle background pages |
| `Shapes` / `Shape` | Diagram elements | `Page.Shapes`, `Shape.Shapes` | The main work unit: geometry, text, data, connectors, groups |
| `Masters` / `Master` | Reusable shape definitions in stencils | `Document.Masters` | Drop canonical shapes onto pages by name or master |
| `Windows` / `Window` | UI windows onto documents and pages | `Application.Windows`, `Application.ActiveWindow` | Selection, view state, zoom, active page context |
| `Selection` | Batch of selected shapes | `Window.Selection` | Align, distribute, group, connect, move, resize, export |
| `Cell` | A ShapeSheet formula cell | `Shape.Cells(...)`, `Shape.CellsSRC(...)`, `Style.Cells(...)` | The foundation for fine-grained automation |
| `Connect` / `Connects` | Relationship between connected shapes | `Shape.Connects`, `Shape.FromConnects`, `Page.Connects` | Read the graph structure of a diagram |
| `Layer` / `Layers` | Per-page or per-master layer definitions | `Page.Layers`, `Master.Layers` | Visibility, printability, locking, shape grouping semantics |
| `Style` / `Styles` | Formatting inheritance definitions | `Document.Styles` | Reusable fill, line, and text formatting |
| `DataRecordset` / `DataRecordsets` | External data linked into a document | `Document.DataRecordsets` | Data-driven diagrams, refresh, shape-data mapping |

## The root object: `Application`

`Application` is the root of the entire model. Microsoft describes it as the object that represents a Visio instance and from which other objects are retrieved.

Most server-level responsibilities start here:

- create or attach to a running Visio instance
- get `Documents`, `Windows`, and the active objects
- manage application-wide behavior
- start and end undo scopes
- quit the application cleanly

Important members called out by Microsoft:

- `Documents`
- `Windows`
- `Addons`
- `ActiveDocument`
- `ActivePage`
- `ActiveWindow`
- `BeginUndoScope`
- `EndUndoScope`
- `Quit`

Important implementation note:

- Microsoft documents both `CreateObject("Visio.Application")` and `InvisibleApp`
- if this repo wants "headless until asked to show the window" behavior similar to Visio, `InvisibleApp` is worth evaluating early

Important event note:

- `Application` has a very large event surface, including document, page, shape, selection, formula, and window change events
- that is useful for future advanced scenarios, but a first MCP server does not need to expose events directly

## `Document`: drawings, stencils, and templates are all documents

Visio's `Document` object is more general than a Visio presentation. Microsoft states that a `Document` can represent:

- a drawing
- a stencil
- a template

This is a major architectural difference from Visio and should directly influence the server design.

Why it matters:

- opening a stencil is not a side channel; it is normal document automation
- masters are retrieved from documents
- templates are also documents
- a single server session may need to juggle a drawing document and one or more stencil documents

Document surfaces worth designing around:

- `Pages`
- `Masters`
- `Styles`
- `DataRecordsets`
- `DocumentSheet`
- `Save`, `SaveAs`, `Close`, `ExportAsFixedFormat`, `PrintOut`

Practical implication:

- a future `file` tool for Visio should probably understand document kinds, not just "open one drawing"
- stencil management is likely a first-class feature, not an implementation detail

## `Page`: the drawing surface

`Page` represents a drawing area. Microsoft highlights two especially important aspects:

- pages can be foreground or background pages
- shapes can be created directly on a page or dropped from masters onto a page

Key page-level creation paths from Microsoft documentation:

- `DrawSpline`
- `DrawOval`
- `Drop`
- `DropMany`

Important model characteristics:

- a page owns a `Shapes` collection
- a page also has `Connects`, which is useful for reading a full page-level connection graph
- background pages are a real concept and not just formatting metadata

Practical implication for an MCP server:

- page creation and page inspection should be separate from shape operations
- page APIs should include page kind, size, and background relationships
- dropping from a master will often be more useful than drawing primitives directly

## `Shape`: the real center of gravity

Microsoft describes `Shape` very broadly: anything you can select in a drawing window.

That includes:

- basic shapes
- group shapes
- guides
- embedded or linked objects from other applications

This is a critical design point: in Visio, even many things that feel "special" are still just shapes plus ShapeSheet data.

Two especially important consequences:

1. Connectors are shapes.
   - A line or connector is itself a `Shape`.
   - The topological relationship between shapes is represented separately through `Connect` objects.

2. Groups are shapes that contain shapes.
   - A group is still a `Shape`.
   - Nested shapes matter and should not be treated as a rare edge case.

Microsoft also notes:

- `Page.PageSheet` returns a `Shape` whose type is `visTypePage`
- `Master.PageSheet` returns a `Shape` whose type is `visTypePage`
- `Document.DocumentSheet` returns a `Shape` whose type is `visTypeDoc`

That means page-level and document-level properties often still flow through shape-like ShapeSheet access.

Practical implication:

- the future server should treat "shape-like sheet access" as a shared abstraction
- shape, page sheet, master sheet, and document sheet all want the same low-level cell tooling

## `Master`: reusable shape definitions

`Master` is the reusable template for shapes, typically stored in stencils.

This object family matters because Visio users do not only draw freehand. They usually work by dropping masters from stencils into a drawing.

Why `Master` matters:

- it is the primary factory for domain-specific shapes
- it enables predictable creation by name rather than coordinate-only drawing
- it is the bridge between stencils and page content

Server design implication:

- list stencil documents
- list masters within a stencil
- drop a master onto a target page
- expose names in a stable way for AI clients

If this repo only supports primitive drawing at first, it will miss much of what makes Visio automation valuable.

## `Window` and `Selection`: UI-aware automation surfaces

`Window` represents an open Visio window. It is the bridge between the underlying model and the current interactive editing context.

Key `Window` roles from Microsoft docs:

- identify the current document and page shown in the window
- retrieve the current `Selection`
- manage view state such as zoom and visible helpers

The most important detail in the official docs is the division of responsibility between `Window` and `Selection`.

Microsoft explicitly marks many old `Window` methods as obsolete for new or rebuilt solutions and says to use `Selection` methods instead for operations such as:

- group and ungroup
- combine, union, intersect, fragment, subtract, trim
- copy, cut, delete, duplicate

That is very useful guidance for a new server:

- view state belongs on `Window`
- batch editing belongs on `Selection`

`Selection` is effectively Visio's batch-shape operation object. Its method set is rich and directly useful for MCP operations:

- `Align`
- `Distribute`
- `BringToFront`
- `SendToBack`
- `Group`
- `Ungroup`
- `Move`
- `Resize`
- `Rotate`
- `ConnectShapes`
- `Layout`
- `Export`

Practical implication:

- many "multi-shape" operations should be designed around transient selections
- a Visio server may want explicit `selection` commands instead of overloading every action onto a `shape` tool

## ShapeSheet: the most important Visio-specific concept

The ShapeSheet is the spreadsheet-like formula engine behind Visio.

Microsoft's ShapeSheet overview says that every document, page, style, shape, group, guide, and even objects within groups have a ShapeSheet where behavior and appearance are stored.

This is arguably the most important single concept for `mcp-server-visio`.

### ShapeSheet structure

The ShapeSheet is organized as:

- sections
- rows
- cells

A cell contains:

- a formula
- a result or value
- optional error information

Cells can be:

- locally defined
- inherited from a master or style

That inheritance model is a big difference from Visio. In Visio, formatting and behavior are deeply formula-driven and often inherited rather than directly assigned.

### Why ShapeSheet matters to the server

A serious Visio server will almost certainly need first-class support for:

- reading a cell result
- reading a cell formula
- setting `Formula` or `FormulaU`
- reading numeric results via `ResultIU`
- reading or writing user-defined cells
- inspecting geometry and connection points
- resolving references across shapes, pages, styles, masters, and the document

In other words:

- a simple "shape formatting" API is not enough
- a ShapeSheet or cell API is probably unavoidable

## `Cell`: the low-level automation primitive

Microsoft defines `Cell` as a formula that evaluates to some value.

Important capabilities from the official docs:

- get and set formulas
- get results in internal units with `ResultIU`
- get and set locale-independent formulas with `FormulaU`
- glue one shape to another with `GlueTo` and `GlueToPos`
- inspect precedents and dependents

The `Cell` object is how the future server gains precise control over:

- geometry
- position
- sizing
- connector endpoints
- shape data
- custom behavior
- data-driven formulas

### Recommended automation habit: prefer universal names and formulas

The official object model exposes locale-neutral members such as:

- `FormulaU`
- `ResultStrU`
- `NameU`
- `RowNameU`

That strongly suggests a best practice for automation code:

- prefer universal names and formulas for persisted or programmatic operations
- avoid locale-sensitive names where possible

This will matter if the server needs to work across non-English Visio installations.

## Cell reference syntax is a design input, not a low-level detail

Microsoft's ShapeSheet reference on cell references is extremely important because it tells us how Visio users think about addressing data.

Examples from the official docs include:

- same shape: `Width`
- peer shape: `Star!Angle`
- any object by ID: `Sheet.8!FillForegnd`
- page: `ThePage!User.Vanishing_Point`
- other page: `Pages[Page-3]!Sheet.4!BeginX`
- style: `Styles!Manager!LineColor`
- document: `TheDoc!PreviewQuality`

This matters for server design because it suggests two useful access modes:

1. Friendly access
   - pass shape name plus cell name
   - good for conversational workflows

2. Exact ShapeSheet access
   - accept raw ShapeSheet references
   - good for advanced automation and power users

If we skip the second mode entirely, we may block a lot of advanced Visio value.

## ShapeSheet sections that deserve early support

From the official ShapeSheet overview and related references, the following sections are especially important for a first implementation:

### Shape Transform

Core size and placement data such as:

- width
- height
- pin position
- angle

### Geometry

Defines the actual path and vertices of a shape.

This is essential for:

- custom geometry
- path inspection
- advanced freeform generation

### Connection Points

Defines attachable connection points.

This is essential for:

- connector routing
- attaching links to specific anchor points
- diagram semantics beyond simple nearest-point glue

### User-defined cells

Used for custom metadata and formulas attached to a shape.

This is a very promising surface for AI workflows because it gives us a structured place to store:

- custom tags
- calculated state
- semantic identifiers
- server-owned metadata

### DocumentSheet and PageSheet

These act like shape-backed sheets for document and page-level settings.

That means a cell-centric API can cover more than ordinary shapes if we design it well.

## Connectivity model: shapes plus `Connect`

Visio uses two related concepts for connectivity:

1. Connector shapes
   - actual 1-D shapes that appear on the page

2. `Connect` objects
   - the explicit relationship between source and target sheets and cells

Microsoft says:

- `Connect` represents a connection between two shapes
- you retrieve it from `Connects` collections on shapes, pages, and masters
- you create the actual relationship through `Cell.GlueTo` or `GlueToPos`

This is excellent news for an MCP server because it means we can support both:

- visual creation of connector shapes
- semantic inspection of connection topology

Those should likely become separate commands.

## Layers and styles are first-class citizens

Visio is stronger than Visio in document-structure features such as layers and styles.

### `Layer`

Microsoft describes `Layer` as a layer of a page or master. Shapes can be assigned to or removed from it.

Important details:

- layer attributes are themselves cell-backed and available through `CellsC`
- `Index` and `Row` can differ
- layer rows live in the Layers section of the ShapeSheet

This suggests a future server should support:

- list layers
- create and delete layers
- assign and remove shapes
- toggle visibility, printability, and lock-like behaviors

### `Style`

Microsoft describes `Style` as a reusable style definition in a document.

Important details:

- shapes inherit fill, line, and text attributes from styles
- styles also expose `Cells` and `CellsSRC`

This makes styles another shape-like sheet surface, and a good target for reusable formatting commands.

## Data-linked diagrams: `DataRecordset`

`DataRecordset` is Visio's built-in data-linking surface.

Microsoft says it can store, format, refresh, and expose data queried from sources such as:

- Excel
- Access
- SQL Server
- SharePoint lists
- OLE DB and ODBC sources
- XML in the ADO classic XML schema

Important server implications:

- linked data is not an afterthought in Visio
- data refresh and conflict resolution are part of the model
- large linked datasets can materially affect file size and performance

Edition note from Microsoft:

- the official docs note this feature as available only to licensed users of Visio Professional 2013

For a modern Visio server, that is a reminder that some data features may depend on product edition or licensing and should be capability-checked rather than assumed.

## What is likely most important for `mcp-server-visio`

Not every object in the full Visio model needs to become a first-wave MCP tool.

The most promising first-wave surfaces are:

| Future server area | Likely COM objects |
| --- | --- |
| Session and files | `Application`, `Documents`, `Document`, `Windows` |
| Pages | `Document.Pages`, `Page`, `PageSheet` |
| Shapes | `Page.Shapes`, `Shape`, nested `Shape.Shapes` |
| Masters and stencils | `Document.Masters`, `Master`, `Page.Drop`, `Page.DropMany` |
| Selections and multi-shape layout | `Window.Selection`, `Selection` |
| ShapeSheet and cells | `Cell`, `Shape.Cells`, `Shape.CellsSRC`, `DocumentSheet`, `PageSheet` |
| Connectivity | `Connect`, `Connects`, `Cell.GlueTo`, connector shapes |
| Layers and styles | `Layer`, `Style` |
| Data linkage | `DataRecordset` |
| Window and view control | `Window` |

If we had to prioritize only a handful of implementation pillars, they would be:

1. document and page lifecycle
2. shape creation and inspection
3. master and stencil drop workflows
4. a strong cell and ShapeSheet API
5. connectivity and multi-shape operations

## Practical interop guidance for this repo

Because this repo started as a Visio automation stack, the following migration notes matter:

### 1. Keep late binding as a valid option

The current VisioMcp codebase leans heavily on late-bound COM with `dynamic`.

That style can still work for Visio because:

- the object model is COM-first
- the VBA docs are the primary documentation anyway
- it keeps dependency and versioning friction low

The PIA remains useful as a reference even if runtime code stays late-bound.

### 2. Treat ShapeSheet as a first-class API surface

For Visio, a generic "shape formatting" abstraction can go a long way.

For Visio, that will not be enough. The server should be designed with a deliberate ShapeSheet story from day one.

### 3. Model stencils and masters early

In Visio, dropping masters from stencils is often more important than drawing primitives.

That should influence the first public tool design.

### 4. Separate visual connectors from graph relationships

Connector shapes and `Connect` relationships are related but not identical.

Both deserve explicit representation in the server model.

### 5. Prefer locale-neutral APIs where available

Use members such as `FormulaU` and `NameU` when possible so that automation stays predictable across localized Visio installations.

## Minimal example: creating Visio through COM from .NET

```csharp
Type visioType = Type.GetTypeFromProgID("Visio.Application")
    ?? throw new InvalidOperationException("Visio is not installed.");

dynamic app = Activator.CreateInstance(visioType)
    ?? throw new InvalidOperationException("Could not start Visio.");

app.Visible = true;

dynamic doc = app.Documents.Add("");
dynamic page = app.ActivePage;
dynamic shape = page.DrawRectangle(1, 1, 4, 3);

shape.Cells("Width").FormulaU = "2 in";
double widthInInternalUnits = shape.Cells("Width").ResultIU;
```

This short example already touches the key model:

- `Application`
- `Document`
- `Page`
- `Shape`
- `Cell`

That is the core of most real Visio automation.

## Recommended official reading list

These are the most useful official references gathered for the Visio migration:

- [Visio object model overview (VSTO)](https://learn.microsoft.com/en-us/visualstudio/vsto/visio-object-model-overview?view=vs-2022)
- [Visio object model for VBA](https://learn.microsoft.com/en-us/office/vba/api/overview/visio/object-model)
- [About Automation (Visio)](https://learn.microsoft.com/en-us/office/vba/visio/concepts/about-automation-visio)
- [Application object](https://learn.microsoft.com/en-us/office/vba/api/visio.application)
- [Document object](https://learn.microsoft.com/en-us/office/vba/api/visio.document)
- [Shape object](https://learn.microsoft.com/en-us/office/vba/api/visio.shape)
- [Selection object](https://learn.microsoft.com/en-us/office/vba/api/visio.selection)
- [Window object](https://learn.microsoft.com/en-us/office/vba/api/visio.window)
- [Cell object](https://learn.microsoft.com/en-us/office/vba/api/visio.cell)
- [Connect object](https://learn.microsoft.com/en-us/office/vba/api/visio.connect)
- [Layer object](https://learn.microsoft.com/en-us/office/vba/api/visio.layer)
- [Style object](https://learn.microsoft.com/en-us/office/vba/api/visio.style)
- [DataRecordset object](https://learn.microsoft.com/en-us/office/vba/api/visio.datarecordset)
- [About the ShapeSheet Spreadsheet](https://learn.microsoft.com/en-us/office/client-developer/visio/about-the-shapesheet-spreadsheet)
- [About Cell References](https://learn.microsoft.com/en-us/office/client-developer/visio/about-cell-references)
- [Visio ShapeSheet Reference](https://learn.microsoft.com/en-us/office/client-developer/visio/visio-shapesheet-reference)

## Bottom line

The Visio COM model is deeper than the Visio model in one specific way: the ShapeSheet is not a side detail, it is the platform.

If `mcp-server-visio` is meant to be more than a thin wrapper around a few drawing commands, it should be designed around:

- documents and stencils
- pages and shapes
- masters
- selections
- connectivity
- ShapeSheet cells and formulas

That should be the reference point for every next architectural step in this repo.
