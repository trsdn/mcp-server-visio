using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Page")]
public sealed class VisioPageAndTextToolTests(ITestOutputHelper output)
{
    [Fact]
    public void PageCreateReadAndRename_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptPageToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);

            var createResult = InvokePage(
                action: PageAction.Create,
                sessionId: sessionId,
                pageIndex: 0,
                position: 0,
                name: "Overview");
            output.WriteLine(createResult);

            var createJson = JsonDocument.Parse(createResult).RootElement;
            Assert.True(createJson.GetProperty("success").GetBoolean());

            var listResult = InvokePage(
                action: PageAction.List,
                sessionId: sessionId);
            var listJson = JsonDocument.Parse(listResult).RootElement;
            Assert.True(listJson.GetProperty("success").GetBoolean());
            Assert.Contains(
                listJson.GetProperty("pages").EnumerateArray(),
                page => string.Equals(page.GetProperty("name").GetString(), "Overview", StringComparison.Ordinal));

            var readResult = InvokePage(
                action: PageAction.Read,
                sessionId: sessionId,
                pageIndex: 2);
            var readJson = JsonDocument.Parse(readResult).RootElement;
            Assert.True(readJson.GetProperty("success").GetBoolean());
            Assert.Equal("Overview", readJson.GetProperty("page").GetProperty("name").GetString());

            var renameResult = InvokePage(
                action: PageAction.SetName,
                sessionId: sessionId,
                pageIndex: 2,
                name: "Swimlane");
            var renameJson = JsonDocument.Parse(renameResult).RootElement;
            Assert.True(renameJson.GetProperty("success").GetBoolean());

            var listAfterRename = InvokePage(
                action: PageAction.List,
                sessionId: sessionId);
            var listAfterJson = JsonDocument.Parse(listAfterRename).RootElement;
            Assert.Contains(
                listAfterJson.GetProperty("pages").EnumerateArray(),
                page => string.Equals(page.GetProperty("name").GetString(), "Swimlane", StringComparison.Ordinal));
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    [Fact]
    public void PageGuides_AddMoveAndDelete_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptPageGuideToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);

            var addResult = InvokePage(
                action: PageAction.AddGuide,
                sessionId: sessionId,
                pageIndex: 1,
                guideType: 2,
                xPosition: 0,
                yPosition: 144);
            var addJson = JsonDocument.Parse(addResult).RootElement;
            Assert.True(addJson.GetProperty("success").GetBoolean());

            var listResult = InvokePage(
                action: PageAction.ListGuides,
                sessionId: sessionId,
                pageIndex: 1);
            var listJson = JsonDocument.Parse(listResult).RootElement;
            var guide = listJson.GetProperty("guides").EnumerateArray()
                .Single(item => item.GetProperty("guideTypeName").GetString() == "horizontal");
            var guideName = guide.GetProperty("name").GetString();

            Assert.NotNull(guideName);
            Assert.Equal(144f, guide.GetProperty("y").GetSingle(), 1);

            var moveResult = InvokePage(
                action: PageAction.SetGuidePosition,
                sessionId: sessionId,
                pageIndex: 1,
                guideName: guideName,
                xPosition: 0,
                yPosition: 216);
            var moveJson = JsonDocument.Parse(moveResult).RootElement;
            Assert.True(moveJson.GetProperty("success").GetBoolean());

            var movedListResult = InvokePage(
                action: PageAction.ListGuides,
                sessionId: sessionId,
                pageIndex: 1);
            var movedListJson = JsonDocument.Parse(movedListResult).RootElement;
            var movedGuide = movedListJson.GetProperty("guides").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == guideName);
            Assert.Equal(216f, movedGuide.GetProperty("y").GetSingle(), 1);

            var deleteResult = InvokePage(
                action: PageAction.DeleteGuide,
                sessionId: sessionId,
                pageIndex: 1,
                guideName: guideName);
            var deleteJson = JsonDocument.Parse(deleteResult).RootElement;
            Assert.True(deleteJson.GetProperty("success").GetBoolean());

            var finalListResult = InvokePage(
                action: PageAction.ListGuides,
                sessionId: sessionId,
                pageIndex: 1);
            var finalListJson = JsonDocument.Parse(finalListResult).RootElement;
            Assert.DoesNotContain(
                finalListJson.GetProperty("guides").EnumerateArray(),
                item => item.GetProperty("name").GetString() == guideName);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    [Fact]
    public void PageRoutingSettings_ReadAndUpdate_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptPageRoutingToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);

            var initialResult = InvokePage(
                action: PageAction.GetRoutingSettings,
                sessionId: sessionId,
                pageIndex: 1);
            var initialJson = JsonDocument.Parse(initialResult).RootElement;

            var routeResult = InvokePage(
                action: PageAction.SetRouteStyle,
                sessionId: sessionId,
                pageIndex: 1,
                routeStyle: 1);
            Assert.True(JsonDocument.Parse(routeResult).RootElement.GetProperty("success").GetBoolean());

            var extensionResult = InvokePage(
                action: PageAction.SetConnectorRoutingExtension,
                sessionId: sessionId,
                pageIndex: 1,
                connectorRoutingExtension: 2);
            Assert.True(JsonDocument.Parse(extensionResult).RootElement.GetProperty("success").GetBoolean());

            var jumpCodeResult = InvokePage(
                action: PageAction.SetLineJumpCode,
                sessionId: sessionId,
                pageIndex: 1,
                lineJumpCode: 6);
            Assert.True(JsonDocument.Parse(jumpCodeResult).RootElement.GetProperty("success").GetBoolean());

            var jumpStyleResult = InvokePage(
                action: PageAction.SetLineJumpStyle,
                sessionId: sessionId,
                pageIndex: 1,
                lineJumpStyle: 1);
            Assert.True(JsonDocument.Parse(jumpStyleResult).RootElement.GetProperty("success").GetBoolean());

            var walkResult = InvokePage(
                action: PageAction.SetWalkPreference,
                sessionId: sessionId,
                pageIndex: 1,
                walkPreference: 1);
            Assert.True(JsonDocument.Parse(walkResult).RootElement.GetProperty("success").GetBoolean());

            var placeResult = InvokePage(
                action: PageAction.SetPlaceStyle,
                sessionId: sessionId,
                pageIndex: 1,
                placeStyle: 1);
            Assert.True(JsonDocument.Parse(placeResult).RootElement.GetProperty("success").GetBoolean());

            var currentResult = InvokePage(
                action: PageAction.GetRoutingSettings,
                sessionId: sessionId,
                pageIndex: 1);
            var currentJson = JsonDocument.Parse(currentResult).RootElement;

            Assert.Equal(1, currentJson.GetProperty("routeStyle").GetInt32());
            Assert.Equal(2, currentJson.GetProperty("connectorRoutingExtension").GetInt32());
            Assert.Equal(6, currentJson.GetProperty("lineJumpCode").GetInt32());
            Assert.Equal(1, currentJson.GetProperty("lineJumpStyle").GetInt32());
            Assert.Equal(1, currentJson.GetProperty("walkPreference").GetInt32());
            Assert.Equal(1, currentJson.GetProperty("placeStyle").GetInt32());

            InvokePage(
                action: PageAction.SetRouteStyle,
                sessionId: sessionId,
                pageIndex: 1,
                routeStyle: initialJson.GetProperty("routeStyle").GetInt32());
            InvokePage(
                action: PageAction.SetConnectorRoutingExtension,
                sessionId: sessionId,
                pageIndex: 1,
                connectorRoutingExtension: initialJson.GetProperty("connectorRoutingExtension").GetInt32());
            InvokePage(
                action: PageAction.SetLineJumpCode,
                sessionId: sessionId,
                pageIndex: 1,
                lineJumpCode: initialJson.GetProperty("lineJumpCode").GetInt32());
            InvokePage(
                action: PageAction.SetLineJumpStyle,
                sessionId: sessionId,
                pageIndex: 1,
                lineJumpStyle: initialJson.GetProperty("lineJumpStyle").GetInt32());
            InvokePage(
                action: PageAction.SetWalkPreference,
                sessionId: sessionId,
                pageIndex: 1,
                walkPreference: initialJson.GetProperty("walkPreference").GetInt32());
            InvokePage(
                action: PageAction.SetPlaceStyle,
                sessionId: sessionId,
                pageIndex: 1,
                placeStyle: initialJson.GetProperty("placeStyle").GetInt32());
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    [Fact]
    public void TextSetAndGet_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptTextToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var shapeName = AddBasicShape(sessionId);

            var setResult = VisioTextTool.VisioText(
                action: TextAction.SetText,
                session_id: sessionId,
                page_index: 1,
                shape_name: shapeName,
                text: "Start",
                search_text: null,
                replace_text: null,
                font_name: null,
                font_size: null,
                bold: null,
                italic: null,
                color: null,
                alignment: null,
                vertical_alignment: null,
                underline: null,
                strikethrough: null,
                subscript: null,
                superscript: null,
                line_spacing: null,
                space_before: null,
                space_after: null,
                character_spacing: null,
                bullet_type: 0,
                bullet_character: null,
                indent_level: 0,
                link_text: null,
                url: null,
                case_type: 0,
                char_number: 0,
                date_time_format: 0);
            var setJson = JsonDocument.Parse(setResult).RootElement;
            Assert.True(setJson.GetProperty("success").GetBoolean());

            var getResult = VisioTextTool.VisioText(
                action: TextAction.GetText,
                session_id: sessionId,
                page_index: 1,
                shape_name: shapeName,
                text: null,
                search_text: null,
                replace_text: null,
                font_name: null,
                font_size: null,
                bold: null,
                italic: null,
                color: null,
                alignment: null,
                vertical_alignment: null,
                underline: null,
                strikethrough: null,
                subscript: null,
                superscript: null,
                line_spacing: null,
                space_before: null,
                space_after: null,
                character_spacing: null,
                bullet_type: 0,
                bullet_character: null,
                indent_level: 0,
                link_text: null,
                url: null,
                case_type: 0,
                char_number: 0,
                date_time_format: 0);
            output.WriteLine(getResult);

            var getJson = JsonDocument.Parse(getResult).RootElement;
            Assert.True(getJson.GetProperty("success").GetBoolean());
            Assert.Equal(shapeName, getJson.GetProperty("shapeName").GetString());
            Assert.Equal("Start", getJson.GetProperty("text").GetString());
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

    private static string InvokePage(
        PageAction action,
        string sessionId,
        int pageIndex = 0,
        int position = 0,
        string? name = null,
        int guideType = 0,
        string? guideName = null,
        float? xPosition = null,
        float? yPosition = null,
        int routeStyle = 0,
        int connectorRoutingExtension = 0,
        int lineJumpCode = 0,
        int lineJumpStyle = 0,
        int walkPreference = 0,
        int placeStyle = 0)
    {
        return VisioPageTool.VisioPage(
            action: action,
            session_id: sessionId,
            page_index: pageIndex,
            position: position,
            name: name,
            guide_type: guideType,
            guide_name: guideName,
            x_position: xPosition,
            y_position: yPosition,
            route_style: routeStyle,
            connector_routing_extension: connectorRoutingExtension,
            line_jump_code: lineJumpCode,
            line_jump_style: lineJumpStyle,
            walk_preference: walkPreference,
            place_style: placeStyle);
    }

    private static string AddBasicShape(string sessionId)
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

        var listAfterResult = ListShapes(sessionId);
        var afterJson = JsonDocument.Parse(listAfterResult).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .First(name => name is not null && !beforeNames.Contains(name))!;
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
            connector_end: null,

            connection_point_x: null,

            connection_point_y: null,

            connection_point_name: null,

            connection_point_index: 0);
    }

    private static void CloseSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

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

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
