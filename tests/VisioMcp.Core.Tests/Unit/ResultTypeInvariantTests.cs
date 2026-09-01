using VisioMcp.Core.Models;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Validates invariants on result types to prevent Rule 1 violations
/// (Success=true with ErrorMessage set).
/// </summary>
public class ResultTypeInvariantTests
{
    [Fact]
    public void OperationResult_DefaultState_SuccessIsFalse()
    {
        var result = new OperationResult();
        Assert.False(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OperationResult_SuccessTrue_ErrorMessageMustBeNull()
    {
        var result = new OperationResult { Success = true };
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ShapeListResult_DefaultState_EmptyShapesList()
    {
        var result = new ShapeListResult();
        Assert.False(result.Success);
        Assert.Equal(0, result.PageIndex);
        Assert.NotNull(result.Shapes);
        Assert.Empty(result.Shapes);
    }

    [Fact]
    public void TextResult_DefaultState_EmptyText()
    {
        var result = new TextResult();
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Text);
        Assert.NotNull(result.Paragraphs);
        Assert.Empty(result.Paragraphs);
    }

    [Fact]
    public void ShapeInfo_DefaultValues_AreReasonable()
    {
        var info = new ShapeInfo();
        Assert.Equal(0, info.ShapeId);
        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.ShapeType);
        Assert.Equal(0f, info.Left);
        Assert.Equal(0f, info.Top);
        Assert.Equal(0f, info.Width);
        Assert.Equal(0f, info.Height);
        Assert.False(info.HasTextFrame);
        Assert.False(info.HasTable);
        Assert.False(info.HasChart);
        Assert.False(info.IsGroup);
        Assert.False(info.IsPlaceholder);
        Assert.Null(info.Text);
        Assert.Null(info.AlternativeText);
        Assert.Null(info.PlaceholderType);
        Assert.Null(info.GroupItems);
    }

    [Fact]
    public void LayerListResult_DefaultValues_AreReasonable()
    {
        var result = new LayerListResult();
        Assert.False(result.Success);
        Assert.Equal(0, result.PageIndex);
        Assert.NotNull(result.Layers);
        Assert.Empty(result.Layers);
    }

    [Fact]
    public void LayerInfo_DefaultValues_AreReasonable()
    {
        var info = new LayerInfo();
        Assert.Equal(0, info.PageIndex);
        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.NameU);
        Assert.Equal(0, info.ColorIndex);
        Assert.False(info.Visible);
        Assert.False(info.Printable);
        Assert.False(info.Locked);
        Assert.False(info.Snap);
        Assert.False(info.Glue);
        Assert.Equal(0, info.MemberCount);
        Assert.Null(info.ShapeNames);
    }

    [Fact]
    public void RenameResult_DefaultValues()
    {
        var result = new RenameResult();
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.ObjectType);
        Assert.Equal(string.Empty, result.OldName);
        Assert.Equal(string.Empty, result.NewName);
    }

    [Fact]
    public void ExportResult_DefaultValues()
    {
        var result = new ExportResult();
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.OutputPath);
        Assert.Equal(string.Empty, result.Format);
    }

    [Fact]
    public void DocumentPropertyResult_AllPropertiesNullByDefault()
    {
        var result = new DocumentPropertyResult();
        Assert.False(result.Success);
        Assert.Null(result.Title);
        Assert.Null(result.Subject);
        Assert.Null(result.Author);
        Assert.Null(result.Keywords);
        Assert.Null(result.Comments);
        Assert.Null(result.Company);
        Assert.Null(result.Category);
    }

    [Fact]
    public void FileValidationInfo_DefaultValues()
    {
        var result = new FileValidationInfo();
        Assert.False(result.Success);
        Assert.False(result.Exists);
        Assert.Equal(string.Empty, result.FileName);
        Assert.Equal(0, result.FileSizeBytes);
        Assert.False(result.IsReadOnly);
        Assert.False(result.IsMacroEnabled);
        Assert.Equal(0, result.PageCount);
    }

}
