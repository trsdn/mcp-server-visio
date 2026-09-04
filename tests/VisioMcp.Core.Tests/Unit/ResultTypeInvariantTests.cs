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
    public void SlideListResult_DefaultState_EmptySlidesList()
    {
        var result = new SlideListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Slides);
        Assert.Empty(result.Slides);
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
    public void SlideInfo_DefaultValues_AreReasonable()
    {
        var info = new SlideInfo();
        Assert.Equal(0, info.SlideIndex);
        Assert.Equal(0, info.SlideNumber);
        Assert.Equal(string.Empty, info.SlideId);
        Assert.Equal(string.Empty, info.LayoutName);
        Assert.Equal(string.Empty, info.MasterName);
        Assert.Equal(0, info.ShapeCount);
        Assert.False(info.HasNotes);
        Assert.False(info.HasAnimations);
        Assert.Null(info.Name);
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
    public void ChartInfoResult_DefaultValues()
    {
        var result = new ChartInfoResult();
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.ShapeName);
        Assert.Equal(string.Empty, result.ChartTypeName);
        Assert.Null(result.Title);
        Assert.False(result.HasLegend);
        Assert.Equal(0, result.SeriesCount);
    }

    [Fact]
    public void VbaModuleListResult_DefaultValues()
    {
        var result = new VbaModuleListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Modules);
        Assert.Empty(result.Modules);
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
    public void SectionListResult_DefaultValues()
    {
        var result = new SectionListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Sections);
        Assert.Empty(result.Sections);
    }

    [Fact]
    public void AnimationListResult_DefaultValues()
    {
        var result = new AnimationListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Animations);
        Assert.Empty(result.Animations);
    }

    [Fact]
    public void HyperlinkListResult_DefaultValues()
    {
        var result = new HyperlinkListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Hyperlinks);
        Assert.Empty(result.Hyperlinks);
    }

    [Fact]
    public void MasterListResult_DefaultValues()
    {
        var result = new MasterListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Masters);
        Assert.Empty(result.Masters);
    }

    [Fact]
    public void DesignListResult_DefaultValues()
    {
        var result = new DesignListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Designs);
        Assert.Empty(result.Designs);
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
        Assert.Equal(0, result.SlideCount);
    }

    [Fact]
    public void FileValidationInfo_SlideCountAlias_MapsToPageCount()
    {
        var result = new FileValidationInfo
        {
            SlideCount = 4
        };

        Assert.Equal(4, result.PageCount);
        Assert.Equal(4, result.SlideCount);
    }

    [Fact]
    public void CommentListResult_DefaultValues()
    {
        var result = new CommentListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Comments);
        Assert.Empty(result.Comments);
    }

    [Fact]
    public void PlaceholderListResult_DefaultValues()
    {
        var result = new PlaceholderListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Placeholders);
        Assert.Empty(result.Placeholders);
    }

    [Fact]
    public void BackgroundResult_DefaultValues()
    {
        var result = new BackgroundResult();
        Assert.False(result.Success);
        Assert.False(result.FollowMasterBackground);
        Assert.Equal(string.Empty, result.FillType);
    }

    [Fact]
    public void HeaderFooterResult_DefaultValues()
    {
        var result = new HeaderFooterResult();
        Assert.False(result.Success);
        Assert.Null(result.HeaderLeft);
        Assert.Null(result.HeaderCenter);
        Assert.Null(result.HeaderRight);
        Assert.Null(result.FooterLeft);
        Assert.Null(result.FooterCenter);
        Assert.Null(result.FooterRight);
        Assert.Equal(0d, result.HeaderMargin);
        Assert.Equal(0d, result.FooterMargin);
    }

    [Fact]
    public void SmartArtInfoResult_DefaultValues()
    {
        var result = new SmartArtInfoResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Nodes);
        Assert.Empty(result.Nodes);
        Assert.Equal(string.Empty, result.LayoutName);
    }

    [Fact]
    public void CustomShowListResult_DefaultValues()
    {
        var result = new CustomShowListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Shows);
        Assert.Empty(result.Shows);
    }

    [Fact]
    public void PageSetupResult_DefaultValues()
    {
        var result = new PageSetupResult();
        Assert.False(result.Success);
        Assert.Equal(0f, result.SlideWidth);
        Assert.Equal(0f, result.SlideHeight);
    }

    [Fact]
    public void ColorSchemeListResult_DefaultValues()
    {
        var result = new ColorSchemeListResult();
        Assert.False(result.Success);
        Assert.NotNull(result.ColorSchemes);
        Assert.Empty(result.ColorSchemes);
    }

    [Fact]
    public void AccessibilityAuditResult_DefaultValues()
    {
        var result = new AccessibilityAuditResult();
        Assert.False(result.Success);
        Assert.Equal(0, result.TotalSlides);
        Assert.Equal(0, result.IssueCount);
        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ReadingOrderResult_DefaultValues()
    {
        var result = new ReadingOrderResult();
        Assert.False(result.Success);
        Assert.NotNull(result.Shapes);
        Assert.Empty(result.Shapes);
    }
}
