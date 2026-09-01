using System.Text.Json;
using VisioMcp.Core.Models;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Validates JSON serialization behavior of result types,
/// ensuring null properties are omitted and camelCase naming works correctly.
/// </summary>
public class ResultTypeSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    [Fact]
    public void OperationResult_Success_OmitsNullFields()
    {
        var result = new OperationResult { Success = true, Action = "create", Message = "Done" };
        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"success\":true", json);
        Assert.Contains("\"action\":\"create\"", json);
        Assert.DoesNotContain("errorMessage", json);
        Assert.DoesNotContain("filePath", json);
    }

    [Fact]
    public void OperationResult_Failure_IncludesErrorMessage()
    {
        var result = new OperationResult { Success = false, ErrorMessage = "Not found" };
        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"success\":false", json);
        Assert.Contains("\"errorMessage\":\"Not found\"", json);
    }

    [Fact]
    public void ShapeInfo_NullOptionalFields_AreOmitted()
    {
        var info = new ShapeInfo
        {
            ShapeId = 1,
            Name = "Rectangle 1",
            ShapeType = "AutoShape",
            Width = 100f,
            Height = 50f
        };
        var json = JsonSerializer.Serialize(info, JsonOptions);

        Assert.Contains("\"name\":\"Rectangle 1\"", json);
        Assert.DoesNotContain("\"text\":", json);
        Assert.DoesNotContain("\"alternativeText\":", json);
        Assert.DoesNotContain("\"placeholderType\":", json);
        Assert.DoesNotContain("\"groupItems\":", json);
    }

    [Fact]
    public void TextResult_WithParagraphs_SerializesNestedStructure()
    {
        var result = new TextResult
        {
            Success = true,
            ShapeId = 1,
            ShapeName = "Title 1",
            Text = "Hello World",
            Paragraphs =
            [
                new TextParagraphInfo
                {
                    Index = 0,
                    Text = "Hello World",
                    Runs =
                    [
                        new TextRunInfo { Text = "Hello ", Bold = true, FontSize = 24f },
                        new TextRunInfo { Text = "World", Italic = true }
                    ]
                }
            ]
        };
        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"bold\":true", json);
        Assert.Contains("\"fontSize\":24", json);
        Assert.Contains("\"italic\":true", json);
    }

    [Fact]
    public void OperationResult_RoundTrip_PreservesAllFields()
    {
        var original = new OperationResult
        {
            Success = true,
            Action = "delete",
            Message = "Deleted page 3",
            FilePath = @"C:\test\pres.vsdx"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OperationResult>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Success, deserialized.Success);
        Assert.Equal(original.Action, deserialized.Action);
        Assert.Equal(original.Message, deserialized.Message);
        Assert.Equal(original.FilePath, deserialized.FilePath);
    }

    [Fact]
    public void DocumentPropertyResult_AllNulls_MinimalJson()
    {
        var result = new DocumentPropertyResult { Success = true };
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Should only have success, no null property fields
        Assert.Contains("\"success\":true", json);
        Assert.DoesNotContain("\"title\":", json);
        Assert.DoesNotContain("\"author\":", json);
        Assert.DoesNotContain("\"subject\":", json);
    }

    [Fact]
    public void ShapeListResult_SerializesPageIndex_NotLegacySlideIndex()
    {
        var result = new ShapeListResult
        {
            Success = true,
            PageIndex = 2,
            Shapes = []
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"pageIndex\":2", json);
        Assert.DoesNotContain("\"slideIndex\":", json);
    }

    [Fact]
    public void LayerDetailResult_WithShapeNames_SerializesCorrectly()
    {
        var result = new LayerDetailResult
        {
            Success = true,
            PageIndex = 1,
            Layer = new LayerInfo
            {
                PageIndex = 1,
                Name = "Workflow",
                NameU = "Workflow",
                ColorIndex = 4,
                Visible = true,
                Printable = false,
                Locked = true,
                MemberCount = 2,
                ShapeNames = ["Rectangle.1", "Rectangle.2"]
            }
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"pageIndex\":1", json);
        Assert.Contains("\"name\":\"Workflow\"", json);
        Assert.Contains("\"colorIndex\":4", json);
        Assert.Contains("\"shapeNames\":[\"Rectangle.1\",\"Rectangle.2\"]", json);
    }

    [Fact]
    public void FileValidationInfo_SerializesPageCount_NotLegacySlideCount()
    {
        var result = new FileValidationInfo
        {
            Success = true,
            Exists = true,
            FileName = "diagram.vsdx",
            PageCount = 3
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);

        Assert.Contains("\"pageCount\":3", json);
        Assert.DoesNotContain("\"slideCount\":", json);
    }

}
