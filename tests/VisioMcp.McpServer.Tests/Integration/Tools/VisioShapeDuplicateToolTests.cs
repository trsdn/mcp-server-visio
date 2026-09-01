using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Shape")]
public sealed class VisioShapeDuplicateToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeDuplicate_CreatesSecondShapeWithSameGeometry()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioShapeDuplicateToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var originalShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var originalShapeInfo = ReadShape(sessionId, originalShape);

            var duplicateResult = InvokeShape(ShapeAction.Duplicate, sessionId, shape_name: originalShape);
            output.WriteLine(duplicateResult);

            var duplicateJson = JsonDocument.Parse(duplicateResult).RootElement;
            Assert.True(duplicateJson.GetProperty("success").GetBoolean());

            var listJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
            var shapeNames = listJson.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();

            Assert.Equal(2, shapeNames.Count);
            Assert.Contains(originalShape, shapeNames);

            var duplicateShape = shapeNames.Single(name => !string.Equals(name, originalShape, StringComparison.OrdinalIgnoreCase))!;
            var duplicateShapeInfo = ReadShape(sessionId, duplicateShape);

            Assert.Equal(originalShapeInfo.GetProperty("width").GetSingle(), duplicateShapeInfo.GetProperty("width").GetSingle());
            Assert.Equal(originalShapeInfo.GetProperty("height").GetSingle(), duplicateShapeInfo.GetProperty("height").GetSingle());
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    private static string InvokeShape(
        ShapeAction action,
        string sessionId,
        string? shape_name = null)
    {
        return VisioShapeTool.VisioShape(
            action: action,
            session_id: sessionId,
            page_index: 1,
            shape_name: shape_name,
            left: null,
            top: null,
            width: null,
            height: null,
            text: null,
            auto_shape_type: 0,
            z_order_cmd: 0,
            shape_names: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            target_shape_name: null,
            property_name: null,
            property_value: null,
            connector_end: null);
    }

    private static JsonElement ReadShape(string sessionId, string shapeName)
    {
        var readJson = JsonDocument.Parse(InvokeShape(ShapeAction.Read, sessionId, shape_name: shapeName)).RootElement;
        Assert.True(readJson.GetProperty("success").GetBoolean());
        return readJson.GetProperty("shape").Clone();
    }

    private static string CreateSession(string path)
    {
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Create,
            path: path,
            session_id: null,
            page_name: null,
            page_index: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        var json = JsonDocument.Parse(result).RootElement;
        Assert.True(json.GetProperty("success").GetBoolean());
        return json.GetProperty("session_id").GetString()!;
    }

    private static string AddBasicShape(string sessionId, float left, float top, float width, float height)
    {
        var beforeJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
        var beforeNames = beforeJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createResult = VisioShapeTool.VisioShape(
            action: ShapeAction.AddShape,
            session_id: sessionId,
            page_index: 1,
            shape_name: null,
            left: left,
            top: top,
            width: width,
            height: height,
            text: null,
            auto_shape_type: 1,
            z_order_cmd: 0,
            shape_names: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            target_shape_name: null,
            property_name: null,
            property_value: null,
            connector_end: null);
        var createJson = JsonDocument.Parse(createResult).RootElement;
        Assert.True(createJson.GetProperty("success").GetBoolean());

        var afterJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static void CloseSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var closeResult = VisioFileTool.VisioFile(
            action: VisioFileAction.Close,
            path: null,
            session_id: sessionId,
            page_name: null,
            page_index: null,
            save: false,
            show: false,
            timeout_seconds: 300);
        var closeJson = JsonDocument.Parse(closeResult).RootElement;
        Assert.True(closeJson.GetProperty("success").GetBoolean());
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
