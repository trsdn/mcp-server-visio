using System.Text.Json.Serialization;

namespace VisioMcp.Core.Models;

/// <summary>
/// Base result type for all Core operations.
/// Exceptions propagate naturally — batch.Execute() re-throws them via TaskCompletionSource.
/// </summary>
public abstract class ResultBase
{
    public bool Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilePath { get; set; }
}

/// <summary>
/// Result for operations that don't return data (create, delete, etc.)
/// </summary>
public class OperationResult : ResultBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Action { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}

/// <summary>
/// Result for rename operations
/// </summary>
public class RenameResult : ResultBase
{
    public string ObjectType { get; set; } = string.Empty;
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
}

// ── File / Session ────────────────────────────────────────

public class FileValidationInfo : ResultBase
{
    public bool Exists { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsMacroEnabled { get; set; }
    public int PageCount { get; set; }
}

// ── Page ──────────────────────────────────────────────────

public class PageListResult : ResultBase
{
    public List<PageInfo> Pages { get; set; } = [];
}

public class PageInfo
{
    public int PageIndex { get; set; }
    public string PageId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBackground { get; set; }
    public int ShapeCount { get; set; }
}

public class PageDetailResult : ResultBase
{
    public PageInfo? Page { get; set; }
    public List<ShapeInfo> Shapes { get; set; } = [];
}

public class PageGuideListResult : ResultBase
{
    public int PageIndex { get; set; }
    public string PageName { get; set; } = string.Empty;
    public List<PageGuideInfo> Guides { get; set; } = [];
}

public class PageGuideInfo
{
    public string Name { get; set; } = string.Empty;
    public int ShapeType { get; set; }
    public int GuideType { get; set; }
    public string GuideTypeName { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
}

public class PageRoutingSettingsResult : ResultBase
{
    public int PageIndex { get; set; }
    public string PageName { get; set; } = string.Empty;
    public int RouteStyle { get; set; }
    public int ConnectorRoutingExtension { get; set; }
    public int LineJumpCode { get; set; }
    public int LineJumpStyle { get; set; }
    public int WalkPreference { get; set; }
    public int PlaceStyle { get; set; }
    public float LineJumpFactorX { get; set; }
    public float LineJumpFactorY { get; set; }
    public float LineToLineX { get; set; }
    public float LineToLineY { get; set; }
    public float AvenueSizeX { get; set; }
    public float AvenueSizeY { get; set; }
    public float BlockSizeX { get; set; }
    public float BlockSizeY { get; set; }
    public int PageLineJumpDirX { get; set; }
    public int PageLineJumpDirY { get; set; }
}

// ── Layer ──────────────────────────────────────────────────

public class LayerListResult : ResultBase
{
    public int PageIndex { get; set; }
    public List<LayerInfo> Layers { get; set; } = [];
}

public class LayerInfo
{
    public int PageIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameU { get; set; } = string.Empty;
    public int ColorIndex { get; set; }
    public bool Visible { get; set; }
    public bool Printable { get; set; }
    public bool Locked { get; set; }
    public bool Snap { get; set; }
    public bool Glue { get; set; }
    public int MemberCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ShapeNames { get; set; }
}

public class LayerDetailResult : ResultBase
{
    public int PageIndex { get; set; }
    public LayerInfo? Layer { get; set; }
}

// ── Shape ─────────────────────────────────────────────────

public class ShapeListResult : ResultBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PageIndex { get; set; }

    public List<ShapeInfo> Shapes { get; set; } = [];
}

public class ShapeSelectionResult : ResultBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PageIndex { get; set; }

    public List<ShapeInfo> Shapes { get; set; } = [];
}

