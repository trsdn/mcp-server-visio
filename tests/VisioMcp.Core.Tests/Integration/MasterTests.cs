using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Master;
using VisioMcp.Core.Commands.Shape;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// The master tool, reimplemented on <c>Document.Masters</c> (#34).
///
/// Every action here previously threw <c>RuntimeBinderException</c>: the implementation was
/// PowerPoint's <c>SlideMasters</c>/<c>CustomLayouts</c>, which a Visio Document does not have.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Master")]
public sealed class MasterTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly MasterCommands _masters = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void List_OnABlankDocument_SucceedsAndIsEmpty()
    {
        using var batch = CreateDocument();

        var result = _masters.List(batch);

        // A blank Visio document genuinely owns no masters — this is not a failure, and the
        // PowerPoint-backed implementation could not report it because it threw first.
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(result.Masters);
    }

    [Fact]
    public void CreateFromShape_PromotesAShapeIntoAMaster()
    {
        using var batch = CreateDocument();
        var shapeName = AddRect(batch);

        var created = _masters.CreateFromShape(batch, 1, shapeName, "Widget");

        Assert.True(created.Success, created.ErrorMessage);
        Assert.Equal("Widget", created.Master!.Name);
        Assert.True(created.Master.ShapeCount >= 1);
        Assert.Contains(_masters.List(batch).Masters, m => m.Name == "Widget");
    }

    [Fact]
    public void CreateFromShape_LeavesTheOriginalShapeOnThePage()
    {
        using var batch = CreateDocument();
        var shapeName = AddRect(batch);
        int before = _shapes.List(batch, 1).Shapes.Count;

        _masters.CreateFromShape(batch, 1, shapeName, "Widget");

        // Document.Drop copies the definition; it does not move or consume the shape.
        var after = _shapes.List(batch, 1);
        Assert.Equal(before, after.Shapes.Count);
        Assert.Contains(after.Shapes, s => s.Name == shapeName);
    }

    [Fact]
    public void CreateFromShape_WithoutAName_LetsVisioAssignOne()
    {
        using var batch = CreateDocument();
        var shapeName = AddRect(batch);

        var created = _masters.CreateFromShape(batch, 1, shapeName);

        Assert.True(created.Success, created.ErrorMessage);
        Assert.False(string.IsNullOrWhiteSpace(created.Master!.Name));
    }

    [Fact]
    public void Read_ReturnsTheIdentifyingFields()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");

        var read = _masters.Read(batch, "Widget");

        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("Widget", read.Master!.Name);
        Assert.True(read.Master.Id > 0);

        // UniqueID survives copying between documents, which is what makes it worth returning.
        Assert.False(string.IsNullOrWhiteSpace(read.Master.UniqueId));
    }

    [Fact]
    public void Rename_ChangesTheNameUsedForLookup()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");

        var renamed = _masters.Rename(batch, "Widget", "Gadget");

        Assert.True(renamed.Success, renamed.ErrorMessage);
        Assert.Equal("Gadget", renamed.Master!.Name);
        Assert.True(_masters.Read(batch, "Gadget").Success);
    }

    [Fact]
    public void ListInstances_FindsShapesDroppedFromTheMaster()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");
        DropWidget(batch, 3.0, 3.0);
        DropWidget(batch, 5.0, 5.0);

        var instances = _masters.ListInstances(batch, "Widget");

        Assert.True(instances.Success, instances.ErrorMessage);
        Assert.Equal(2, instances.Instances.Count);
        Assert.All(instances.Instances, i => Assert.Equal(1, i.PageIndex));
    }

    [Fact]
    public void ListInstances_DoesNotCountTheShapeTheMasterWasMadeFrom()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");

        var instances = _masters.ListInstances(batch, "Widget");

        // Promoting a shape copies its definition; the original stays an independent shape with no
        // Master, so it is not an instance. Reporting it as one would misstate reuse.
        Assert.Empty(instances.Instances);
    }

    [Fact]
    public void Delete_RemovesTheDefinitionButKeepsExistingInstances()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");
        DropWidget(batch, 3.0, 3.0);
        int shapesBefore = _shapes.List(batch, 1).Shapes.Count;

        var deleted = _masters.Delete(batch, "Widget");

        Assert.True(deleted.Success, deleted.ErrorMessage);
        Assert.DoesNotContain(_masters.List(batch).Masters, m => m.Name == "Widget");

        // Verified against a live instance: the placed shape survives its master's deletion.
        Assert.Equal(shapesBefore, _shapes.List(batch, 1).Shapes.Count);
    }

    [Fact]
    public void Delete_SaysThatInstancesSurvive()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");

        var deleted = _masters.Delete(batch, "Widget");

        // A caller reasonably expects deleting a definition to remove what uses it. It does not,
        // so the message has to say so.
        Assert.Contains("unaffected", deleted.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownMaster_OnAnEmptyDocument_SaysHowToGetOne()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<ArgumentException>(() => _masters.Read(batch, "NoSuchMaster"));

        // Visio's own message is "Object name not found", which does not help when the real
        // situation is that the document has no masters at all.
        Assert.Contains("no masters", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create-from-shape", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownMaster_WhenOthersExist_ListsThem()
    {
        using var batch = CreateDocument();
        _masters.CreateFromShape(batch, 1, AddRect(batch), "Widget");

        var ex = Assert.Throws<ArgumentException>(() => _masters.Read(batch, "NoSuchMaster"));

        Assert.Contains("Widget", ex.Message, StringComparison.Ordinal);
    }

    private static void DropWidget(IVisioBatch batch, double x, double y) =>
        batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic master = ctx.Document.Masters["Widget"];
            page.Drop(master, x, y);
            return 0;
        });

    private string AddRect(IVisioBatch batch)
    {
        var added = _shapes.AddShape(batch, 1, 1, 1.0f, 1.0f, 1.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        var listed = _shapes.List(batch, 1);
        return listed.Shapes[^1].Name;
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"MasterTests_{Guid.NewGuid():N}.vsdx");
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
