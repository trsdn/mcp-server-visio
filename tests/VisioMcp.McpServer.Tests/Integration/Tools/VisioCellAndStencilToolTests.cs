using System.Globalization;
using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Cell")]
public sealed class VisioCellAndStencilToolTests(ITestOutputHelper output)
{
    [Fact]
    public void CellReadAndWrite_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptCellToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var shapeName = AddBasicShape(sessionId);

            var readResult = VisioCellTool.VisioCell(
                action: CellAction.Read,
                session_id: sessionId,
                page_index: 1,
                shape_name: shapeName,
                cell_name: "Width",
                value: null,
                formula: null);
            output.WriteLine(readResult);

            var readJson = JsonDocument.Parse(readResult).RootElement;
            Assert.True(readJson.GetProperty("success").GetBoolean());
            var beforeValue = double.Parse(readJson.GetProperty("cell").GetProperty("value").GetString()!, CultureInfo.InvariantCulture);
            Assert.True(beforeValue > 0);

            var writeResult = VisioCellTool.VisioCell(
                action: CellAction.Write,
                session_id: sessionId,
                page_index: 1,
                shape_name: shapeName,
                cell_name: "Width",
                value: "3",
                formula: null);
            output.WriteLine(writeResult);

            var writeJson = JsonDocument.Parse(writeResult).RootElement;
            Assert.True(writeJson.GetProperty("success").GetBoolean());

            var readAfterResult = VisioCellTool.VisioCell(
                action: CellAction.Read,
                session_id: sessionId,
                page_index: 1,
                shape_name: shapeName,
                cell_name: "Width",
                value: null,
                formula: null);
            var readAfterJson = JsonDocument.Parse(readAfterResult).RootElement;
            var afterValue = double.Parse(readAfterJson.GetProperty("cell").GetProperty("value").GetString()!, CultureInfo.InvariantCulture);
            Assert.Equal(3d, afterValue, 3);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    [Fact]
    public void StencilListMastersAndDropMaster_ReturnJsonSuccess()
    {
        var stencilPath = FindInstalledStencilPath();
        var tempPath = Path.Join(Path.GetTempPath(), $"PptStencilToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);

            var listResult = VisioStencilTool.VisioStencil(
                action: StencilAction.ListMasters,
                session_id: sessionId,
                stencil_path: stencilPath,
                page_index: 0,
                master_name: null,
                x_position: null,
                y_position: null);
            output.WriteLine(listResult);

            var listJson = JsonDocument.Parse(listResult).RootElement;
            Assert.True(listJson.GetProperty("success").GetBoolean());
            var masters = listJson.GetProperty("masters").EnumerateArray().ToList();
            Assert.NotEmpty(masters);

            var masterName = masters
                .Select(master => master.GetProperty("name").GetString())
                .First(name => !string.IsNullOrWhiteSpace(name))!;

            var beforeShapeList = ListShapes(sessionId);
            var beforeJson = JsonDocument.Parse(beforeShapeList).RootElement;
            var beforeCount = beforeJson.GetProperty("shapes").GetArrayLength();

            var dropResult = VisioStencilTool.VisioStencil(
                action: StencilAction.DropMaster,
                session_id: sessionId,
                stencil_path: stencilPath,
                page_index: 1,
                master_name: masterName,
                x_position: 144f,
                y_position: 144f);
            output.WriteLine(dropResult);

            var dropJson = JsonDocument.Parse(dropResult).RootElement;
            Assert.True(dropJson.GetProperty("success").GetBoolean());

            var afterShapeList = ListShapes(sessionId);
            var afterJson = JsonDocument.Parse(afterShapeList).RootElement;
            var afterCount = afterJson.GetProperty("shapes").GetArrayLength();

            Assert.True(afterCount > beforeCount);
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

    private static string AddBasicShape(string sessionId)
    {
        var listBeforeResult = ListShapes(sessionId);
        var beforeJson = JsonDocument.Parse(listBeforeResult).RootElement;
        var beforeNames = beforeJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createResult = AddRectangleShape(sessionId, left: 72f, top: 72f, width: 144f, height: 72f);
        var createJson = JsonDocument.Parse(createResult).RootElement;
        Assert.True(createJson.GetProperty("success").GetBoolean());

        var listAfterResult = ListShapes(sessionId);
        var afterJson = JsonDocument.Parse(listAfterResult).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .First(name => name is not null && !beforeNames.Contains(name))!;
    }

    private static string FindInstalledStencilPath()
    {
        var roots = new[]
        {
            @"C:\Program Files\Microsoft Office\root\Office16\Visio Content",
            @"C:\Program Files (x86)\Microsoft Office\root\Office16\Visio Content",
            @"C:\Program Files\Microsoft Office\Office16\Visio Content",
            @"C:\Program Files (x86)\Microsoft Office\Office16\Visio Content"
        };

        var preferredFiles = new[] { "BASIC_M.VSSX", "BLOCK_M.VSSX", "BASICORGCHART_M.VSSX" };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var preferredFile in preferredFiles)
            {
                var match = Directory.EnumerateFiles(root, preferredFile, SearchOption.AllDirectories).FirstOrDefault();
                if (match is not null)
                {
                    return match;
                }
            }
        }

        throw new InvalidOperationException("Could not find an installed Visio stencil file for MCP stencil tests.");
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

    private static string AddRectangleShape(string sessionId, float left, float top, float width, float height)
    {
        return VisioShapeTool.VisioShape(
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