public class ShapeInfo
{
    public int ShapeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShapeType { get; set; } = string.Empty;
    public float Left { get; set; }
    public float Top { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int ZOrderPosition { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AlternativeText { get; set; }

    public bool HasTextFrame { get; set; }
    public bool HasTable { get; set; }
    public bool HasChart { get; set; }
    public bool IsGroup { get; set; }
    public bool IsPlaceholder { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PlaceholderType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ShapeInfo>? GroupItems { get; set; }
}

public class ShapeDetailResult : ResultBase
{
    public ShapeInfo? Shape { get; set; }
}

public class ConnectorInfo
{
    public int ShapeId { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StartShapeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndShapeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StartConnectionCell { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndConnectionCell { get; set; }
}

public class ConnectorListResult : ResultBase
{
    public int PageIndex { get; set; }
    public List<ConnectorInfo> Connectors { get; set; } = [];
}

public class ConnectorDetailResult : ResultBase
{
    public int PageIndex { get; set; }
    public ConnectorInfo? Connector { get; set; }
}

public class ShapeConnectionInfo
{
    public int ConnectorShapeId { get; set; }
    public string ConnectorName { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConnectorEnd { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConnectorConnectionCell { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ShapeConnectionCell { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConnectedShapeName { get; set; }
}

public class ShapeConnectionListResult : ResultBase
{
    public int PageIndex { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public List<ShapeConnectionInfo> Connections { get; set; } = [];
}

// ── Cell / ShapeSheet ───────────────────────────────────────

public class CellInfo
{
    public string CellName { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }
}

public class CellResult : ResultBase
{
    public int PageIndex { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public CellInfo? Cell { get; set; }
}

public class CellListResult : ResultBase
{
    public int PageIndex { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public List<CellInfo> Cells { get; set; } = [];
}

public class ShapePropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyValue { get; set; } = string.Empty;
}

public class ShapePropertyResult : ResultBase
{
    public int PageIndex { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public ShapePropertyInfo? Property { get; set; }
}

public class ShapePropertyListResult : ResultBase
{
    public int PageIndex { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public List<ShapePropertyInfo> Properties { get; set; } = [];
}

// ── Stencil / Master ───────────────────────────────────────

public class StencilMasterInfo
{
    public string Name { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NameU { get; set; }
}

public class StencilMasterListResult : ResultBase
{
    public string StencilPath { get; set; } = string.Empty;
    public List<StencilMasterInfo> Masters { get; set; } = [];
}

// ── Text ──────────────────────────────────────────────────

public class TextResult : ResultBase
{
    public int ShapeId { get; set; }
    public string ShapeName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<TextParagraphInfo> Paragraphs { get; set; } = [];
}

public class TextParagraphInfo
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Alignment { get; set; }

    public List<TextRunInfo> Runs { get; set; } = [];
}

public class TextRunInfo
{
    public string Text { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FontName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? FontSize { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Bold { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Italic { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
}

// ── Table (in shapes) ────────────────────────────────────

// ── Master / Layout ───────────────────────────────────────

// ── Notes ─────────────────────────────────────────────────

// ── Transition ────────────────────────────────────────────

// ── Animation ─────────────────────────────────────────────

// ── Export ─────────────────────────────────────────────────

public class ExportResult : ResultBase
{
    public string OutputPath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

// ── Window ────────────────────────────────────────────────

public class WindowInfoResult : ResultBase
{
    public int WindowState { get; set; }
    public string WindowStateName { get; set; } = string.Empty;
    public float Left { get; set; }
    public float Top { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public class WindowViewportResult : ResultBase
{
    public int PageIndex { get; set; }
    public string PageName { get; set; } = string.Empty;
    public int WindowType { get; set; }
    public string WindowTypeName { get; set; } = string.Empty;
    public int WindowSubType { get; set; }
    public string WindowSubTypeName { get; set; } = string.Empty;
    public int ViewFit { get; set; }
    public string ViewFitName { get; set; } = string.Empty;
    public float ZoomPercent { get; set; }
    public float Left { get; set; }
    public float Top { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float CenterX { get; set; }
    public float CenterY { get; set; }
}

public class WindowVisualAidsResult : ResultBase
{
    public int PageIndex { get; set; }
    public string PageName { get; set; } = string.Empty;
    public bool GridVisible { get; set; }
    public bool GuidesVisible { get; set; }
    public bool RulersVisible { get; set; }
    public bool DrawingAidsEnabled { get; set; }
}

public class WindowSnapSettingsResult : ResultBase
{
    public bool DrawingAidsEnabled { get; set; }
    public int GridSnapStrength { get; set; }
    public int GuidesSnapStrength { get; set; }
    public int PointsSnapStrength { get; set; }
    public int RulerSnapStrength { get; set; }
    public int GeometrySnapStrength { get; set; }
    public int ExtensionsSnapStrength { get; set; }
}

// ── Hyperlink ─────────────────────────────────────────────

// ── Section ───────────────────────────────────────────────

// ── Document Properties ───────────────────────────────────

public class DocumentPropertyResult : ResultBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subject { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Author { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Keywords { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Comments { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Company { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }
}

// ── Media ─────────────────────────────────────────────────

// ── Comment ──────────────────────────────────────────────

// ── Placeholder ──────────────────────────────────────────

// ── Background ───────────────────────────────────────────

// ── Header/Footer ────────────────────────────────────────

// ── SmartArt ─────────────────────────────────────────────

// ── Custom Show ──────────────────────────────────────────

// ── Page Setup ───────────────────────────────────────────

// ── Tags ─────────────────────────────────────────────────

// ── Color Scheme ─────────────────────────────────────────

// ── Accessibility ────────────────────────────────────────

// ── Design Catalog ───────────────────────────────────────

