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
public sealed class VisioShapeMergeToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeMerge_Union_ReturnsJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioShapeMergeToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var firstShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var secondShape = AddBasicShape(sessionId, 144f, 108f, 144f, 72f);

            var beforeJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
            Assert.Equal(2, beforeJson.GetProperty("shapes").EnumerateArray().Count());

            var mergeResult = InvokeShape(
                ShapeAction.MergeShapes,
                sessionId,
                shape_names: $"{firstShape},{secondShape}",
                merge_type: 1);
            output.WriteLine(mergeResult);

            var mergeJson = JsonDocument.Parse(mergeResult).RootElement;
            Assert.True(mergeJson.GetProperty("success").GetBoolean());

            var afterResult = InvokeShape(ShapeAction.List, sessionId);
            output.WriteLine(afterResult);

            var afterJson = JsonDocument.Parse(afterResult).RootElement;
            var mergedShape = Assert.Single(afterJson.GetProperty("shapes").EnumerateArray());
            Assert.True(mergedShape.GetProperty("width").GetSingle() > 144f);
            Assert.True(mergedShape.GetProperty("height").GetSingle() > 72f);
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
        string? shape_name = null,
        string? shape_names = null,
        int merge_type = 0)
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
            shape_names: shape_names,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: merge_type,
            target_shape_name: null,
            property_name: null,
            property_value: null,
            connector_end: null);
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
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            VisioFileTool.VisioFile(
                VisioFileAction.Close,
                path: null,
                session_id: sessionId,
                page_name: null,
                page_index: null,
                save: false,
                show: false,
                timeout_seconds: 300);
        }
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
