using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Cell;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Models;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for connection point CRUD (#32).
///
/// A connection point is not a connector. A connector is the line between two shapes; a connection
/// point is a position <i>on</i> a shape that a connector attaches to. The `shape` tool already had
/// connector actions and none for the anchors they glue to.
///
/// Per Rule 30 these run against real Visio. The column layout of the Connections section is
/// positional and undocumented in this repository, so it was read back from a live instance:
/// 0 = X, 1 = Y, 2 = DirX, 3 = DirY, 4 = Type, 5 = AutoGen.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Shape")]
public sealed class ConnectionPointTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly ShapeCommands _shapes = new();
    private readonly CellCommands _cells = new();

    [Fact]
    public void ANewShape_HasNoConnectionPoints()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var listed = _shapes.ListConnectionPoints(batch, 1, shapeName);

        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.Empty(listed.ConnectionPoints);
    }

    [Fact]
    public void AddConnectionPoint_Named_BecomesAGlueTarget()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var added = _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*1", "Top");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.NotNull(added.ConnectionPoint);
        Assert.Equal("Top", added.ConnectionPoint!.Name);
        Assert.Equal("Connections.Top", added.ConnectionPoint.GlueTarget);
        Assert.Equal("Width*0.5", added.ConnectionPoint.X);
        Assert.Equal("Height*1", added.ConnectionPoint.Y);
    }

    [Fact]
    public void AddConnectionPoint_Unnamed_IsAddressedByIndex()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var added = _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0", "Height*0.5");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.Equal(0, added.ConnectionPoint!.RowIndex);
        Assert.Equal(string.Empty, added.ConnectionPoint.Name);

        // An unnamed point still has a usable cell name, but it is positional.
        Assert.Equal("Connections.X1", added.ConnectionPoint.GlueTarget);
    }

    [Fact]
    public void ConnectionPointPosition_IsAFormula_SoItFollowsAResize()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*1", "Top");

        // The point is defined relative to the shape, so widening the shape moves it. This is the
        // entire reason these cells take expressions rather than co-ordinates.
        var before = _cells.Read(batch, 1, shapeName, "Connections.Top.X");
        Assert.True(before.Success, before.ErrorMessage);
        var beforeX = double.Parse(before.Cell!.Value!, System.Globalization.CultureInfo.InvariantCulture);

        var resized = _shapes.MoveResize(batch, 1, shapeName, null, null, 288.0f, null);
        Assert.True(resized.Success, resized.ErrorMessage);

        var after = _cells.Read(batch, 1, shapeName, "Connections.Top.X");
        var afterX = double.Parse(after.Cell!.Value!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(afterX > beforeX, $"Expected the connection point to follow the resize: {beforeX} -> {afterX}");
    }

    [Fact]
    public void ListConnectionPoints_ReportsEveryPoint()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*1", "Top");
        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*0", "Bottom");

        var listed = _shapes.ListConnectionPoints(batch, 1, shapeName);

        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.Equal(2, listed.ConnectionPoints.Count);
        Assert.Contains(listed.ConnectionPoints, p => p.Name == "Top" && p.GlueTarget == "Connections.Top");
        Assert.Contains(listed.ConnectionPoints, p => p.Name == "Bottom" && p.GlueTarget == "Connections.Bottom");
    }

    [Fact]
    public void SetConnectionPoint_MovesIt()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0", "Height*0", "Corner");

        var moved = _shapes.SetConnectionPoint(batch, 1, shapeName, 0, "Width*1", "Height*1");

        Assert.True(moved.Success, moved.ErrorMessage);
        Assert.Equal("Width*1", moved.ConnectionPoint!.X);
        Assert.Equal("Height*1", moved.ConnectionPoint.Y);

        // The name survives a move; only the position changed.
        Assert.Equal("Corner", moved.ConnectionPoint.Name);
    }

    [Fact]
    public void DeleteConnectionPoint_RemovesIt()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*1", "Top");
        Assert.Single(_shapes.ListConnectionPoints(batch, 1, shapeName).ConnectionPoints);

        var deleted = _shapes.DeleteConnectionPoint(batch, 1, shapeName, 0);

        Assert.True(deleted.Success, deleted.ErrorMessage);
        Assert.Empty(_shapes.ListConnectionPoints(batch, 1, shapeName).ConnectionPoints);
    }

    [Fact]
    public void DeleteConnectionPoint_BeyondTheEnd_ReportsTheActualCount()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _shapes.AddConnectionPoint(batch, 1, shapeName, "Width*0.5", "Height*1", "Only");

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _shapes.DeleteConnectionPoint(batch, 1, shapeName, 4));

        Assert.Contains("1 connection point(s)", ex.Message, StringComparison.Ordinal);
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"ConnectionPointTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    private string AddRectangle(IVisioBatch batch)
    {
        var added = _shapes.AddShape(batch, 1, 1, 1.0f, 1.0f, 2.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        ShapeListResult listed = _shapes.List(batch, 1);
        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.NotEmpty(listed.Shapes);

        return listed.Shapes[^1].Name;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // The file may still be briefly held after the batch disposes.
            }
        }
    }
}
