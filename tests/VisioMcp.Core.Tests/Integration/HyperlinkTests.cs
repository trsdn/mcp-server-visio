using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Hyperlink;
using VisioMcp.Core.Commands.Shape;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// The hyperlink tool, reimplemented on <c>Shape.Hyperlinks</c> (#35).
///
/// Every action previously threw <c>RuntimeBinderException</c>: the implementation was PowerPoint's
/// <c>ActionSettings</c>/<c>ppActionHyperlink</c>. The modelling differs — a Visio shape carries a
/// *collection* of hyperlinks, not the single click-action PowerPoint attaches — which is why this
/// is a new interface rather than a retargeted one.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Hyperlink")]
public sealed class HyperlinkTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly HyperlinkCommands _hyperlinks = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void Add_AttachesAnExternalLink()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        var added = _hyperlinks.Add(batch, 1, shape, "https://example.com", description: "Example");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.Equal("https://example.com", added.Hyperlink!.Address);
        Assert.Equal("Example", added.Hyperlink.Description);
    }

    [Fact]
    public void ListForShape_ReadsBackTheFirstHyperlink()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);
        _hyperlinks.Add(batch, 1, shape, "https://example.com");

        var listed = _hyperlinks.ListForShape(batch, 1, shape);

        // Shape.Hyperlinks is 0-based, unlike Pages, Shapes and Masters. Reading it as 1-based
        // throws COMException "Invalid parameter" on a shape with exactly one hyperlink, so this
        // asserts the whole enumeration path rather than just the count.
        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.Single(listed.Hyperlinks);
        Assert.Equal(0, listed.Hyperlinks[0].RowIndex);
        Assert.Equal("https://example.com", listed.Hyperlinks[0].Address);
    }

    [Fact]
    public void AShapeCanCarrySeveralHyperlinks()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        _hyperlinks.Add(batch, 1, shape, "https://one.example", hyperlinkName: "First");
        _hyperlinks.Add(batch, 1, shape, "https://two.example", hyperlinkName: "Second");

        var listed = _hyperlinks.ListForShape(batch, 1, shape);

        // The central difference from PowerPoint, where a shape has at most one.
        Assert.Equal(2, listed.Hyperlinks.Count);
        Assert.Contains(listed.Hyperlinks, h => h.Name == "First");
        Assert.Contains(listed.Hyperlinks, h => h.Name == "Second");
    }

    [Fact]
    public void Add_WithoutAName_LetsVisioAssignARowName()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        var added = _hyperlinks.Add(batch, 1, shape, "https://example.com");

        Assert.False(string.IsNullOrWhiteSpace(added.Hyperlink!.Name));
        Assert.True(_hyperlinks.Read(batch, 1, shape, added.Hyperlink.Name).Success);
    }

    [Fact]
    public void Add_InternalLink_NeedsOnlySubAddress()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        var added = _hyperlinks.Add(batch, 1, shape, subAddress: "Page-1");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.Equal("Page-1", added.Hyperlink!.SubAddress);
        Assert.Equal(string.Empty, added.Hyperlink.Address);
    }

    [Fact]
    public void Add_WithNoTargetAtAll_IsRejected()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        var ex = Assert.Throws<ArgumentException>(() => _hyperlinks.Add(batch, 1, shape));

        // Visio accepts a row with neither target and does nothing with it, so it would otherwise
        // report a working link.
        Assert.Contains("needs a target", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_ChangesOnlyWhatIsGiven()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);
        _hyperlinks.Add(batch, 1, shape, "https://old.example", description: "Keep me", hyperlinkName: "Link");

        var updated = _hyperlinks.Update(batch, 1, shape, "Link", address: "https://new.example");

        Assert.Equal("https://new.example", updated.Hyperlink!.Address);
        Assert.Equal("Keep me", updated.Hyperlink.Description);
    }

    [Fact]
    public void Update_WithNothingToChange_IsRejected()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);
        _hyperlinks.Add(batch, 1, shape, "https://example.com", hyperlinkName: "Link");

        var ex = Assert.Throws<ArgumentException>(() => _hyperlinks.Update(batch, 1, shape, "Link"));

        Assert.Contains("at least one", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Delete_RemovesOnlyTheNamedHyperlink()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);
        _hyperlinks.Add(batch, 1, shape, "https://one.example", hyperlinkName: "First");
        _hyperlinks.Add(batch, 1, shape, "https://two.example", hyperlinkName: "Second");

        var deleted = _hyperlinks.Delete(batch, 1, shape, "First");

        Assert.True(deleted.Success, deleted.ErrorMessage);
        var remaining = _hyperlinks.ListForShape(batch, 1, shape);
        Assert.Single(remaining.Hyperlinks);
        Assert.Equal("Second", remaining.Hyperlinks[0].Name);
    }

    [Fact]
    public void List_SpansPagesAndShapes()
    {
        using var batch = CreateDocument();
        var first = AddRect(batch);
        var second = AddRect(batch, 4.0f);
        _hyperlinks.Add(batch, 1, first, "https://one.example");
        _hyperlinks.Add(batch, 1, second, "https://two.example");

        var listed = _hyperlinks.List(batch);

        Assert.Equal(2, listed.Hyperlinks.Count);
        Assert.All(listed.Hyperlinks, h => Assert.Equal(1, h.PageIndex));
        Assert.Contains(listed.Hyperlinks, h => h.ShapeName == first);
        Assert.Contains(listed.Hyperlinks, h => h.ShapeName == second);
    }

    [Fact]
    public void List_OnADocumentWithNoLinks_SucceedsAndIsEmpty()
    {
        using var batch = CreateDocument();
        AddRect(batch);

        var listed = _hyperlinks.List(batch);

        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.Empty(listed.Hyperlinks);
    }

    [Fact]
    public void UnknownHyperlink_OnAShapeWithNone_SaysHowToAddOne()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);

        var ex = Assert.Throws<ArgumentException>(() => _hyperlinks.Read(batch, 1, shape, "Row_1"));

        // Visio's own message is "Invalid parameter", which does not identify the hyperlink or the
        // shape, let alone say the shape has none.
        Assert.Contains("no hyperlinks", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hyperlink(add)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownHyperlink_WhenOthersExist_ListsThem()
    {
        using var batch = CreateDocument();
        var shape = AddRect(batch);
        _hyperlinks.Add(batch, 1, shape, "https://example.com", hyperlinkName: "Docs");

        var ex = Assert.Throws<ArgumentException>(() => _hyperlinks.Read(batch, 1, shape, "Missing"));

        Assert.Contains("Docs", ex.Message, StringComparison.Ordinal);
    }

    private string AddRect(IVisioBatch batch, float left = 1.0f)
    {
        var added = _shapes.AddShape(batch, 1, 1, left, 1.0f, 1.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        var listed = _shapes.List(batch, 1);
        return listed.Shapes[^1].Name;
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"HyperlinkTests_{Guid.NewGuid():N}.vsdx");
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
