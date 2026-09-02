using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Shape;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Connecting shapes with Visio's own dynamic connectors (#36e), and making
/// <c>connectorType</c> mean something on <c>add-connector</c>.
///
/// Two things separate a dynamic connector from the glued straight line <c>add-connector</c> drew:
/// it is an instance of the <c>Dynamic connector</c> master, and it routes around obstacles rather
/// than passing through them. Both are asserted here rather than assumed.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Shape")]
public sealed class ConnectShapesTests : IDisposable
{
    private const int VisSectionFirstComponent = 10;

    private readonly List<string> _tempFiles = [];
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void ConnectShapes_CreatesOneConnectorForTwoShapes()
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);

        var result = _shapes.ConnectShapes(batch, 1, $"{a},{b}");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Single(result.Connectors);
        Assert.Equal(a, result.Connectors[0].StartShapeName);
        Assert.Equal(b, result.Connectors[0].EndShapeName);
    }

    [Fact]
    public void ConnectShapes_ChainsInTheOrderGiven()
    {
        using var batch = CreateDocument();
        var a = AddRect(batch, 1.0f, 1.0f);
        var b = AddRect(batch, 4.0f, 1.0f);
        var c = AddRect(batch, 7.0f, 1.0f);

        var result = _shapes.ConnectShapes(batch, 1, $"{a},{b},{c}");

        // Three shapes chain into two connectors, not three and not one.
        Assert.Equal(2, result.Connectors.Count);
        Assert.Equal(a, result.Connectors[0].StartShapeName);
        Assert.Equal(b, result.Connectors[0].EndShapeName);
        Assert.Equal(b, result.Connectors[1].StartShapeName);
        Assert.Equal(c, result.Connectors[1].EndShapeName);
    }

    [Fact]
    public void ConnectShapes_ProducesARealDynamicConnector()
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);

        var result = _shapes.ConnectShapes(batch, 1, $"{a},{b}");
        var name = result.Connectors[0].Name;

        // The distinguishing properties of a dynamic connector, which a drawn line does not have.
        var (master, connects, objType) = batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.Shapes.Item(name);
            string m = shape.Master == null ? "NONE" : (string)shape.Master.NameU;
            return (m, (int)shape.Connects.Count, (string)shape.CellsU["ObjType"].FormulaU);
        });

        Assert.Equal("Dynamic connector", master);
        Assert.Equal(2, connects);      // glued at both ends
        Assert.Equal("2", objType);     // routable
    }

    [Fact]
    public void ConnectShapes_RoutesAroundAShapeInTheWay()
    {
        using var batch = CreateDocument();
        var a = AddRect(batch, 1.0f, 5.0f);
        AddRect(batch, 3.0f, 4.5f, height: 2.0f);   // obstacle directly between them
        var c = AddRect(batch, 6.0f, 5.0f);

        var result = _shapes.ConnectShapes(batch, 1, $"{a},{c}");
        var name = result.Connectors[0].Name;

        var vertices = batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.Shapes.Item(name);
            return (int)shape.Section(VisSectionFirstComponent).Count;
        });

        // A straight line has two vertices. A detour needs more. This is the property that
        // distinguishes a dynamic connector from the glued line add-connector used to draw.
        Assert.True(vertices > 2, $"expected the connector to route around the obstacle, but it had {vertices} vertices");
    }

    [Theory]
    [InlineData(1, "2", "1")]   // straight
    [InlineData(2, "1", "1")]   // elbow / right angle
    [InlineData(3, "1", "2")]   // curved
    public void ConnectShapes_AppliesTheRequestedRoutingStyle(int connectorType, string routeStyle, string lineRouteExt)
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);

        var result = _shapes.ConnectShapes(batch, 1, $"{a},{b}", connectorType);
        var name = result.Connectors[0].Name;

        var (actualRoute, actualExt) = ReadRouting(batch, name);

        Assert.Equal(routeStyle, actualRoute);
        Assert.Equal(lineRouteExt, actualExt);
    }

    [Theory]
    [InlineData(1, "2", "1")]
    [InlineData(2, "1", "1")]
    [InlineData(3, "1", "2")]
    public void AddConnector_NoLongerIgnoresConnectorType(int connectorType, string routeStyle, string lineRouteExt)
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);

        var added = _shapes.AddConnector(batch, 1, connectorType, a, b);
        Assert.True(added.Success, added.ErrorMessage);

        var name = LastShapeName(batch);
        var (actualRoute, actualExt) = ReadRouting(batch, name);

        // Before #36e every connectorType produced an identical straight line, and said it had not.
        Assert.Equal(routeStyle, actualRoute);
        Assert.Equal(lineRouteExt, actualExt);
    }

    [Fact]
    public void ConnectShapes_WithOneShape_ExplainsWhatIsNeeded()
    {
        using var batch = CreateDocument();
        var a = AddRect(batch, 1.0f, 1.0f);

        var ex = Assert.Throws<ArgumentException>(() => _shapes.ConnectShapes(batch, 1, a));

        // Visio's own message here is "Requested operation is presently disabled", which tells an
        // agent nothing about what to do differently.
        Assert.Contains("at least two", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectShapes_WithAnUnknownRoutingStyle_NamesTheValidOnes()
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);

        var ex = Assert.Throws<ArgumentException>(() => _shapes.ConnectShapes(batch, 1, $"{a},{b}", 9));

        Assert.Contains("straight", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("elbow", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("curved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConnectShapes_WithAnUnknownRoutingStyle_CreatesNothing()
    {
        using var batch = CreateDocument();
        var (a, b) = TwoShapes(batch);
        int before = ShapeCount(batch);

        Assert.Throws<ArgumentException>(() => _shapes.ConnectShapes(batch, 1, $"{a},{b}", 9));

        // Validated before the document is touched, so a bad style leaves no half-built chain.
        Assert.Equal(before, ShapeCount(batch));
    }

    private static (string RouteStyle, string LineRouteExt) ReadRouting(IVisioBatch batch, string shapeName) =>
        batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.Shapes.Item(shapeName);
            return ((string)shape.CellsU["ShapeRouteStyle"].FormulaU,
                    (string)shape.CellsU["ConLineRouteExt"].FormulaU);
        });

    private static int ShapeCount(IVisioBatch batch) =>
        batch.Execute((ctx, ct) => (int)ctx.Document.Pages[1].Shapes.Count);

    private static string LastShapeName(IVisioBatch batch) =>
        batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            return (string)page.Shapes.Item((int)page.Shapes.Count).Name;
        });

    private (string, string) TwoShapes(IVisioBatch batch) =>
        (AddRect(batch, 1.0f, 1.0f), AddRect(batch, 5.0f, 4.0f));

    private string AddRect(IVisioBatch batch, float left, float top, float width = 1.0f, float height = 1.0f)
    {
        var added = _shapes.AddShape(batch, 1, 1, left, top, width, height);
        Assert.True(added.Success, added.ErrorMessage);
        return LastShapeName(batch);
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"ConnectShapesTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
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
