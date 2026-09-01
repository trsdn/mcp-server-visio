---
applyTo: "src/VisioMcp.Core/Commands/**/*.cs,tests/**/*.cs"
---

# Visio COM Patterns - Quick Reference

> **Essential patterns for Visio COM automation via late binding**

## Core Principles

1. **Use Late Binding** - `dynamic` types with `Type.GetTypeFromProgID("Visio.Application")` or `Type.GetTypeFromProgID("Visio.InvisibleApp")`
2. **1-Based Indexing** - Visio collections (`Pages`, `Shapes`, `Documents`, stencil `Masters`) start at 1
3. **Exception Propagation** - Never wrap in try-catch; let `batch.Execute()` handle exceptions
4. **ShapeSheet First** - Use `shape.CellsU("CellName")` for geometry, formatting, and page/shape properties
5. **Inches for Drawing** - `DrawRectangle`, `DrawLine`, and `Drop` coordinates are in Visio internal units (inches)

---

## Decision Tree: Which Tool to Use

```
Working with Visio documents?
├─ File lifecycle (create, open, save, close, list)?
│  └─ Use file tool
│
├─ Pages (create, read, rename, delete, guides, layout/routing)?
│  └─ Use page tool
│
├─ Shapes on a page (draw, drop, modify, delete, connect)?
│  └─ Use shape / shapealign tool
│
├─ ShapeSheet cells (geometry, formulas, colors, line weight)?
│  └─ Use cell tool
│
├─ Text inside a shape?
│  └─ Use text tool or assign shape.Text
│
├─ Layers and assignment?
│  └─ Use layer tool
│
├─ Stencil masters?
│  └─ Use stencil tool
│
└─ Document metadata, export, and windows?
   └─ Use docproperty / export / window tools
```

---

## Page Operations

### Adding Pages

```csharp
return batch.Execute((ctx, ct) =>
{
    dynamic document = ctx.Document;
    dynamic pages = document.Pages;
    dynamic? page = null;
    try
    {
        page = pages.Add();
        page.Name = name;
        return new OperationResult { Success = true, Action = "create" };
    }
    finally
    {
        if (page != null) ComUtilities.Release(ref page!);
        ComUtilities.Release(ref pages!);
    }
});
```

### Navigating Pages

```csharp
// By index (1-based)
dynamic page = ((dynamic)ctx.Document).Pages.Item(pageIndex);

// Iterate all pages
dynamic pages = ((dynamic)ctx.Document).Pages;
try
{
    int count = Convert.ToInt32(pages.Count);
    for (int i = 1; i <= count; i++)
    {
        dynamic page = pages.Item(i);
        try
        {
            // ... process page ...
        }
        finally
        {
            ComUtilities.Release(ref page!);
        }
    }
}
finally
{
    ComUtilities.Release(ref pages!);
}
```

### Deleting Pages

```csharp
// Delete the page and let Visio reindex subsequent pages
page.Delete(0);
```

---

## Shape Lifecycle

### Creating Shapes

```csharp
// Basic rectangle (x1, y1, x2, y2 in inches)
dynamic rectangle = page.DrawRectangle(1.0, 1.0, 3.0, 2.0);
rectangle.Name = "Process";
rectangle.Text = "Process step";

// Line (x1, y1, x2, y2 in inches)
dynamic connector = page.DrawLine(1.0, 1.0, 3.0, 1.0);

// Drop a stencil master
dynamic master = stencilDocument.Masters.ItemU("Process");
dynamic dropped = page.Drop(master, 2.0, 2.0);
```

### Modifying Shapes with ShapeSheet Cells

```csharp
// Position and size
shape.CellsU("PinX").FormulaU = "2 in";
shape.CellsU("PinY").FormulaU = "1.5 in";
shape.CellsU("Width").FormulaU = "2 in";
shape.CellsU("Height").FormulaU = "1 in";

// Rotation (Angle cell uses formulas; radians are also possible)
shape.CellsU("Angle").FormulaU = "45 deg";

// Formatting
shape.CellsU("FillForegnd").FormulaU = "RGB(255,0,0)";
shape.CellsU("LineWeight").FormulaU = "2 pt";

// Name and text
shape.Name = "Decision";
shape.Text = "Approve?";
```

### Reading ShapeSheet Values

```csharp
dynamic widthCell = shape.CellsU("Width");
try
{
    double widthInches = Convert.ToDouble(widthCell.ResultIU);
    string widthFormula = widthCell.FormulaU?.ToString() ?? string.Empty;
}
finally
{
    ComUtilities.Release(ref widthCell!);
}
```

### Deleting Shapes

