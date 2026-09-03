using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Window")]
public sealed class VisioWindowToolTests(ITestOutputHelper output)
{
    [Fact]
    public void WindowViewport_ZoomPanAndFit_ReturnsJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioWindowToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var shapeName = AddBasicShape(sessionId, 432f, 288f, 144f, 72f);
            var shapeCenter = ReadShapeCenter(sessionId, shapeName);

            var setZoomResult = InvokeWindow(WindowAction.SetZoom, sessionId, zoom_percent: 150);
            output.WriteLine(setZoomResult);
            Assert.True(JsonDocument.Parse(setZoomResult).RootElement.GetProperty("success").GetBoolean());

            var zoomJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetZoom, sessionId, page_index: 1)).RootElement;
            Assert.Equal(150f, zoomJson.GetProperty("zoomPercent").GetSingle(), 1);

            var beforePanJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetViewport, sessionId, page_index: 1)).RootElement;
            var beforeCenter = ReadCenter(beforePanJson);

            var panToShapeResult = InvokeWindow(WindowAction.PanToShape, sessionId, page_index: 1, shape_name: shapeName);
            output.WriteLine(panToShapeResult);
            Assert.True(JsonDocument.Parse(panToShapeResult).RootElement.GetProperty("success").GetBoolean());

            var afterPanJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetViewport, sessionId, page_index: 1)).RootElement;
            var afterPanCenter = ReadCenter(afterPanJson);
            Assert.True(Distance(afterPanCenter, shapeCenter) < Distance(beforeCenter, shapeCenter));

            var panOffsetResult = InvokeWindow(WindowAction.PanByOffset, sessionId, page_index: 1, offset_x: 72f, offset_y: 0f);
            output.WriteLine(panOffsetResult);
            Assert.True(JsonDocument.Parse(panOffsetResult).RootElement.GetProperty("success").GetBoolean());

            var afterOffsetJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetViewport, sessionId, page_index: 1)).RootElement;
            Assert.NotEqual(afterPanJson.GetProperty("centerX").GetSingle(), afterOffsetJson.GetProperty("centerX").GetSingle());

            var selectResult = VisioShapeTool.VisioShape(
                action: ShapeAction.SelectShapes,
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
                color_hex: null,
                line_width: null,
                degrees: null,
                shape_names: shapeName,
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
            Assert.True(JsonDocument.Parse(selectResult).RootElement.GetProperty("success").GetBoolean());

            var fitSelectionResult = InvokeWindow(WindowAction.FitSelection, sessionId, page_index: 1);
            output.WriteLine(fitSelectionResult);
            Assert.True(JsonDocument.Parse(fitSelectionResult).RootElement.GetProperty("success").GetBoolean());

            var fitPageResult = InvokeWindow(WindowAction.FitPage, sessionId, page_index: 1);
            output.WriteLine(fitPageResult);
            Assert.True(JsonDocument.Parse(fitPageResult).RootElement.GetProperty("success").GetBoolean());

            var fitPageJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetViewport, sessionId, page_index: 1)).RootElement;
            Assert.Equal(1, fitPageJson.GetProperty("viewFit").GetInt32());
            Assert.True(fitPageJson.GetProperty("width").GetSingle() > 0);
            Assert.True(fitPageJson.GetProperty("height").GetSingle() > 0);

            var initialVisualAidsJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetVisualAids, sessionId, page_index: 1)).RootElement;
            Assert.True(initialVisualAidsJson.GetProperty("success").GetBoolean());

            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGridVisible, sessionId, page_index: 1, visible: false)).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGuidesVisible, sessionId, page_index: 1, visible: false)).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetRulersVisible, sessionId, page_index: 1, visible: false)).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetDrawingAids, sessionId, enabled: false)).RootElement.GetProperty("success").GetBoolean());

            var disabledVisualAidsJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetVisualAids, sessionId, page_index: 1)).RootElement;
            Assert.False(disabledVisualAidsJson.GetProperty("gridVisible").GetBoolean());
            Assert.False(disabledVisualAidsJson.GetProperty("guidesVisible").GetBoolean());
            Assert.False(disabledVisualAidsJson.GetProperty("rulersVisible").GetBoolean());
            Assert.False(disabledVisualAidsJson.GetProperty("drawingAidsEnabled").GetBoolean());

            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGridVisible, sessionId, page_index: 1, visible: initialVisualAidsJson.GetProperty("gridVisible").GetBoolean())).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGuidesVisible, sessionId, page_index: 1, visible: initialVisualAidsJson.GetProperty("guidesVisible").GetBoolean())).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetRulersVisible, sessionId, page_index: 1, visible: initialVisualAidsJson.GetProperty("rulersVisible").GetBoolean())).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetDrawingAids, sessionId, enabled: initialVisualAidsJson.GetProperty("drawingAidsEnabled").GetBoolean())).RootElement.GetProperty("success").GetBoolean());

            var initialSnapJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetSnapSettings, sessionId)).RootElement;
            Assert.True(initialSnapJson.GetProperty("success").GetBoolean());

            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGridSnapStrength, sessionId, strength: 1)).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGuidesSnapStrength, sessionId, strength: 17)).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGeometrySnapStrength, sessionId, strength: 19)).RootElement.GetProperty("success").GetBoolean());

            var updatedSnapJson = JsonDocument.Parse(InvokeWindow(WindowAction.GetSnapSettings, sessionId)).RootElement;
            Assert.Equal(1, updatedSnapJson.GetProperty("gridSnapStrength").GetInt32());
            Assert.Equal(17, updatedSnapJson.GetProperty("guidesSnapStrength").GetInt32());
            Assert.Equal(19, updatedSnapJson.GetProperty("geometrySnapStrength").GetInt32());

            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGridSnapStrength, sessionId, strength: initialSnapJson.GetProperty("gridSnapStrength").GetInt32())).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGuidesSnapStrength, sessionId, strength: initialSnapJson.GetProperty("guidesSnapStrength").GetInt32())).RootElement.GetProperty("success").GetBoolean());
            Assert.True(JsonDocument.Parse(InvokeWindow(WindowAction.SetGeometrySnapStrength, sessionId, strength: initialSnapJson.GetProperty("geometrySnapStrength").GetInt32())).RootElement.GetProperty("success").GetBoolean());
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    private static string InvokeWindow(
        WindowAction action,
        string sessionId,
        int page_index = 0,
        int zoom_percent = 0,
        string? shape_name = null,
        float? offset_x = null,
        float? offset_y = null,
        bool visible = false,
        bool enabled = false,
        int strength = 0)
    {
        return VisioWindowTool.VisioWindow(
            action: action,
            session_id: sessionId,
            zoom_percent: zoom_percent,
            page_index: page_index,
            shape_name: shape_name,
            offset_x: offset_x,
            offset_y: offset_y,
            visible: visible,
            enabled: enabled,
            strength: strength);
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

        var createResult = InvokeShape(
            ShapeAction.AddShape,
            sessionId,
            left: left,
            top: top,
            width: width,
            height: height,
            auto_shape_type: 1);
        Assert.True(JsonDocument.Parse(createResult).RootElement.GetProperty("success").GetBoolean());

        var afterJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static (float X, float Y) ReadShapeCenter(string sessionId, string shapeName)
    {
        var readJson = JsonDocument.Parse(InvokeShape(ShapeAction.Read, sessionId, shape_name: shapeName)).RootElement;
        var shape = readJson.GetProperty("shape");
        return (shape.GetProperty("left").GetSingle(), shape.GetProperty("top").GetSingle());
    }

    private static (float X, float Y) ReadCenter(JsonElement viewportJson)
        => (viewportJson.GetProperty("centerX").GetSingle(), viewportJson.GetProperty("centerY").GetSingle());

    private static double Distance((float X, float Y) first, (float X, float Y) second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static string InvokeShape(
        ShapeAction action,
        string sessionId,
        string? shape_name = null,
        float? left = null,
        float? top = null,
        float? width = null,
        float? height = null,
        int auto_shape_type = 0)
    {
        return VisioShapeTool.VisioShape(
            action: action,
            session_id: sessionId,
            page_index: 1,
            shape_name: shape_name,
            left: left,
            top: top,
            width: width,
            height: height,
            text: null,
            auto_shape_type: auto_shape_type,
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
