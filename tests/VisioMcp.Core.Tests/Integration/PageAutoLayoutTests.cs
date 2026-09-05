using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Page;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Page auto-layout and connector rerouting (#126).
///
/// Layout is intentionally destructive: these tests assert geometry changes against a real Visio
/// document rather than accepting a success flag.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Page")]
public sealed class PageAutoLayoutTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly PageCommands _pages = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void LayoutSelection_MovesOnlyTheSelectedConnectedShapes()
    {
        using var batch = CreateDocument();
        CreateConnectedPair(batch, "A", "B", 1.5, 1.5);
        CreateConnectedPair(batch, "C", "D", 4.5, 4.5);
        var before = ReadPositions(batch, "A", "B", "C", "D");

        var result = _pages.LayoutSelection(batch, 1, "A,B");
        var after = ReadPositions(batch, "A", "B", "C", "D");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("selection", result.Scope);
        AssertMoved(before["A"], after["A"], "A");
        AssertMoved(before["B"], after["B"], "B");
        AssertUnchanged(before["C"], after["C"], "C");
        AssertUnchanged(before["D"], after["D"], "D");
        Assert.Contains(result.Shapes, s => s.Name == "A" && s.Moved);
        Assert.Contains(result.Shapes, s => s.Name == "B" && s.Moved);
    }

    [Fact]
    public void LayoutPage_MovesEveryConnectedComponentOnThePage()
    {
        using var batch = CreateDocument();
        CreateConnectedPair(batch, "A", "B", 1.5, 1.5);
        CreateConnectedPair(batch, "C", "D", 4.5, 4.5);
        var before = ReadPositions(batch, "A", "B", "C", "D");

        var result = _pages.LayoutPage(batch, 1);
        var after = ReadPositions(batch, "A", "B", "C", "D");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("page", result.Scope);
        foreach (string name in before.Keys)
        {
            AssertMoved(before[name], after[name], name);
            Assert.Contains(result.Shapes, s => s.Name == name && s.Moved);
        }
    }

    [Fact]
    public void IncrementalLayout_ReportsWhenVisioMakesNoVisibleAdjustment()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "A", 1, 1, 2, 2);
        CreateNamedRectangle(batch, "B", 3, 2.25, 4, 3.25);
        CreateNamedRectangle(batch, "C", 6, 5, 7, 6);
        var before = ReadPositions(batch, "A", "B", "C");

        var result = _pages.IncrementalLayout(batch, 1, "A,B", alignOrSpace: 1, alignHorizontal: 2, alignVertical: 0);
        var after = ReadPositions(batch, "A", "B", "C");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("selection", result.Scope);
        Assert.Contains("no visible shape movement", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.All(result.Shapes, shape => Assert.False(shape.Moved));
        AssertUnchanged(before["A"], after["A"], "A");
        AssertUnchanged(before["B"], after["B"], "B");
        AssertUnchanged(before["C"], after["C"], "C");
    }

    [Fact]
    public void SetPassiveRouting_TogglesTheVisioPageRoutingFlag()
    {
        using var batch = CreateDocument();

        var enabled = _pages.SetPassiveRouting(batch, 1, true);
        var disabled = _pages.SetPassiveRouting(batch, 1, false);

        Assert.True(enabled.Success, enabled.ErrorMessage);
        Assert.True(disabled.Success, disabled.ErrorMessage);
        Assert.True(enabled.LayoutRoutePassive);
        Assert.False(disabled.LayoutRoutePassive);
    }

    [Fact]
    public void LayoutSettings_ReadAndWriteThePageSheetCellsThatDriveLayout()
    {
        using var batch = CreateDocument();

        var defaults = _pages.GetRoutingSettings(batch, 1);
        var lineRoute = _pages.SetLineRouteExtension(batch, 1, 2);
        var spacing = _pages.SetLayoutSpacing(batch, 1, 36f, 48f);
        var depth = _pages.SetPlaceDepth(batch, 1, 2);
        var resize = _pages.SetResizePage(batch, 1, true);

        Assert.True(defaults.Success, defaults.ErrorMessage);
        Assert.Equal(0, defaults.PlaceStyle);
        Assert.Equal(0, defaults.RouteStyle);
        Assert.Equal(0, defaults.LineRouteExtension);
        Assert.Equal(0, defaults.PlaceDepth);
        Assert.False(defaults.ResizePage);
        Assert.False(defaults.LayoutRoutePassive);
        Assert.InRange(defaults.AvenueSizeX, 21.25f, 21.27f);
        Assert.InRange(defaults.AvenueSizeY, 21.25f, 21.27f);

        Assert.True(lineRoute.Success, lineRoute.ErrorMessage);
        Assert.Equal(2, spacing.LineRouteExtension);
        Assert.Equal(36f, spacing.AvenueSizeX, precision: 3);
        Assert.Equal(48f, spacing.AvenueSizeY, precision: 3);
        Assert.Equal(2, depth.PlaceDepth);
        Assert.True(resize.ResizePage);
    }

    [Fact]
    public void LayoutResult_SurvivesSaveAndReopen()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        Dictionary<string, Position> afterLayout;
        using (var batch = VisioSession.BeginBatch(path))
        {
            CreateConnectedPair(batch, "A", "B", 1.5, 1.5);
            _pages.LayoutPage(batch, 1);
            afterLayout = ReadPositions(batch, "A", "B");
            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);
        var afterReopen = ReadPositions(reopened, "A", "B");

        AssertEqualPosition(afterLayout["A"], afterReopen["A"], "A");
        AssertEqualPosition(afterLayout["B"], afterReopen["B"], "B");
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private void CreateConnectedPair(IVisioBatch batch, string firstName, string secondName, double left, double bottom)
    {
        CreateNamedRectangle(batch, firstName, left, bottom, left + 1, bottom + 1);
        CreateNamedRectangle(batch, secondName, left + 0.05, bottom + 0.05, left + 1.05, bottom + 1.05);
        var connected = _shapes.ConnectShapes(batch, 1, $"{firstName},{secondName}");
        Assert.True(connected.Success, connected.ErrorMessage);
    }

    private static void CreateNamedRectangle(IVisioBatch batch, string shapeName, double x1, double y1, double x2, double y2)
    {
        batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shape = page.DrawRectangle(x1, y1, x2, y2);
                shape.Name = shapeName;
                return 0;
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static Dictionary<string, Position> ReadPositions(IVisioBatch batch, params string[] shapeNames) =>
        batch.Execute((ctx, ct) =>
        {
            var positions = new Dictionary<string, Position>(StringComparer.OrdinalIgnoreCase);
            dynamic? pages = null;
            dynamic? page = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                foreach (string shapeName in shapeNames)
                {
                    dynamic? shape = null;
                    try
                    {
                        shape = page.Shapes.Item(shapeName);
                        positions[shapeName] = new Position(
                            ReadCellResultIU(shape, "PinX"),
                            ReadCellResultIU(shape, "PinY"));
                    }
                    finally
                    {
                        if (shape != null) ComUtilities.Release(ref shape!);
                    }
                }

                return positions;
            }
            finally
            {
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });

    private static double ReadCellResultIU(dynamic shape, string cellName)
    {
        dynamic cell = shape.CellsU(cellName);
        try
        {
            return Convert.ToDouble(cell.ResultIU);
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static void AssertMoved(Position before, Position after, string shapeName) =>
        Assert.True(Distance(before, after) > 0.1, $"expected '{shapeName}' to move, before={before}, after={after}");

    private static void AssertUnchanged(Position before, Position after, string shapeName) =>
        Assert.True(Distance(before, after) < 0.001, $"expected '{shapeName}' to stay put, before={before}, after={after}");

    private static void AssertEqualPosition(Position expected, Position actual, string shapeName) =>
        Assert.True(Distance(expected, actual) < 0.001, $"expected '{shapeName}' to persist at {expected}, actual={actual}");

    private static double Distance(Position first, Position second)
    {
        double dx = first.Left - second.Left;
        double dy = first.Top - second.Top;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private sealed record Position(double Left, double Top);
}
