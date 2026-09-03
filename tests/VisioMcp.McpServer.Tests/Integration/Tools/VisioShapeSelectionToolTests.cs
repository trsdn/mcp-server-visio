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
public sealed class VisioShapeSelectionToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeSelection_SelectAddRemoveClear_ReturnsJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioShapeSelectionToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var firstShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var secondShape = AddBasicShape(sessionId, 252f, 72f, 144f, 72f);

            var selectResult = InvokeShape(
                ShapeAction.SelectShapes,
                sessionId,
                shape_names: firstShape);
            output.WriteLine(selectResult);
            Assert.True(JsonDocument.Parse(selectResult).RootElement.GetProperty("success").GetBoolean());

            var selectedJson = JsonDocument.Parse(InvokeShape(ShapeAction.ListSelection, sessionId)).RootElement;
            var selectedNames = selectedJson.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();
            Assert.Equal([firstShape], selectedNames);

            var addResult = InvokeShape(
                ShapeAction.AddToSelection,
                sessionId,
                shape_names: secondShape);
            output.WriteLine(addResult);
            Assert.True(JsonDocument.Parse(addResult).RootElement.GetProperty("success").GetBoolean());

            var afterAddJson = JsonDocument.Parse(InvokeShape(ShapeAction.ListSelection, sessionId)).RootElement;
            var namesAfterAdd = afterAddJson.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains(firstShape, namesAfterAdd);
            Assert.Contains(secondShape, namesAfterAdd);

            var removeResult = InvokeShape(
                ShapeAction.RemoveFromSelection,
                sessionId,
                shape_names: firstShape);
            output.WriteLine(removeResult);
            Assert.True(JsonDocument.Parse(removeResult).RootElement.GetProperty("success").GetBoolean());

            var afterRemoveJson = JsonDocument.Parse(InvokeShape(ShapeAction.ListSelection, sessionId)).RootElement;
            var namesAfterRemove = afterRemoveJson.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();
            Assert.Equal([secondShape], namesAfterRemove);

            var clearResult = InvokeShape(ShapeAction.ClearSelection, sessionId);
            output.WriteLine(clearResult);
            Assert.True(JsonDocument.Parse(clearResult).RootElement.GetProperty("success").GetBoolean());

            var afterClearJson = JsonDocument.Parse(InvokeShape(ShapeAction.ListSelection, sessionId)).RootElement;
            Assert.Empty(afterClearJson.GetProperty("shapes").EnumerateArray());
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
        string? shape_names = null)
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
            color_hex: null,
            line_width: null,
            degrees: null,
            shape_names: shape_names,
            alt_text: null,
            target_page_index: 0,
            visible: false,
            offset_x: null,
            offset_y: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            flip_type: 0,
            margin_left: null,
            margin_right: null,
            margin_top: null,
            margin_bottom: null,
            word_wrap: null,
            auto_size: null,
            color1: null,
            color2: null,
            gradient_style: 0,
            radius: null,
            reflection_type: 0,
            opacity: null,
            shape_type: 0,
            source_shape_name: null,
            target_shape_name: null,
            scale_x: null,
            scale_y: null,
            locked: false,
            preset_effect: 0,
            font_name: null,
            font_size: null,
            rotation_x: null,
            rotation_y: null,
            rotation_z: null,
            bevel_type: null,
            bevel_depth: null,
            property_name: null,
            property_value: null,
            connector_end: null,

            connection_point_x: null,

            connection_point_y: null,

            connection_point_name: null,

            connection_point_index: 0);
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
            color_hex: null,
            line_width: null,
            degrees: null,
            shape_names: null,
            alt_text: null,
            target_page_index: 0,
            visible: false,
            offset_x: null,
            offset_y: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            flip_type: 0,
            margin_left: null,
            margin_right: null,
            margin_top: null,
            margin_bottom: null,
            word_wrap: null,
            auto_size: null,
            color1: null,
            color2: null,
            gradient_style: 0,
            radius: null,
            reflection_type: 0,
            opacity: null,
            shape_type: 0,
            source_shape_name: null,
            target_shape_name: null,
            scale_x: null,
            scale_y: null,
            locked: false,
            preset_effect: 0,
            font_name: null,
            font_size: null,
            rotation_x: null,
            rotation_y: null,
            rotation_z: null,
            bevel_type: null,
            bevel_depth: null,
            property_name: null,
            property_value: null,
            connector_end: null,

            connection_point_x: null,

            connection_point_y: null,

            connection_point_name: null,

            connection_point_index: 0);
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
