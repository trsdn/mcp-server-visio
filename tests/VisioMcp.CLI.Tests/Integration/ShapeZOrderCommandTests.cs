using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Shape")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class ShapeZOrderCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeZOrder_BringToFrontAndSendToBack_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeZOrderTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var firstShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var secondShape = await AddRectangleShapeAsync(sessionId, 96, 96, 144, 72);
            var thirdShape = await AddRectangleShapeAsync(sessionId, 120, 120, 144, 72);

            var initialOrder = await ReadShapeOrderAsync(sessionId);
            Assert.Equal(new[] { firstShape, secondShape, thirdShape }, initialOrder);

            var (frontResult, frontJson) = await CliProcessHelper.RunJsonAsync(
                $"shape z-order --session {sessionId} --page-index 1 --shape-name \"{firstShape}\" --z-order-cmd 1");
            output.WriteLine($"shape z-order front: {frontResult.Stdout}");

            Assert.Equal(0, frontResult.ExitCode);
            Assert.True(frontJson.RootElement.GetProperty("success").GetBoolean());

            var afterFrontOrder = await ReadShapeOrderAsync(sessionId);
            Assert.Equal(new[] { secondShape, thirdShape, firstShape }, afterFrontOrder);

            var (backResult, backJson) = await CliProcessHelper.RunJsonAsync(
                $"shape z-order --session {sessionId} --page-index 1 --shape-name \"{thirdShape}\" --z-order-cmd 2");
            output.WriteLine($"shape z-order back: {backResult.Stdout}");

            Assert.Equal(0, backResult.ExitCode);
            Assert.True(backJson.RootElement.GetProperty("success").GetBoolean());

            var afterBackOrder = await ReadShapeOrderAsync(sessionId);
            Assert.Equal(new[] { thirdShape, secondShape, firstShape }, afterBackOrder);
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

    private static async Task<List<string?>> ReadShapeOrderAsync(string sessionId)
    {
        var (_, pageJson) = await CliProcessHelper.RunJsonAsync($"page read --session {sessionId} --page-index 1");
        Assert.True(pageJson.RootElement.GetProperty("success").GetBoolean());

        return pageJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToList();
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
