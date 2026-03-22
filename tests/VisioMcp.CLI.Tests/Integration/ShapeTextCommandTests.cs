using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

/// <summary>
/// Integration tests for the minimal Visio-native shape and text workflow.
/// Verifies that a real Visio session can create a shape on a page and then read/write its text.
/// </summary>
[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Shape")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class ShapeTextCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeAddAndTextRoundTrip_WorksOnVisioPage()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeTextTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (_, listBeforeJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list --session {sessionId} --page-index 1");
            var beforeShapes = listBeforeJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();

            var (addResult, addJson) = await CliProcessHelper.RunJsonAsync(
                $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72");
            output.WriteLine($"shape add-shape: {addResult.Stdout}");

            Assert.Equal(0, addResult.ExitCode);
            Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listAfterJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list --session {sessionId} --page-index 1");
            var afterShapes = listAfterJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();

            Assert.True(afterShapes.Count > beforeShapes.Count);

            var shapeName = afterShapes
                .Select(shape => shape.GetProperty("name").GetString())
                .Except(beforeShapes.Select(shape => shape.GetProperty("name").GetString()))
                .First();

            var (setResult, setJson) = await CliProcessHelper.RunJsonAsync(
                $"text set --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --text \"Hello Visio\"");
            output.WriteLine($"text set: {setResult.Stdout}");

            Assert.Equal(0, setResult.ExitCode);
            Assert.True(setJson.RootElement.GetProperty("success").GetBoolean());

            var (getResult, getJson) = await CliProcessHelper.RunJsonAsync(
                $"text get --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
            output.WriteLine($"text get: {getResult.Stdout}");

            Assert.Equal(0, getResult.ExitCode);
            Assert.True(getJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Hello Visio", getJson.RootElement.GetProperty("text").GetString());
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        var (result, json) = await CliProcessHelper.RunJsonAsync($"session create \"{filePath}\"");

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false");
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
