using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Shape")]
[Trait("Speed", "Medium")]
public sealed class ShapeAlignCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeAlign_AlignAndDistribute_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeAlignTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var first = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var second = await AddRectangleShapeAsync(sessionId, 252, 144, 144, 72);
            var third = await AddRectangleShapeAsync(sessionId, 468, 108, 144, 72);

            var (alignResult, alignJson) = await CliProcessHelper.RunJsonAsync(
                $"shapealign align --session {sessionId} --page-index 1 --shape-names \"{first},{second}\" --align-type 0");
            output.WriteLine($"shapealign align: {alignResult.Stdout}");

            Assert.Equal(0, alignResult.ExitCode);
            Assert.True(alignJson.RootElement.GetProperty("success").GetBoolean());

            var firstAfterAlign = await ReadShapeAsync(sessionId, first);
            var secondAfterAlign = await ReadShapeAsync(sessionId, second);
            Assert.Equal(firstAfterAlign.GetProperty("left").GetSingle(), secondAfterAlign.GetProperty("left").GetSingle(), 3);

            var (distributeResult, distributeJson) = await CliProcessHelper.RunJsonAsync(
                $"shapealign distribute --session {sessionId} --page-index 1 --shape-names \"{first},{second},{third}\" --distribute-type 0");
            output.WriteLine($"shapealign distribute: {distributeResult.Stdout}");

            Assert.Equal(0, distributeResult.ExitCode);
            Assert.True(distributeJson.RootElement.GetProperty("success").GetBoolean());

            var lefts = new[]
            {
                (await ReadShapeAsync(sessionId, first)).GetProperty("left").GetSingle(),
                (await ReadShapeAsync(sessionId, second)).GetProperty("left").GetSingle(),
                (await ReadShapeAsync(sessionId, third)).GetProperty("left").GetSingle()
            }.OrderBy(value => value).ToArray();

            var firstGap = lefts[1] - lefts[0];
            var secondGap = lefts[2] - lefts[1];
            Assert.Equal(firstGap, secondGap, 3);
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

    private static async Task<JsonElement> ReadShapeAsync(string sessionId, string shapeName)
    {
        var (_, json) = await CliProcessHelper.RunJsonAsync(
            $"shape read --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
        return json.RootElement.GetProperty("shape").Clone();
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
