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
public sealed class VisioShapeZOrderToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeZOrder_BringToFrontAndSendToBack_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioShapeZOrderToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var firstShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var secondShape = AddBasicShape(sessionId, 96f, 96f, 144f, 72f);
            var thirdShape = AddBasicShape(sessionId, 120f, 120f, 144f, 72f);

            Assert.Equal(new[] { firstShape, secondShape, thirdShape }, ReadShapeOrder(sessionId));

            var bringFrontResult = InvokeShape(
                ShapeAction.ZOrder,
                sessionId,
                shape_name: firstShape,
                z_order_cmd: 1);
            output.WriteLine(bringFrontResult);

            var bringFrontJson = JsonDocument.Parse(bringFrontResult).RootElement;
            Assert.True(bringFrontJson.GetProperty("success").GetBoolean());
            Assert.Equal(new[] { secondShape, thirdShape, firstShape }, ReadShapeOrder(sessionId));

            var sendBackResult = InvokeShape(
                ShapeAction.ZOrder,
                sessionId,
                shape_name: thirdShape,
                z_order_cmd: 2);
            output.WriteLine(sendBackResult);

            var sendBackJson = JsonDocument.Parse(sendBackResult).RootElement;
            Assert.True(sendBackJson.GetProperty("success").GetBoolean());
            Assert.Equal(new[] { thirdShape, secondShape, firstShape }, ReadShapeOrder(sessionId));
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
        int z_order_cmd = 0)
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
            z_order_cmd: z_order_cmd,
            color_hex: null,
            line_width: null,
            degrees: null,
            shape_names: null,
            alt_text: null,
            target_slide_index: 0,
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
            action_type: 0,
            hyperlink_address: null,
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
            target_slide_index: 0,
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
            action_type: 0,
            hyperlink_address: null,
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

    private static List<string?> ReadShapeOrder(string sessionId)
    {
        var pageResult = VisioPageTool.VisioPage(
            action: PageAction.Read,
            session_id: sessionId,
            page_index: 1,
            position: 0,
            name: null,
            guide_type: 0,
            x_position: null,
            y_position: null,
            guide_name: null,
            route_style: 0,
            connector_routing_extension: 0,
            line_jump_code: 0,
            line_jump_style: 0,
            walk_preference: 0,
            place_style: 0,
            is_background: false,
            back_page_name: null);

        var pageJson = JsonDocument.Parse(pageResult).RootElement;
        Assert.True(pageJson.GetProperty("success").GetBoolean());

        return pageJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToList();
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
