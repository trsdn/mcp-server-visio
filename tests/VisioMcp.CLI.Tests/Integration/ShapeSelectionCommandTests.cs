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
public sealed class ShapeSelectionCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeSelection_SelectAddRemoveClear_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeSelectionTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var firstShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var secondShape = await AddRectangleShapeAsync(sessionId, 252, 72, 144, 72);

            var (selectResult, selectJson) = await CliProcessHelper.RunJsonAsync(
                $"shape select-shapes --session {sessionId} --page-index 1 --shape-names \"{firstShape}\"");
            output.WriteLine($"shape select-shapes: {selectResult.Stdout}");
            Assert.Equal(0, selectResult.ExitCode);
            Assert.True(selectJson.RootElement.GetProperty("success").GetBoolean());

            var (_, selectedJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-selection --session {sessionId} --page-index 1");
            var selectedNames = selectedJson.RootElement.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();
            Assert.Equal([firstShape], selectedNames);

            var (addResult, addJson) = await CliProcessHelper.RunJsonAsync(
                $"shape add-to-selection --session {sessionId} --page-index 1 --shape-names \"{secondShape}\"");
            output.WriteLine($"shape add-to-selection: {addResult.Stdout}");
            Assert.Equal(0, addResult.ExitCode);
            Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterAddJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-selection --session {sessionId} --page-index 1");
            var namesAfterAdd = afterAddJson.RootElement.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains(firstShape, namesAfterAdd);
            Assert.Contains(secondShape, namesAfterAdd);

            var (removeResult, removeJson) = await CliProcessHelper.RunJsonAsync(
                $"shape remove-from-selection --session {sessionId} --page-index 1 --shape-names \"{firstShape}\"");
            output.WriteLine($"shape remove-from-selection: {removeResult.Stdout}");
            Assert.Equal(0, removeResult.ExitCode);
            Assert.True(removeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterRemoveJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-selection --session {sessionId} --page-index 1");
            var namesAfterRemove = afterRemoveJson.RootElement.GetProperty("shapes").EnumerateArray()
                .Select(shape => shape.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToList();
            Assert.Equal([secondShape], namesAfterRemove);

            var (clearResult, clearJson) = await CliProcessHelper.RunJsonAsync(
                $"shape clear-selection --session {sessionId} --page-index 1");
            output.WriteLine($"shape clear-selection: {clearResult.Stdout}");
            Assert.Equal(0, clearResult.ExitCode);
            Assert.True(clearJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterClearJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-selection --session {sessionId} --page-index 1");
            Assert.Empty(afterClearJson.RootElement.GetProperty("shapes").EnumerateArray());
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
