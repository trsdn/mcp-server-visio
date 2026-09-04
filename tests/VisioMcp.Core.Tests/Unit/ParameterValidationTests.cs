using VisioMcp.Core.Commands.Comment;
using VisioMcp.Core.Commands.DocumentProperty;
using VisioMcp.Core.Commands.Export;
using VisioMcp.Core.Commands.Hyperlink;
using VisioMcp.Core.Commands.Image;
using VisioMcp.Core.Commands.Master;
using VisioMcp.Core.Commands.Page;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Commands.ShapeAlign;
using VisioMcp.Core.Commands.Text;
using VisioMcp.Core.Commands.Vba;
using VisioMcp.Core.Commands.Window;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Tests that Core Commands validate required parameters before executing.
/// These tests verify that ArgumentException/ArgumentNullException is thrown
/// for null/empty required parameters WITHOUT needing a PowerPoint COM connection.
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

    // ── Hyperlink Commands ───────────────────────────────────

    [Fact]
    public void HyperlinkAdd_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Add(null!, 1, null!, "https://example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HyperlinkAdd_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentException>(() => commands.Add(null!, 1, shapeName, "https://example.com"));
    }

    [Fact]
    public void HyperlinkAdd_NoTarget_ThrowsArgumentException()
    {
        var commands = new HyperlinkCommands();

        // Visio accepts a hyperlink row with neither target and does nothing with it, so this is
        // rejected up front rather than reported as a created link.
        Assert.Throws<ArgumentException>(() => commands.Add(null!, 1, "Shape1", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HyperlinkAdd_EmptyAddressAndNoSubAddress_ThrowsArgumentException(string address)
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentException>(() => commands.Add(null!, 1, "Shape1", address));
    }

    [Fact]
    public void HyperlinkRead_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Read(null!, 1, null!, "Row_1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HyperlinkRead_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentException>(() => commands.Read(null!, 1, shapeName, "Row_1"));
    }

    [Fact]
    public void HyperlinkRead_NullHyperlinkName_ThrowsArgumentNullException()
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Read(null!, 1, "Shape1", null!));
    }

    [Fact]
    public void HyperlinkUpdate_NoFieldsGiven_ThrowsArgumentException()
    {
        var commands = new HyperlinkCommands();

        // Every field is optional so omitted ones stay put; a call with none of them would be a
        // silent no-op reported as success.
        Assert.Throws<ArgumentException>(() => commands.Update(null!, 1, "Shape1", "Row_1"));
    }

    [Fact]
    public void HyperlinkDelete_NullHyperlinkName_ThrowsArgumentNullException()
    {
        var commands = new HyperlinkCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Delete(null!, 1, "Shape1", null!));
    }

    // ── VBA Commands ─────────────────────────────────────────

    [Fact]
    public void VbaView_NullModuleName_ThrowsArgumentNullException()
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentNullException>(() => commands.View(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VbaView_EmptyModuleName_ThrowsArgumentException(string moduleName)
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentException>(() => commands.View(null!, moduleName));
    }

    [Fact]
    public void VbaImport_NullModuleName_ThrowsArgumentNullException()
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Import(null!, null!, "Sub Test()\nEnd Sub", 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VbaImport_EmptyModuleName_ThrowsArgumentException(string moduleName)
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentException>(() => commands.Import(null!, moduleName, "Sub Test()\nEnd Sub", 1));
    }

    [Fact]
    public void VbaImport_NullCode_ThrowsArgumentNullException()
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Import(null!, "Module1", null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VbaImport_EmptyCode_ThrowsArgumentException(string code)
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentException>(() => commands.Import(null!, "Module1", code, 1));
    }

    [Fact]
    public void VbaDelete_NullModuleName_ThrowsArgumentNullException()
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Delete(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VbaDelete_EmptyModuleName_ThrowsArgumentException(string moduleName)
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentException>(() => commands.Delete(null!, moduleName));
    }

    [Fact]
    public void VbaRun_NullMacroName_ThrowsArgumentNullException()
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Run(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VbaRun_EmptyMacroName_ThrowsArgumentException(string macroName)
    {
        var commands = new VbaCommands();
        Assert.Throws<ArgumentException>(() => commands.Run(null!, macroName));
    }

    // ── Section Commands ─────────────────────────────────────

    // ── Animation Commands ───────────────────────────────────

    // ── Chart Commands ───────────────────────────────────────

    // ── Chart Commands (Additional) ─────────────────────────

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

    // ── Image Commands ──────────────────────────────────────

    [Fact]
    public void ImageCrop_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Crop(null!, 1, null!, 0f, 0f, 0f, 0f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageCrop_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentException>(() => commands.Crop(null!, 1, shapeName, 0f, 0f, 0f, 0f));
    }

    [Fact]
    public void ImageSetBrightnessContrast_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetBrightnessContrast(null!, 1, null!, 0.5f, 0.5f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageSetBrightnessContrast_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentException>(() => commands.SetBrightnessContrast(null!, 1, shapeName, 0.5f, 0.5f));
    }

    [Fact]
    public void ImageSetTransparentColor_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetTransparentColor(null!, 1, null!, "#FFFFFF"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageSetTransparentColor_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentException>(() => commands.SetTransparentColor(null!, 1, shapeName, "#FFFFFF"));
    }

    [Fact]
    public void ImageSetTransparentColor_NullColorHex_ThrowsArgumentNullException()
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetTransparentColor(null!, 1, "Image1", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageSetTransparentColor_EmptyColorHex_ThrowsArgumentException(string colorHex)
    {
        var commands = new ImageCommands();
        Assert.Throws<ArgumentException>(() => commands.SetTransparentColor(null!, 1, "Image1", colorHex));
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
    public void ShapeSetFill_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetFill(null!, 1, null!, "#FF0000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetFill_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetFill(null!, 1, shapeName, "#FF0000"));
    }

    [Fact]
    public void ShapeSetFill_NullColorHex_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetFill(null!, 1, "Shape1", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetFill_EmptyColorHex_ThrowsArgumentException(string colorHex)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetFill(null!, 1, "Shape1", colorHex));
    }

    [Fact]
    public void ShapeSetLine_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetLine(null!, 1, null!, "#FF0000", 1f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetLine_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetLine(null!, 1, shapeName, "#FF0000", 1f));
    }

    [Fact]
    public void ShapeSetLine_NullColorHex_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetLine(null!, 1, "Shape1", null!, 1f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetLine_EmptyColorHex_ThrowsArgumentException(string colorHex)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetLine(null!, 1, "Shape1", colorHex, 1f));
    }

    [Fact]
    public void ShapeSetRotation_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetRotation(null!, 1, null!, 45f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetRotation_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetRotation(null!, 1, shapeName, 45f));
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
    public void ShapeSetAltText_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetAltText(null!, 1, null!, "alt text"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetAltText_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetAltText(null!, 1, shapeName, "alt text"));
    }

    [Fact]
    public void ShapeCopyToPage_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.CopyToPage(null!, 1, null!, 2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeCopyToPage_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.CopyToPage(null!, 1, shapeName, 2));
    }

    [Fact]
    public void ShapeSetShadow_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetShadow(null!, 1, null!, true, 3f, 3f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetShadow_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetShadow(null!, 1, shapeName, true, 3f, 3f));
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

    [Fact]
    public void ShapeFlip_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Flip(null!, 1, null!, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeFlip_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Flip(null!, 1, shapeName, 0));
    }

    // ── Shape Commands (Additional) ─────────────────────────

    [Fact]
    public void ShapeSetTextFrame_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetTextFrame(null!, 1, null!, null, null, null, null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetTextFrame_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetTextFrame(null!, 1, shapeName, null, null, null, null, null, null));
    }

    [Fact]
    public void ShapeReadFill_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadFill(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeReadFill_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadFill(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeReadLine_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadLine(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeReadLine_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadLine(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeSetGradientFill_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGradientFill(null!, 1, null!, "#FF0000", "#0000FF", 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetGradientFill_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetGradientFill(null!, 1, shapeName, "#FF0000", "#0000FF", 1));
    }

    [Fact]
    public void ShapeSetGradientFill_NullColor1_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGradientFill(null!, 1, "Shape1", null!, "#0000FF", 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetGradientFill_EmptyColor1_ThrowsArgumentException(string color1)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetGradientFill(null!, 1, "Shape1", color1, "#0000FF", 1));
    }

    [Fact]
    public void ShapeSetGradientFill_NullColor2_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGradientFill(null!, 1, "Shape1", "#FF0000", null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetGradientFill_EmptyColor2_ThrowsArgumentException(string color2)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetGradientFill(null!, 1, "Shape1", "#FF0000", color2, 1));
    }

    [Fact]
    public void ShapeSetGlow_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGlow(null!, 1, null!, 5f, "#FF0000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetGlow_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetGlow(null!, 1, shapeName, 5f, "#FF0000"));
    }

    [Fact]
    public void ShapeSetGlow_NullColorHex_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetGlow(null!, 1, "Shape1", 5f, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetGlow_EmptyColorHex_ThrowsArgumentException(string colorHex)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetGlow(null!, 1, "Shape1", 5f, colorHex));
    }

    [Fact]
    public void ShapeSetReflection_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetReflection(null!, 1, null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetReflection_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetReflection(null!, 1, shapeName, 1));
    }

    [Fact]
    public void ShapeSetOpacity_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetOpacity(null!, 1, null!, 0.5f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetOpacity_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetOpacity(null!, 1, shapeName, 0.5f));
    }

    [Fact]
    public void ShapeScale_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Scale(null!, 1, null!, 1.5f, 1.5f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeScale_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Scale(null!, 1, shapeName, 1.5f, 1.5f));
    }

    [Fact]
    public void ShapeSetLockAspectRatio_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetLockAspectRatio(null!, 1, null!, true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetLockAspectRatio_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetLockAspectRatio(null!, 1, shapeName, true));
    }

    [Fact]
    public void ShapeSetSoftEdge_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetSoftEdge(null!, 1, null!, 5f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSetSoftEdge_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.SetSoftEdge(null!, 1, shapeName, 5f));
    }

    [Fact]
    public void ShapeReadShadow_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadShadow(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeReadShadow_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadShadow(null!, 1, shapeName));
    }

    [Fact]
    public void ShapeAddTextEffect_NullText_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.AddTextEffect(null!, 1, 0, null!, "Arial", 24f, 100f, 100f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAddTextEffect_EmptyText_ThrowsArgumentException(string text)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.AddTextEffect(null!, 1, 0, text, "Arial", 24f, 100f, 100f));
    }

    [Fact]
    public void ShapeAddTextEffect_NullFontName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.AddTextEffect(null!, 1, 0, "Text", null!, 24f, 100f, 100f));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeAddTextEffect_EmptyFontName_ThrowsArgumentException(string fontName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.AddTextEffect(null!, 1, 0, "Text", fontName, 24f, 100f, 100f));
    }

    [Fact]
    public void ShapeSet3D_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Set3D(null!, 1, null!, null, null, null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeSet3D_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.Set3D(null!, 1, shapeName, null, null, null, null, null));
    }

    [Fact]
    public void ShapeCopyFormatting_NullSourceShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.CopyFormatting(null!, 1, null!, "Target"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeCopyFormatting_EmptySourceShapeName_ThrowsArgumentException(string sourceShapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.CopyFormatting(null!, 1, sourceShapeName, "Target"));
    }

    [Fact]
    public void ShapeCopyFormatting_NullTargetShapeName_ThrowsArgumentNullException()
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentNullException>(() => commands.CopyFormatting(null!, 1, "Source", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShapeCopyFormatting_EmptyTargetShapeName_ThrowsArgumentException(string targetShapeName)
    {
        var commands = new ShapeCommands();
        Assert.Throws<ArgumentException>(() => commands.CopyFormatting(null!, 1, "Source", targetShapeName));
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

    [Fact]
    public void TextFormat_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Format(null!, 1, null!, null, null, null, null, null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextFormat_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.Format(null!, 1, shapeName, null, null, null, null, null, null, null));
    }

    [Fact]
    public void TextFormatAdvanced_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.FormatAdvanced(null!, 1, null!, null, null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextFormatAdvanced_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.FormatAdvanced(null!, 1, shapeName, null, null, null, null));
    }

    // ── Text Commands (Additional) ──────────────────────────

    [Fact]
    public void TextSetSpacing_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetSpacing(null!, 1, null!, null, null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextSetSpacing_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.SetSpacing(null!, 1, shapeName, null, null, null, null));
    }

    [Fact]
    public void TextSetBullets_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.SetBullets(null!, 1, null!, 1, null, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextSetBullets_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.SetBullets(null!, 1, shapeName, 1, null, 0));
    }

    [Fact]
    public void TextInsertLink_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertLink(null!, 1, null!, "Click here", "https://example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertLink_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertLink(null!, 1, shapeName, "Click here", "https://example.com"));
    }

    [Fact]
    public void TextInsertLink_NullLinkText_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertLink(null!, 1, "Shape1", null!, "https://example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertLink_EmptyLinkText_ThrowsArgumentException(string linkText)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertLink(null!, 1, "Shape1", linkText, "https://example.com"));
    }

    [Fact]
    public void TextInsertLink_NullUrl_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertLink(null!, 1, "Shape1", "Click here", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertLink_EmptyUrl_ThrowsArgumentException(string url)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertLink(null!, 1, "Shape1", "Click here", url));
    }

    [Fact]
    public void TextChangeCase_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ChangeCase(null!, 1, null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextChangeCase_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.ChangeCase(null!, 1, shapeName, 1));
    }

    [Fact]
    public void TextReadSpacing_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadSpacing(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextReadSpacing_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadSpacing(null!, 1, shapeName));
    }

    [Fact]
    public void TextReadBullets_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ReadBullets(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextReadBullets_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.ReadBullets(null!, 1, shapeName));
    }

    [Fact]
    public void TextInsertSymbol_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertSymbol(null!, 1, null!, "Wingdings", 65));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertSymbol_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertSymbol(null!, 1, shapeName, "Wingdings", 65));
    }

    [Fact]
    public void TextInsertSymbol_NullFontName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertSymbol(null!, 1, "Shape1", null!, 65));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertSymbol_EmptyFontName_ThrowsArgumentException(string fontName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertSymbol(null!, 1, "Shape1", fontName, 65));
    }

    [Fact]
    public void TextInsertDateTime_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertDateTime(null!, 1, null!, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertDateTime_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertDateTime(null!, 1, shapeName, 1));
    }

    [Fact]
    public void TextInsertPageNumber_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentNullException>(() => commands.InsertPageNumber(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextInsertPageNumber_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new TextCommands();
        Assert.Throws<ArgumentException>(() => commands.InsertPageNumber(null!, 1, shapeName));
    }

    // ── Background Commands ──────────────────────────────────
    // Removed in #36c: the background domain was superseded. Background *pages* moved to
    // page(set-background / set-back-page); background *fill* is a PageSheet cell reached with
    // cell(set-formula, sheet_target='page', cell_name='FillForegnd').

    // ── SmartArt Commands ────────────────────────────────────

    // ── SmartArt Commands (Additional) ──────────────────────

    // ── Comment Commands ─────────────────────────────────────

    [Fact]
    public void CommentAdd_NullText_ThrowsArgumentNullException()
    {
        var commands = new CommentCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Add(null!, 1, null!, "Author", 0, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CommentAdd_EmptyText_ThrowsArgumentException(string text)
    {
        var commands = new CommentCommands();
        Assert.Throws<ArgumentException>(() => commands.Add(null!, 1, text, "Author", 0, 0));
    }

    [Fact]
    public void CommentAdd_NullAuthor_ThrowsArgumentNullException()
    {
        var commands = new CommentCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Add(null!, 1, "Comment text", null!, 0, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CommentAdd_EmptyAuthor_ThrowsArgumentException(string author)
    {
        var commands = new CommentCommands();
        Assert.Throws<ArgumentException>(() => commands.Add(null!, 1, "Comment text", author, 0, 0));
    }

    // ── Custom Show Commands ─────────────────────────────────

    // ── Slide Import Commands ────────────────────────────────

    // ── Media Commands ───────────────────────────────────────

    // ── Media Commands (Additional) ─────────────────────────

    // ── Slide Commands ───────────────────────────────────────

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

    // ── Slide Table Commands ─────────────────────────────────

    // ── Slide Table Commands (Additional) ────────────────────

    // ── Background Commands (Additional) ────────────────────
    // Removed in #36c along with the domain itself.

    // ── Proofing Commands ───────────────────────────────────

    // ── Master Commands ─────────────────────────────────────

    [Fact]
    public void MasterRead_NullMasterName_ThrowsArgumentNullException()
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Read(null!, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MasterRead_EmptyMasterName_ThrowsArgumentException(string masterName)
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentException>(() => commands.Read(null!, masterName));
    }

    [Fact]
    public void MasterCreateFromShape_NullShapeName_ThrowsArgumentNullException()
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentNullException>(() => commands.CreateFromShape(null!, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MasterCreateFromShape_EmptyShapeName_ThrowsArgumentException(string shapeName)
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentException>(() => commands.CreateFromShape(null!, 1, shapeName));
    }

    [Fact]
    public void MasterRename_NullNewName_ThrowsArgumentNullException()
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Rename(null!, "Widget", null!));
    }

    [Fact]
    public void MasterDelete_NullMasterName_ThrowsArgumentNullException()
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentNullException>(() => commands.Delete(null!, null!));
    }

    [Fact]
    public void MasterListInstances_NullMasterName_ThrowsArgumentNullException()
    {
        var commands = new MasterCommands();
        Assert.Throws<ArgumentNullException>(() => commands.ListInstances(null!, null!));
    }

    // ── Slide Commands (Additional) ─────────────────────────

}