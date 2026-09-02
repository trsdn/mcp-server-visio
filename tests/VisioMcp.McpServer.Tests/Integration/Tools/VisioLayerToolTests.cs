using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Layer")]
public sealed class VisioLayerToolTests(ITestOutputHelper output)
{
    [Fact]
    public void Layer_CreateAssignConfigureDelete_ReturnsJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioLayerToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var shapeName = AddBasicShape(sessionId);

            var createResult = InvokeLayer(
                LayerAction.Create,
                sessionId,
                layer_name: "Workflow",
                color_index: 4,
                visible: true,
                printable: false,
                locked: true);
            output.WriteLine(createResult);

            var createJson = JsonDocument.Parse(createResult).RootElement;
            Assert.True(createJson.GetProperty("success").GetBoolean());

            var listResult = InvokeLayer(LayerAction.List, sessionId);
            var listJson = JsonDocument.Parse(listResult).RootElement;
            var listedLayer = listJson.GetProperty("layers").EnumerateArray()
                .First(item => item.GetProperty("name").GetString() == "Workflow");
            Assert.Equal(4, listedLayer.GetProperty("colorIndex").GetInt32());

            var addShapeResult = InvokeLayer(
                LayerAction.AddShape,
                sessionId,
                layer_name: "Workflow",
                shape_name: shapeName,
                preserve_membership: true);
            Assert.True(JsonDocument.Parse(addShapeResult).RootElement.GetProperty("success").GetBoolean());

            var readResult = InvokeLayer(LayerAction.Read, sessionId, layer_name: "Workflow");
            var readJson = JsonDocument.Parse(readResult).RootElement;
            Assert.Equal(1, readJson.GetProperty("layer").GetProperty("memberCount").GetInt32());

            var setVisibilityResult = InvokeLayer(
                LayerAction.SetVisibility,
                sessionId,
                layer_name: "Workflow",
                visible: false);
            Assert.True(JsonDocument.Parse(setVisibilityResult).RootElement.GetProperty("success").GetBoolean());

            var setPrintResult = InvokeLayer(
                LayerAction.SetPrint,
                sessionId,
                layer_name: "Workflow",
                printable: true);
            Assert.True(JsonDocument.Parse(setPrintResult).RootElement.GetProperty("success").GetBoolean());

            var setLockResult = InvokeLayer(
                LayerAction.SetLock,
                sessionId,
                layer_name: "Workflow",
                locked: false);
            Assert.True(JsonDocument.Parse(setLockResult).RootElement.GetProperty("success").GetBoolean());

            var setColorResult = InvokeLayer(
                LayerAction.SetColor,
                sessionId,
                layer_name: "Workflow",
                color_index: 7);
            Assert.True(JsonDocument.Parse(setColorResult).RootElement.GetProperty("success").GetBoolean());

            var configuredResult = InvokeLayer(LayerAction.Read, sessionId, layer_name: "Workflow");
            var configuredJson = JsonDocument.Parse(configuredResult).RootElement;
            var configuredLayer = configuredJson.GetProperty("layer");
            Assert.False(configuredLayer.GetProperty("visible").GetBoolean());
            Assert.True(configuredLayer.GetProperty("printable").GetBoolean());
            Assert.False(configuredLayer.GetProperty("locked").GetBoolean());
            Assert.Equal(7, configuredLayer.GetProperty("colorIndex").GetInt32());

            var removeResult = InvokeLayer(
                LayerAction.RemoveShape,
                sessionId,
                layer_name: "Workflow",
                shape_name: shapeName,
                preserve_membership: true);
            Assert.True(JsonDocument.Parse(removeResult).RootElement.GetProperty("success").GetBoolean());

            var deleteResult = InvokeLayer(LayerAction.Delete, sessionId, layer_name: "Workflow");
            Assert.True(JsonDocument.Parse(deleteResult).RootElement.GetProperty("success").GetBoolean());
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    private static string InvokeLayer(
        LayerAction action,
        string sessionId,
        string? layer_name = null,
        string? shape_name = null,
        int? color_index = null,
        bool? visible = null,
        bool? printable = null,
        bool? locked = null,
        bool preserve_membership = true)
    {
        return VisioLayerTool.VisioLayer(
            action: action,
            session_id: sessionId,
            page_index: 1,
            layer_name: layer_name,
            shape_name: shape_name,
            color_index: color_index,
            visible: visible,
            printable: printable,
            locked: locked,
            preserve_membership: preserve_membership);
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

    private static string AddBasicShape(string sessionId)
    {
        var beforeJson = JsonDocument.Parse(VisioShapeTool.VisioShape(
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
            connection_point_index: 0)).RootElement;
        var beforeNames = beforeJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createResult = VisioShapeTool.VisioShape(
            action: ShapeAction.AddShape,
            session_id: sessionId,
            page_index: 1,
            shape_name: null,
            left: 72f,
            top: 72f,
            width: 144f,
            height: 72f,
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

        var afterJson = JsonDocument.Parse(VisioShapeTool.VisioShape(
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
            connection_point_index: 0)).RootElement;
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
