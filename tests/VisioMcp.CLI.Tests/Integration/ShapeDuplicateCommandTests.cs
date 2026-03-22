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
public sealed class ShapeDuplicateCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeDuplicate_CreatesSecondShapeWithSameGeometry()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeDuplicateTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var originalShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var originalShapeInfo = await ReadShapeAsync(sessionId, originalShape);

            var (duplicateResult, duplicateJson) = await CliProcessHelper.RunJsonAsync(
                $"shape duplicate --session {sessionId} --page-index 1 --shape-name \"{originalShape}\"");
            output.WriteLine($"shape duplicate: {duplicateResult.Stdout}");

            Assert.Equal(0, duplicateResult.ExitCode);
            Assert.True(duplicateJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
            var shapeNames = listJson.RootElement.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();

            Assert.Equal(2, shapeNames.Count);
            Assert.Contains(originalShape, shapeNames);

            var duplicateShape = shapeNames.Single(name => !string.Equals(name, originalShape, StringComparison.OrdinalIgnoreCase))!;
            var duplicateShapeInfo = await ReadShapeAsync(sessionId, duplicateShape);

            Assert.Equal(originalShapeInfo.GetProperty("width").GetSingle(), duplicateShapeInfo.GetProperty("width").GetSingle());
            Assert.Equal(originalShapeInfo.GetProperty("height").GetSingle(), duplicateShapeInfo.GetProperty("height").GetSingle());
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
        var (_, readJson) = await CliProcessHelper.RunJsonAsync(
            $"shape read --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
        Assert.True(readJson.RootElement.GetProperty("success").GetBoolean());
        return readJson.RootElement.GetProperty("shape").Clone();
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
