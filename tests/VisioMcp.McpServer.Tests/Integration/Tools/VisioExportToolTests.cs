using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Export")]
public sealed class VisioExportToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ToPdf_CreatesPdfFile()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptExportPdfTool_{Guid.NewGuid():N}.vsdx");
        var pdfPath = Path.Join(Path.GetTempPath(), $"PptExportPdfTool_{Guid.NewGuid():N}.pdf");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            AddBasicShape(sessionId);

            var result = VisioExportTool.VisioExport(
                action: ExportAction.ToPdf,
                session_id: sessionId,
                destination_path: pdfPath,
                from_page: null,
                to_page: null,
                page_index: 0,
                copies: 0);
            output.WriteLine(result);

            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal(pdfPath, json.GetProperty("outputPath").GetString());
            Assert.Equal("PDF", json.GetProperty("format").GetString());
            Assert.True(File.Exists(pdfPath));
            Assert.True(new FileInfo(pdfPath).Length > 0);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteIfExists(tempPath);
            DeleteIfExists(pdfPath);
        }
    }

    [Fact]
    public void PageExport_CreatesSvgFile()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptExportPageTool_{Guid.NewGuid():N}.vsdx");
        var svgPath = Path.Join(Path.GetTempPath(), $"PptExportPageTool_{Guid.NewGuid():N}.svg");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            AddBasicShape(sessionId);

            var result = VisioExportTool.VisioExport(
                action: ExportAction.PageExport,
                session_id: sessionId,
                destination_path: svgPath,
                from_page: null,
                to_page: null,
                page_index: 1,
                copies: 0);
            output.WriteLine(result);

            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal(svgPath, json.GetProperty("outputPath").GetString());
            Assert.Equal("SVG", json.GetProperty("format").GetString());
            Assert.True(File.Exists(svgPath));
            Assert.True(new FileInfo(svgPath).Length > 0);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteIfExists(tempPath);
            DeleteIfExists(svgPath);
        }
    }

    [Fact]
    public void SaveCopy_CreatesDocumentCopy()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptExportCopyTool_{Guid.NewGuid():N}.vsdx");
        var copyPath = Path.Join(Path.GetTempPath(), $"PptExportCopyTool_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            AddBasicShape(sessionId);

            var result = VisioExportTool.VisioExport(
                action: ExportAction.SaveCopy,
                session_id: sessionId,
                destination_path: copyPath,
                from_page: null,
                to_page: null,
                page_index: 0,
                copies: 0);
            output.WriteLine(result);

            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal(copyPath, json.GetProperty("outputPath").GetString());
            Assert.Equal("VSDX", json.GetProperty("format").GetString());
            Assert.True(File.Exists(copyPath));
            Assert.True(new FileInfo(copyPath).Length > 0);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteIfExists(tempPath);
            DeleteIfExists(copyPath);
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

    private static void AddBasicShape(string sessionId)
    {
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
            shape_names: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            target_shape_name: null,
            property_name: null,
            property_value: null,
            connector_end: null);

        var json = JsonDocument.Parse(createResult).RootElement;
        Assert.True(json.GetProperty("success").GetBoolean());
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

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
