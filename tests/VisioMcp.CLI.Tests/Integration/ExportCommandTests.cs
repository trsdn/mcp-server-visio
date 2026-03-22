using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Export")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class ExportCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ExportToPdf_CreatesPdfFile()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliExportPdf_{Guid.NewGuid():N}.vsdx");
        var pdfPath = Path.Join(Path.GetTempPath(), $"CliExportPdf_{Guid.NewGuid():N}.pdf");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            await AddBasicShapeAsync(sessionId);

            var (result, json) = await CliProcessHelper.RunJsonAsync(
                $"export to-pdf --session {sessionId} --destination-path \"{pdfPath}\"");
            output.WriteLine($"export to-pdf: {result.Stdout}");

            Assert.Equal(0, result.ExitCode);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(pdfPath, json.RootElement.GetProperty("outputPath").GetString());
            Assert.Equal("PDF", json.RootElement.GetProperty("format").GetString());
            Assert.True(File.Exists(pdfPath));
            Assert.True(new FileInfo(pdfPath).Length > 0);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
            DeleteIfExists(pdfPath);
        }
    }

    [Fact]
    public async Task PageExport_CreatesPngFile()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliExportPage_{Guid.NewGuid():N}.vsdx");
        var pngPath = Path.Join(Path.GetTempPath(), $"CliExportPage_{Guid.NewGuid():N}.png");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            await AddBasicShapeAsync(sessionId);

            var (result, json) = await CliProcessHelper.RunJsonAsync(
                $"export page-export --session {sessionId} --page-index 1 --destination-path \"{pngPath}\"");
            output.WriteLine($"export page-export: {result.Stdout}");

            Assert.Equal(0, result.ExitCode);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(pngPath, json.RootElement.GetProperty("outputPath").GetString());
            Assert.Equal("PNG", json.RootElement.GetProperty("format").GetString());
            Assert.True(File.Exists(pngPath));
            Assert.True(new FileInfo(pngPath).Length > 0);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
            DeleteIfExists(pngPath);
        }
    }

    [Fact]
    public async Task SaveCopy_CreatesDocumentCopy()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliSaveCopy_{Guid.NewGuid():N}.vsdx");
        var copyPath = Path.Join(Path.GetTempPath(), $"CliSaveCopy_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            await AddBasicShapeAsync(sessionId);

            var (result, json) = await CliProcessHelper.RunJsonAsync(
                $"export save-copy --session {sessionId} --destination-path \"{copyPath}\"");
            output.WriteLine($"export save-copy: {result.Stdout}");

            Assert.Equal(0, result.ExitCode);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(copyPath, json.RootElement.GetProperty("outputPath").GetString());
            Assert.Equal("VSDX", json.RootElement.GetProperty("format").GetString());
            Assert.True(File.Exists(copyPath));
            Assert.True(new FileInfo(copyPath).Length > 0);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
            DeleteIfExists(copyPath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        var (result, json) = await CliProcessHelper.RunJsonAsync($"session create \"{filePath}\"");

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task AddBasicShapeAsync(string sessionId)
    {
        var (result, json) = await CliProcessHelper.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72");

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false");
        }

        DeleteIfExists(filePath);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
