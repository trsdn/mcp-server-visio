using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Shape")]
[Trait("RequiresVisio", "true")]
[Trait("Speed", "Medium")]
public sealed class ShapeMergeCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeMerge_Union_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeMergeTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var firstShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var secondShape = await AddRectangleShapeAsync(sessionId, 144, 108, 144, 72);

            var (_, beforeJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
            var beforeShapes = beforeJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();
            Assert.Equal(2, beforeShapes.Count);

            var (mergeResult, mergeJson) = await CliProcessHelper.RunJsonAsync(
                $"shape merge --session {sessionId} --page-index 1 --shape-names \"{firstShape},{secondShape}\" --merge-type 1");
            output.WriteLine($"shape merge: {mergeResult.Stdout}");

            Assert.Equal(0, mergeResult.ExitCode);
            Assert.True(mergeJson.RootElement.GetProperty("success").GetBoolean());

            var (afterResult, afterJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
            output.WriteLine($"shape list after merge: {afterResult.Stdout}");

            Assert.Equal(0, afterResult.ExitCode);
            Assert.True(afterJson.RootElement.GetProperty("success").GetBoolean());

            var afterShapes = afterJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();
            var mergedShape = Assert.Single(afterShapes);
            Assert.True(mergedShape.GetProperty("width").GetSingle() > 144f);
            Assert.True(mergedShape.GetProperty("height").GetSingle() > 72f);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        var (result, json) = await CliProcessHelper.RunJsonAsync($"session create \"{filePath}\"", timeoutMs: 120000);

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task<string> AddRectangleShapeAsync(string sessionId, float left, float top, float width, float height)
    {
        var (_, beforeJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
        var beforeNames = beforeJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (_, addJson) = await CliProcessHelper.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left {left} --top {top} --width {width} --height {height}");
        Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

        var (_, afterJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
        return afterJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false", timeoutMs: 120000);
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
