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
public sealed class VisioShapeConnectorToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeListConnectorsAndReadConnector_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptShapeConnectorToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var startShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var endShape = AddBasicShape(sessionId, 288f, 72f, 144f, 72f);

            var addConnectorResult = VisioShapeTool.VisioShape(
                action: ShapeAction.AddConnector,
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
                connector_type: 1,
                start_shape_name: startShape,
                end_shape_name: endShape,
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
                connector_end: null);
            var addConnectorJson = JsonDocument.Parse(addConnectorResult).RootElement;
            Assert.True(addConnectorJson.GetProperty("success").GetBoolean());

            var listResult = VisioShapeTool.VisioShape(
                action: ShapeAction.ListConnectors,
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
                connector_end: null);
            output.WriteLine(listResult);

            var listJson = JsonDocument.Parse(listResult).RootElement;
            Assert.True(listJson.GetProperty("success").GetBoolean());

            var connector = listJson.GetProperty("connectors").EnumerateArray()
                .First(item =>
                    string.Equals(item.GetProperty("startShapeName").GetString(), startShape, StringComparison.Ordinal)
                    && string.Equals(item.GetProperty("endShapeName").GetString(), endShape, StringComparison.Ordinal));

            var connectorName = connector.GetProperty("name").GetString();
            Assert.False(string.IsNullOrWhiteSpace(connectorName));

            var readResult = VisioShapeTool.VisioShape(
                action: ShapeAction.ReadConnector,
                session_id: sessionId,
                page_index: 1,
                shape_name: connectorName,
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
                connector_end: null);
            output.WriteLine(readResult);

            var readJson = JsonDocument.Parse(readResult).RootElement;
            Assert.True(readJson.GetProperty("success").GetBoolean());
            Assert.Equal(connectorName, readJson.GetProperty("connector").GetProperty("name").GetString());
            Assert.Equal(startShape, readJson.GetProperty("connector").GetProperty("startShapeName").GetString());
            Assert.Equal(endShape, readJson.GetProperty("connector").GetProperty("endShapeName").GetString());
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    [Fact]
    public void ShapeDisconnectAndReconnectConnector_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptShapeReconnectToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var startShape = AddBasicShape(sessionId, 72f, 72f, 144f, 72f);
            var endShape = AddBasicShape(sessionId, 288f, 72f, 144f, 72f);
            var replacementStart = AddBasicShape(sessionId, 72f, 216f, 144f, 72f);
            var replacementEnd = AddBasicShape(sessionId, 288f, 216f, 144f, 72f);

            var addConnectorResult = VisioShapeTool.VisioShape(
                action: ShapeAction.AddConnector,
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
                connector_type: 1,
                start_shape_name: startShape,
                end_shape_name: endShape,
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
                connector_end: null);
            Assert.True(JsonDocument.Parse(addConnectorResult).RootElement.GetProperty("success").GetBoolean());

            var connectorName = FindConnectorName(sessionId, startShape, endShape);

            var disconnectStartResult = InvokeShape(
                ShapeAction.DisconnectConnector,
                sessionId,
                connectorName,
                connectorEnd: "start");
            var disconnectStartJson = JsonDocument.Parse(disconnectStartResult).RootElement;
            Assert.True(disconnectStartJson.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(disconnectStartJson.GetProperty("connector"), null, endShape);

            var reconnectStartResult = InvokeShape(
                ShapeAction.ReconnectConnector,
                sessionId,
                connectorName,
                connectorEnd: "start",
                targetShapeName: replacementStart);
            var reconnectStartJson = JsonDocument.Parse(reconnectStartResult).RootElement;
            Assert.True(reconnectStartJson.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(reconnectStartJson.GetProperty("connector"), replacementStart, endShape);

            var disconnectEndResult = InvokeShape(
                ShapeAction.DisconnectConnector,
                sessionId,
                connectorName,
                connectorEnd: "end");
            var disconnectEndJson = JsonDocument.Parse(disconnectEndResult).RootElement;
            Assert.True(disconnectEndJson.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(disconnectEndJson.GetProperty("connector"), replacementStart, null);

            var reconnectEndResult = InvokeShape(
                ShapeAction.ReconnectConnector,
                sessionId,
                connectorName,
                connectorEnd: "end",
                targetShapeName: replacementEnd);
            var reconnectEndJson = JsonDocument.Parse(reconnectEndResult).RootElement;
            Assert.True(reconnectEndJson.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(reconnectEndJson.GetProperty("connector"), replacementStart, replacementEnd);
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
        var listBeforeResult = ListShapes(sessionId);
        var beforeJson = JsonDocument.Parse(listBeforeResult).RootElement;
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
            connector_end: null);

        var createJson = JsonDocument.Parse(createResult).RootElement;
        Assert.True(createJson.GetProperty("success").GetBoolean());

        var listAfterResult = ListShapes(sessionId);
        var afterJson = JsonDocument.Parse(listAfterResult).RootElement;

        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static string FindConnectorName(string sessionId, string? expectedStartShapeName, string? expectedEndShapeName)
    {
        var listJson = JsonDocument.Parse(InvokeShape(ShapeAction.ListConnectors, sessionId)).RootElement;
        return listJson.GetProperty("connectors").EnumerateArray()
            .First(item =>
                string.Equals(GetOptionalString(item, "startShapeName"), expectedStartShapeName, StringComparison.Ordinal)
                && string.Equals(GetOptionalString(item, "endShapeName"), expectedEndShapeName, StringComparison.Ordinal))
            .GetProperty("name")
            .GetString()!;
    }

    private static string InvokeShape(
        ShapeAction action,
        string sessionId,
        string? shapeName = null,
        string? connectorEnd = null,
        string? targetShapeName = null)
    {
        return VisioShapeTool.VisioShape(
            action: action,
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
            target_shape_name: targetShapeName,
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
            connector_end: connectorEnd);
    }

    private static void AssertConnectorEndpoints(JsonElement connectorJson, string? expectedStartShapeName, string? expectedEndShapeName)
    {
        Assert.Equal(expectedStartShapeName, GetOptionalString(connectorJson, "startShapeName"));
        Assert.Equal(expectedEndShapeName, GetOptionalString(connectorJson, "endShapeName"));
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        string? value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string ListShapes(string sessionId)
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
            connector_end: null);
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
