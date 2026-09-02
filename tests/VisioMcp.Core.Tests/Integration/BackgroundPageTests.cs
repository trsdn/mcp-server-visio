using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Page;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Background pages (#36c, and the background half of #67).
///
/// A Visio background page is a normal page marked as a background and then attached to other
/// pages, which show it behind their own content. Shared furniture — a title block, a logo — drawn
/// once. There is no PowerPoint analogue, which is why the suppressed <c>background</c> domain
/// could not simply be ported.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Page")]
public sealed class BackgroundPageTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly PageCommands _pages = new();

    [Fact]
    public void ANewPage_IsNotABackground_AndHasNoBackdrop()
    {
        using var batch = CreateDocument();

        var read = _pages.ReadBackground(batch, 1);

        Assert.True(read.Success, read.ErrorMessage);
        Assert.False(read.IsBackground);
        Assert.Null(read.BackPageName);
    }

    [Fact]
    public void SetBackground_MarksThePage()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");

        var set = _pages.SetBackground(batch, frame, true);

        Assert.True(set.Success, set.ErrorMessage);
        Assert.True(set.IsBackground);
        Assert.True(_pages.ReadBackground(batch, set.PageIndex).IsBackground);
    }

    [Fact]
    public void SetBackground_False_TurnsItBackIntoANormalPage()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");
        var marked = _pages.SetBackground(batch, frame, true);

        var cleared = _pages.SetBackground(batch, marked.PageIndex, false);

        Assert.False(cleared.IsBackground);
    }

    [Fact]
    public void SetBackPage_AttachesABackgroundToAPage()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");
        _pages.SetBackground(batch, frame, true);

        var attached = _pages.SetBackPage(batch, 1, "Frame");

        Assert.True(attached.Success, attached.ErrorMessage);
        Assert.Equal("Frame", attached.BackPageName);

        // The page showing the background is not itself a background.
        Assert.False(attached.IsBackground);
    }

    [Fact]
    public void List_ReportsBothFlags()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");
        _pages.SetBackground(batch, frame, true);
        _pages.SetBackPage(batch, 1, "Frame");

        var listed = _pages.List(batch);

        var content = listed.Pages.Single(p => p.PageIndex == 1);
        var background = listed.Pages.Single(p => p.Name == "Frame");

        Assert.Equal("Frame", content.BackPageName);
        Assert.False(content.IsBackground);
        Assert.True(background.IsBackground);
        Assert.Null(background.BackPageName);
    }

    [Fact]
    public void SetBackPage_ToAPageThatIsNotABackground_SaysWhatToDoFirst()
    {
        using var batch = CreateDocument();
        AddPage(batch, "Plain");

        var ex = Assert.Throws<ArgumentException>(() => _pages.SetBackPage(batch, 1, "Plain"));

        // Visio's own message is "Inappropriate target object for this action", which names
        // neither the object nor the requirement.
        Assert.Contains("not a background page", ex.Message, StringComparison.Ordinal);
        Assert.Contains("set-background", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetBackPage_ToAPageThatDoesNotExist_ListsTheOnesThatDo()
    {
        using var batch = CreateDocument();
        AddPage(batch, "Frame");

        var ex = Assert.Throws<ArgumentException>(() => _pages.SetBackPage(batch, 1, "NoSuchPage"));

        Assert.Contains("Frame", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetBackPage_ToItself_IsRejected()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");
        var marked = _pages.SetBackground(batch, frame, true);

        var ex = Assert.Throws<ArgumentException>(() => _pages.SetBackPage(batch, marked.PageIndex, "Frame"));

        Assert.Contains("cannot show itself", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearBackPage_DetachesButKeepsTheBackgroundPage()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");
        _pages.SetBackground(batch, frame, true);
        _pages.SetBackPage(batch, 1, "Frame");

        var cleared = _pages.ClearBackPage(batch, 1);

        // Assigning null here throws COMException "Invalid parameter"; Visio detaches on an empty
        // string. This asserts the detach actually happened rather than that the call returned.
        Assert.True(cleared.Success, cleared.ErrorMessage);
        Assert.Null(cleared.BackPageName);
        Assert.Null(_pages.ReadBackground(batch, 1).BackPageName);

        // The background page itself survives.
        Assert.Contains(_pages.List(batch).Pages, p => p.Name == "Frame");
    }

    [Fact]
    public void ClearBackPage_WhenNoneAttached_IsHarmless()
    {
        using var batch = CreateDocument();

        var cleared = _pages.ClearBackPage(batch, 1);

        Assert.True(cleared.Success, cleared.ErrorMessage);
        Assert.Null(cleared.BackPageName);
    }

    [Fact]
    public void ABackgroundPage_CanItselfShowAnother()
    {
        using var batch = CreateDocument();
        AddPage(batch, "Outer");
        AddPage(batch, "Inner");

        // Indices are re-resolved after each flag change: see SetBackground_ReturnsTheNewIndex.
        _pages.SetBackground(batch, IndexOf(batch, "Outer"), true);
        _pages.SetBackground(batch, IndexOf(batch, "Inner"), true);

        var chained = _pages.SetBackPage(batch, IndexOf(batch, "Outer"), "Inner");

        // Verified against a live instance: Visio permits chaining backgrounds.
        Assert.True(chained.Success, chained.ErrorMessage);
        Assert.True(chained.IsBackground);
        Assert.Equal("Inner", chained.BackPageName);
    }

    [Fact]
    public void SetBackground_ReturnsTheNewIndex_BecauseVisioReordersThePages()
    {
        using var batch = CreateDocument();
        AddPage(batch, "Frame");
        AddPage(batch, "Later");
        int frameBefore = IndexOf(batch, "Frame");

        var set = _pages.SetBackground(batch, frameBefore, true);

        // Visio keeps background pages after normal ones, so marking one moves it. Returning the
        // index that was passed in would send the caller's next call to a different page.
        Assert.Equal(IndexOf(batch, "Frame"), set.PageIndex);
        Assert.NotEqual(frameBefore, set.PageIndex);
        Assert.Contains("page_index changed", set.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetBackground_ExplainsTheSecondStep()
    {
        using var batch = CreateDocument();
        int frame = AddPage(batch, "Frame");

        var set = _pages.SetBackground(batch, frame, true);

        // Marking a page as a background does nothing visible on its own, so the message has to
        // name the step that makes it take effect.
        Assert.Contains("set-back-page", set.Message, StringComparison.Ordinal);
    }

    private int AddPage(IVisioBatch batch, string name)
    {
        var created = _pages.Create(batch, 0, name);
        Assert.True(created.Success, created.ErrorMessage);

        return IndexOf(batch, name);
    }

    /// <summary>
    /// Current index of a page by name. Marking a page as a background reorders the collection,
    /// so an index must never be held across a set-background call.
    /// </summary>
    private int IndexOf(IVisioBatch batch, string name) =>
        _pages.List(batch).Pages.Single(p => p.Name == name).PageIndex;

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"BackgroundPageTests_{Guid.NewGuid():N}.vsdx");
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
