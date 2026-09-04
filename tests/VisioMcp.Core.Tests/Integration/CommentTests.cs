using System.Text.RegularExpressions;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Comment;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Reviewer comments ported to Visio <c>Page.Comments</c> and <c>Shape.Comments</c> (#62).
///
/// Integration tests against real Visio (Rule 30). These comments are separate from the
/// ShapeSheet <c>Comment</c> cell, which is Visio's accessibility/alt-text field.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Comment")]
public sealed class CommentTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly CommentCommands _comments = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void List_ReturnsExistingPageCommentAddedThroughVisioNativeApi()
    {
        using var batch = CreateDocument();
        AddNativePageComment(batch, "native page comment");

        var result = _comments.List(batch, 1);

        Assert.True(result.Success, result.ErrorMessage);
        var comment = Assert.Single(result.Comments);
        Assert.Equal(1, comment.PageIndex);
        Assert.Equal(1, comment.CommentIndex);
        Assert.Equal("native page comment", comment.Text);
        Assert.False(string.IsNullOrWhiteSpace(comment.AuthorName));
        Assert.False(string.IsNullOrWhiteSpace(comment.AuthorInitials));
        AssertIsoDate(comment.CreateDate);
        AssertIsoDate(comment.EditDate);
        Assert.Equal("Page", comment.AssociatedObjectType);
        Assert.Equal("Page-1", comment.AssociatedPageName);
        Assert.Null(comment.AssociatedShapeName);
    }

    [Fact]
    public void Add_PageCommentRoundTrips()
    {
        using var batch = CreateDocument();

        var add = _comments.Add(batch, 1, "page level review");
        var list = _comments.List(batch, 1);

        Assert.True(add.Success, add.ErrorMessage);
        Assert.Null(add.ErrorMessage);
        var comment = Assert.Single(list.Comments);
        Assert.Equal("page level review", comment.Text);
        Assert.Equal("Page", comment.AssociatedObjectType);
        Assert.Equal("Page-1", comment.AssociatedPageName);
    }

    [Fact]
    public void Add_ShapeCommentCanBeFilteredAndReportsAssociation()
    {
        using var batch = CreateDocument();
        CreateNamedShape(batch, "ReviewTarget");
        _comments.Add(batch, 1, "page comment");

        var add = _comments.Add(batch, 1, "shape comment", "ReviewTarget");
        var all = _comments.List(batch, 1);
        var filtered = _comments.List(batch, 1, "ReviewTarget");

        Assert.True(add.Success, add.ErrorMessage);
        Assert.Equal(2, all.Comments.Count);
        var comment = Assert.Single(filtered.Comments);
        Assert.Equal("shape comment", comment.Text);
        Assert.Equal("Shape", comment.AssociatedObjectType);
        Assert.Equal("ReviewTarget", comment.AssociatedShapeName);
        Assert.Null(comment.AssociatedPageName);
    }

    [Fact]
    public void Delete_RemovesOneBasedPageCommentIndex()
    {
        using var batch = CreateDocument();
        _comments.Add(batch, 1, "first");
        _comments.Add(batch, 1, "second");

        var delete = _comments.Delete(batch, 1, 1);
        var list = _comments.List(batch, 1);

        Assert.True(delete.Success, delete.ErrorMessage);
        var comment = Assert.Single(list.Comments);
        Assert.Equal("second", comment.Text);
        Assert.Equal(1, comment.CommentIndex);
    }

    [Fact]
    public void Clear_DeletesPageAndShapeCommentsThroughDeleteAll()
    {
        using var batch = CreateDocument();
        CreateNamedShape(batch, "ClearTarget");
        _comments.Add(batch, 1, "page comment");
        _comments.Add(batch, 1, "shape comment", "ClearTarget");

        var clear = _comments.Clear(batch, 1);
        var list = _comments.List(batch, 1);

        Assert.True(clear.Success, clear.ErrorMessage);
        Assert.Empty(list.Comments);
    }

    [Fact]
    public void ShapeComments_DoNotOverwriteShapeSheetCommentAltTextCell()
    {
        using var batch = CreateDocument();
        CreateNamedShape(batch, "AccessibleShape");
        _shapes.SetAltText(batch, 1, "AccessibleShape", "Existing accessibility text");

        _comments.Add(batch, 1, "reviewer note", "AccessibleShape");

        Assert.Equal("\"Existing accessibility text\"", ReadShapeCommentCellFormula(batch, "AccessibleShape"));
        Assert.Equal("reviewer note", Assert.Single(_comments.List(batch, 1, "AccessibleShape").Comments).Text);
    }

    [Fact]
    public void Comments_SurviveSaveAndReopen()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        using (var batch = VisioSession.BeginBatch(path))
        {
            CreateNamedShape(batch, "PersistedShape");
            _comments.Add(batch, 1, "persisted page comment");
            _comments.Add(batch, 1, "persisted shape comment", "PersistedShape");
            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);
        var list = _comments.List(reopened, 1);

        Assert.Equal(["persisted page comment", "persisted shape comment"], list.Comments.Select(c => c.Text).ToArray());
        Assert.Equal("PersistedShape", list.Comments.Single(c => c.Text == "persisted shape comment").AssociatedShapeName);
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private static void AddNativePageComment(IVisioBatch batch, string text)
    {
        batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? comments = null;
            dynamic? comment = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                comments = page.Comments;
                comment = comments.Add(text);
                return 0;
            }
            finally
            {
                if (comment != null) ComUtilities.Release(ref comment!);
                if (comments != null) ComUtilities.Release(ref comments!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static void CreateNamedShape(IVisioBatch batch, string shapeName)
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
                shape = page.DrawRectangle(1, 1, 2, 2);
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

    private static string ReadShapeCommentCellFormula(IVisioBatch batch, string shapeName)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shapes = null;
            dynamic? shape = null;
            dynamic? cell = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shapes = page.Shapes;
                shape = shapes.Item(shapeName);
                cell = shape.CellsU["Comment"];
                return cell.FormulaU?.ToString() ?? string.Empty;
            }
            finally
            {
                if (cell != null) ComUtilities.Release(ref cell!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (shapes != null) ComUtilities.Release(ref shapes!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static void AssertIsoDate(string value)
    {
        Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}T", RegexOptions.CultureInvariant), value);
    }
}
