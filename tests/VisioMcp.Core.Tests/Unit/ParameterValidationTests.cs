using VisioMcp.Core.Commands.DocumentProperty;
using VisioMcp.Core.Commands.Export;
using VisioMcp.Core.Commands.Page;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Commands.ShapeAlign;
using VisioMcp.Core.Commands.Text;
using VisioMcp.Core.Commands.Window;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Tests that Core Commands validate required parameters before executing.
/// These tests verify that ArgumentException/ArgumentNullException is thrown
/// for null/empty required parameters WITHOUT needing a Visio COM connection.
/// </summary>
public class ParameterValidationTests
{
    // ── Window Commands ──────────────────────────────────────

    [Theory]
    [InlineData(4)]
    [InlineData(1000)]
    public void WindowSetZoom_OutOfRange_ThrowsArgumentOutOfRangeException(int zoomPercent)
    {
        var commands = new WindowCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.SetZoom(null!, zoomPercent));
    }

    [Fact]
    public void WindowPanToShape_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new WindowCommands();
        Assert.Throws<ArgumentNullException>(() => commands.PanToShape(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WindowPanToShape_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new WindowCommands();
        Assert.Throws<ArgumentException>(() => commands.PanToShape(null!, 1, shapeName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void WindowSetGridSnapStrength_OutOfRange_ThrowsArgumentOutOfRangeException(int strength)
    {
        var commands = new WindowCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.SetGridSnapStrength(null!, strength));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void WindowSetGeometrySnapStrength_OutOfRange_ThrowsArgumentOutOfRangeException(int strength)
    {
        var commands = new WindowCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.SetGeometrySnapStrength(null!, strength));
    }

    // ── Page Commands ────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void PageAddGuide_InvalidGuideType_ThrowsArgumentOutOfRangeException(int guideType)
    {
        var commands = new PageCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.AddGuide(null!, 1, guideType, 72, 144));
    }

    [Fact]
    public void PageSetGuidePosition_NullGuideName_ThrowsArgumentNullException()
    {
        var commands = new PageCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGuidePosition(null!, 1, null!, 72, 144));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PageDeleteGuide_EmptyGuideName_ThrowsArgumentException(string guideName)
    {
        var commands = new PageCommands();
        Assert.Throws<ArgumentException>(() => commands.DeleteGuide(null!, 1, guideName));
    }

    // ── Export Commands ──────────────────────────────────────

    [Fact]
    public void ExportToPdf_NullDestinationPath_ThrowsArgumentNullException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ToPdf(null!, null!, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExportToPdf_EmptyDestinationPath_ThrowsArgumentException(string path)
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.ToPdf(null!, path, null, null));
    }

    [Fact]
    public void ExportToXps_NullDestinationPath_ThrowsArgumentNullException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ToXps(null!, null!, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExportToXps_EmptyDestinationPath_ThrowsArgumentException(string path)
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.ToXps(null!, path, null, null));
    }

    [Fact]
    public void ExportPageExport_InvalidPageIndex_ThrowsArgumentOutOfRangeException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.PageExport(null!, 0, "page.png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExportPageExport_EmptyDestinationPath_ThrowsArgumentException(string path)
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.PageExport(null!, 1, path));
    }

    // ── Export Commands (Additional) ─────────────────────────

    [Fact]
    public void ExportPageExport_MissingExtension_ThrowsArgumentException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.PageExport(null!, 1, "page-export"));
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(1, null)]
    public void ExportPrint_PartialPageRange_ThrowsArgumentException(int? fromPage, int? toPage)
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.Print(null!, 1, fromPage, toPage));
    }

    [Fact]
    public void ExportPrint_InvalidFromPage_ThrowsArgumentOutOfRangeException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.Print(null!, 1, 0, 1));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ExportPrint_InvalidToPage_ThrowsArgumentException(int? fromPage, int? toPage)
    {
        var commands = new ExportCommands();
        Assert.ThrowsAny<ArgumentException>(() => commands.Print(null!, 1, fromPage, toPage));
    }

    [Fact]
    public void ExportSaveCopy_NullDestinationPath_ThrowsArgumentNullException()
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SaveCopy(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExportSaveCopy_EmptyDestinationPath_ThrowsArgumentException(string path)
    {
        var commands = new ExportCommands();
        Assert.Throws<ArgumentException>(() => commands.SaveCopy(null!, path));
    }

    // ── Shape Commands ───────────────────────────────────────

    [Fact]
    public void ShapeRead_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Read(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeRead_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Read(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeMoveResize_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.MoveResize(null!, 1, null!, 0, 0, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeMoveResize_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.MoveResize(null!, 1, shapeName, 0, 0, null, null));
    }

    [Fact]
    public void ShapeDelete_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Delete(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeDelete_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Delete(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeZOrder_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ZOrder(null!, 1, null!, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeZOrder_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ZOrder(null!, 1, shapeName, 0));
    }

    [Fact]
    public void ShapeGroup_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Group(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeGroup_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Group(null!, 1, shapeNames));
    }

    [Fact]
    public void ShapeUngroup_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Ungroup(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeUngroup_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Ungroup(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeReadGroup_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadGroup(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeReadGroup_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadGroup(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeSelectShapes_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SelectShapes(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSelectShapes_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SelectShapes(null!, 1, shapeNames));
    }

    [Fact]
    public void ShapeAddToSelection_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.AddToSelection(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAddToSelection_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.AddToSelection(null!, 1, shapeNames));
    }

    [Fact]
    public void ShapeRemoveFromSelection_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.RemoveFromSelection(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeRemoveFromSelection_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.RemoveFromSelection(null!, 1, shapeNames));
    }

    [Fact]
    public void ShapeAddConnector_NullStartShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.AddConnector(null!, 1, 1, null!, "End"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAddConnector_EmptyStartShapeName_ThrowsArgumentException(string startShapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.AddConnector(null!, 1, 1, startShapeName, "End"));
    }

    [Fact]
    public void ShapeAddConnector_NullEndShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.AddConnector(null!, 1, 1, "Start", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAddConnector_EmptyEndShapeName_ThrowsArgumentException(string endShapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.AddConnector(null!, 1, 1, "Start", endShapeName));
    }

    [Fact]
    public void ShapeReadConnector_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadConnector(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeReadConnector_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadConnector(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeGetProperty_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.GetProperty(null!, 1, null!, "Cost Center"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeGetProperty_EmptyPropertyName_ThrowsArgumentException(string propertyName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.GetProperty(null!, 1, "Shape.1", propertyName));
    }

    [Fact]
    public void ShapeSetProperty_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetProperty(null!, 1, null!, "Cost Center", "IT"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetProperty_EmptyPropertyName_ThrowsArgumentException(string propertyName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetProperty(null!, 1, "Shape.1", propertyName, "IT"));
    }

    [Fact]
    public void ShapeDeleteProperty_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.DeleteProperty(null!, 1, null!, "Cost Center"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeDeleteProperty_EmptyPropertyName_ThrowsArgumentException(string propertyName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.DeleteProperty(null!, 1, "Shape.1", propertyName));
    }

    [Fact]
    public void ShapeMergeShapes_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.MergeShapes(null!, 1, null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeMergeShapes_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.MergeShapes(null!, 1, shapeNames, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ShapeMergeShapes_InvalidMergeType_ThrowsArgumentOutOfRangeException(int mergeType)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.MergeShapes(null!, 1, "Shape.1,Shape.2", mergeType));
    }

    [Fact]
    public void ShapeDuplicate_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Duplicate(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeDuplicate_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Duplicate(null!, 1, shapeName));
    }

    // ── Text Commands ────────────────────────────────────────

    [Fact]
    public void TextGetText_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.GetText(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextGetText_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.GetText(null!, 1, shapeName));
    }

    [Fact]
    public void TextSetText_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetText(null!, 1, null!, "text"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextSetText_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.SetText(null!, 1, shapeName, "text"));
    }

    // ── Document Property Commands ───────────────────────────

    [Fact]
    public void DocumentPropertyGetCustom_NullPropertyName_ThrowsArgumentNullException()
    {
        var commands = new DocumentPropertyCommands();
        Assert.Throws<ArgumentNullException>(() => commands.GetCustom(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DocumentPropertyGetCustom_EmptyPropertyName_ThrowsArgumentException(string propertyName)
    {
        var commands = new DocumentPropertyCommands();
        Assert.Throws<ArgumentException>(() => commands.GetCustom(null!, propertyName));
    }

    [Fact]
    public void DocumentPropertySetCustom_NullPropertyName_ThrowsArgumentNullException()
    {
        var commands = new DocumentPropertyCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetCustom(null!, null!, "value"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DocumentPropertySetCustom_EmptyPropertyName_ThrowsArgumentException(string propertyName)
    {
        var commands = new DocumentPropertyCommands();
        Assert.Throws<ArgumentException>(() => commands.SetCustom(null!, propertyName, "value"));
    }

    // ── Shape Align Commands ─────────────────────────────────

    [Fact]
    public void ShapeAlignAlign_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeAlignCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Align(null!, 1, null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAlignAlign_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeAlignCommands();
        Assert.Throws<ArgumentException>(() => commands.Align(null!, 1, shapeNames, 1));
    }

    [Fact]
    public void ShapeAlignDistribute_NullShapeNames_ThrowsArgumentNullException()
    {
        var commands = new ShapeAlignCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Distribute(null!, 1, null!, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAlignDistribute_EmptyShapeNames_ThrowsArgumentException(string shapeNames)
    {
        var commands = new ShapeAlignCommands();
        Assert.Throws<ArgumentException>(() => commands.Distribute(null!, 1, shapeNames, 0));
    }

}