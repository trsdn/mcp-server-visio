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
public sealed class VisioShapealignToolTests(ITestOutputHelper output)
{
    [Fact]
    public void Shapealign_AlignAndDistribute_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioShapealignToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var first = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var second = AddBasicShape(sessionId, 252f, 144f, 144f, 72f);
            var third = AddBasicShape(sessionId, 468f, 108f, 144f, 72f);

            var alignResult = VisioShapealignTool.VisioShapealign(
                action: ShapealignAction.Align,
                session_id: sessionId,
                page_index: 1,
                shape_names: $"{first},{second}",
                align_type: 0,
                distribute_type: 0);
            output.WriteLine(alignResult);

            var alignJson = JsonDocument.Parse(alignResult).RootElement;
            Assert.True(alignJson.GetProperty("success").GetBoolean());

            var firstAfterAlign = ReadShape(sessionId, first);
            var secondAfterAlign = ReadShape(sessionId, second);
            Assert.Equal(firstAfterAlign.GetProperty("left").GetSingle(), secondAfterAlign.GetProperty("left").GetSingle(), 3);

            var distributeResult = VisioShapealignTool.VisioShapealign(
                action: ShapealignAction.Distribute,
                session_id: sessionId,
                page_index: 1,
                shape_names: $"{first},{second},{third}",
                align_type: 0,
                distribute_type: 0);
            output.WriteLine(distributeResult);

            var distributeJson = JsonDocument.Parse(distributeResult).RootElement;
            Assert.True(distributeJson.GetProperty("success").GetBoolean());

            var lefts = new[]
            {
                ReadShape(sessionId, first).GetProperty("left").GetSingle(),
                ReadShape(sessionId, second).GetProperty("left").GetSingle(),
                ReadShape(sessionId, third).GetProperty("left").GetSingle()
            }.OrderBy(value => value).ToArray();

            var firstGap = lefts[1] - lefts[0];
            var secondGap = lefts[2] - lefts[1];
            Assert.Equal(firstGap, secondGap, 3);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
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
        var beforeJson = JsonDocument.Parse(ReadShapeList(sessionId)).RootElement;
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

        var afterJson = JsonDocument.Parse(ReadShapeList(sessionId)).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static string ReadShapeList(string sessionId)
    {
        return VisioShapeTool.VisioShape(
            action: ShapeAction.List,
            session_id: sessionId,
            page_index: 1,
            shape_name: null,
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
        var result = VisioShapeTool.VisioShape(
            action: ShapeAction.Read,
            session_id: sessionId,
            page_index: 1,
            shape_name: shapeName,
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

        return JsonDocument.Parse(result).RootElement.GetProperty("shape").Clone();
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
