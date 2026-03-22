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
public sealed class ShapeGroupCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeGroup_ListReadUngroup_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeGroupTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var firstShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var secondShape = await AddRectangleShapeAsync(sessionId, 252, 72, 144, 72);

            var (groupResult, groupJson) = await CliProcessHelper.RunJsonAsync(
                $"shape group --session {sessionId} --page-index 1 --shape-names \"{firstShape},{secondShape}\"");
            output.WriteLine($"shape group: {groupResult.Stdout}");

            Assert.Equal(0, groupResult.ExitCode);
            Assert.True(groupJson.RootElement.GetProperty("success").GetBoolean());

            var (listGroupsResult, listGroupsJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-groups --session {sessionId} --page-index 1");
            output.WriteLine($"shape list-groups: {listGroupsResult.Stdout}");

            Assert.Equal(0, listGroupsResult.ExitCode);
            Assert.True(listGroupsJson.RootElement.GetProperty("success").GetBoolean());

            var groups = listGroupsJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();
            var group = Assert.Single(groups);
            var groupName = group.GetProperty("name").GetString();
            Assert.False(string.IsNullOrWhiteSpace(groupName));
            Assert.True(group.GetProperty("isGroup").GetBoolean());

            var (readGroupResult, readGroupJson) = await CliProcessHelper.RunJsonAsync(
                $"shape read-group --session {sessionId} --page-index 1 --shape-name \"{groupName}\"");
            output.WriteLine($"shape read-group: {readGroupResult.Stdout}");

            Assert.Equal(0, readGroupResult.ExitCode);
            Assert.True(readGroupJson.RootElement.GetProperty("success").GetBoolean());

            var groupItems = readGroupJson.RootElement.GetProperty("shape").GetProperty("groupItems").EnumerateArray().ToList();
            Assert.Equal(2, groupItems.Count);

            var memberNames = groupItems
                .Select(item => item.GetProperty("name").GetString())
                .Where(name => name is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Contains(firstShape, memberNames);
            Assert.Contains(secondShape, memberNames);

            var (ungroupResult, ungroupJson) = await CliProcessHelper.RunJsonAsync(
                $"shape ungroup --session {sessionId} --page-index 1 --shape-name \"{groupName}\"");
            output.WriteLine($"shape ungroup: {ungroupResult.Stdout}");

            Assert.Equal(0, ungroupResult.ExitCode);
            Assert.True(ungroupJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listAfterUngroupJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-groups --session {sessionId} --page-index 1");
            Assert.Empty(listAfterUngroupJson.RootElement.GetProperty("shapes").EnumerateArray());
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