```csharp
shape.Delete();
ComUtilities.Release(ref shape!);
```

---

## Text Manipulation

### Shape Text

```csharp
// Read text
string text = shape.Text?.ToString() ?? string.Empty;

// Write text
shape.Text = "New label";
```

### Text-Related ShapeSheet Cells

```csharp
shape.CellsU("TxtWidth").FormulaU = "Width";
shape.CellsU("TxtHeight").FormulaU = "Height";
shape.CellsU("TxtPinX").FormulaU = "Width*0.5";
shape.CellsU("TxtPinY").FormulaU = "Height*0.5";
```

Visio has no PowerPoint-style `TextFrame`, `TextRange`, or `HasTextFrame`. Use `shape.Text` for content and ShapeSheet cells for text block geometry and formatting.

---

## Connectivity

```csharp
// Outgoing connects from a shape
dynamic connects = shape.Connects;
try
{
    int count = Convert.ToInt32(connects.Count);
    for (int i = 1; i <= count; i++)
    {
        dynamic connect = connects.Item(i);
        try
        {
            // ... inspect connection ...
        }
        finally
        {
            ComUtilities.Release(ref connect!);
        }
    }
}
finally
{
    ComUtilities.Release(ref connects!);
}

// Incoming connects are available via shape.FromConnects
```

---

## COM Object Cleanup (CRITICAL)

### Standard Pattern

```csharp
dynamic? page = null;
dynamic? shape = null;
dynamic? cell = null;
try
{
    page = ((dynamic)ctx.Document).Pages.Item(1);
    shape = page.Shapes.Item(1);
    cell = shape.CellsU("Width");
    // ... operations ...
}
finally
{
    // Release in reverse acquisition order
    if (cell != null) ComUtilities.Release(ref cell!);
    if (shape != null) ComUtilities.Release(ref shape!);
    if (page != null) ComUtilities.Release(ref page!);
}
```

### Loop Cleanup

```csharp
dynamic shapes = page.Shapes;
try
{
    int count = Convert.ToInt32(shapes.Count);
    for (int i = 1; i <= count; i++)
    {
        dynamic? shape = null;
        try
        {
            shape = shapes.Item(i);
            // ... process shape ...
        }
        finally
        {
            if (shape != null) ComUtilities.Release(ref shape!);
        }
    }
}
finally
{
    ComUtilities.Release(ref shapes!);
}
```

### Reverse-Order Deletion in Loops

```csharp
// When deleting shapes, iterate in reverse to avoid index shifting
dynamic shapes = page.Shapes;
try
{
    for (int i = Convert.ToInt32(shapes.Count); i >= 1; i--)
    {
        dynamic? shape = null;
        try
        {
            shape = shapes.Item(i);
            if (ShouldDelete(shape))
            {
                shape.Delete();
            }
        }
        finally
        {
            if (shape != null) ComUtilities.Release(ref shape!);
        }
    }
}
finally
{
    ComUtilities.Release(ref shapes!);
}
```

---

## Common Visio COM Quirks

| Quirk | Detail |
|-------|--------|
| **1-based indexing** | Collections use `Pages.Item(1)`, `Shapes.Item(1)`, `Masters.Item(1)` |
| **ShapeSheet cells** | Geometry and formatting live in cells such as `PinX`, `PinY`, `Width`, `Height`, `Angle`, `FillForegnd` |
| **Internal units** | `ResultIU` returns internal units; drawing coordinates are inches |
| **Formula writes** | Use `FormulaU` for locale-independent formulas such as `"2 in"` or `"RGB(255,0,0)"` |
| **double returns** | Numeric properties often return `double`; use `Convert.ToInt32()` / `Convert.ToDouble()` |
| **Text content** | Use `shape.Text`; no `TextFrame` or `TextRange` |
| **Delete reindexes** | Deleting shapes/pages shifts subsequent indices; iterate in reverse when deleting many shapes |
| **Connectivity** | Use `shape.Connects` and `shape.FromConnects` for links between shapes |

---

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| 0-based indexing | Visio is 1-based |
| PowerPoint-style text APIs | Use `shape.Text` and ShapeSheet cells |
| Pixel measurements | Use Visio internal units/inches and ShapeSheet formulas |
| Assigning numeric formulas as raw doubles when units matter | Use `FormulaU` with units |
| `int x = shape.Property` | Use `Convert.ToInt32()` or `Convert.ToDouble()` for numeric COM values |
| Missing COM cleanup | Use `try-finally` + `ComUtilities.Release()` |
| Catching exceptions in Core | Let `batch.Execute()` handle via TaskCompletionSource |
