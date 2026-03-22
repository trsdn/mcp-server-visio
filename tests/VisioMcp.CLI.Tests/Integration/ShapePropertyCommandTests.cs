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
public sealed class ShapePropertyCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeProperty_SetGetListDelete_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapePropertyTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var shapeName = await AddRectangleShapeAsync(sessionId);

            var propertyName = "Cost Center";
            var propertyValue = "IT-42";

            var (setResult, setJson) = await CliProcessHelper.RunJsonAsync(
                $"shape set-property --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --property-name \"{propertyName}\" --property-value \"{propertyValue}\"");
            output.WriteLine($"shape set-property: {setResult.Stdout}");

            Assert.Equal(0, setResult.ExitCode);
            Assert.True(setJson.RootElement.GetProperty("success").GetBoolean());

            var (getResult, getJson) = await CliProcessHelper.RunJsonAsync(
                $"shape get-property --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --property-name \"{propertyName}\"");
            output.WriteLine($"shape get-property: {getResult.Stdout}");

            Assert.Equal(0, getResult.ExitCode);
            Assert.True(getJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(propertyName, getJson.RootElement.GetProperty("property").GetProperty("propertyName").GetString());
            Assert.Equal(propertyValue, getJson.RootElement.GetProperty("property").GetProperty("propertyValue").GetString());

            var (listResult, listJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-properties --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
            output.WriteLine($"shape list-properties: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var property = listJson.RootElement.GetProperty("properties").EnumerateArray()
                .First(item => string.Equals(item.GetProperty("propertyName").GetString(), propertyName, StringComparison.Ordinal));
            Assert.Equal(propertyValue, property.GetProperty("propertyValue").GetString());

            var (deleteResult, deleteJson) = await CliProcessHelper.RunJsonAsync(
                $"shape delete-property --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --property-name \"{propertyName}\"");
            output.WriteLine($"shape delete-property: {deleteResult.Stdout}");

            Assert.Equal(0, deleteResult.ExitCode);
            Assert.True(deleteJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listAfterDeleteJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-properties --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");

            var propertyNames = listAfterDeleteJson.RootElement.GetProperty("properties").EnumerateArray()
                .Select(item => item.GetProperty("propertyName").GetString())
                .Where(name => name is not null)
                .ToList();

            Assert.DoesNotContain(propertyName, propertyNames);
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

    private static async Task<string> AddRectangleShapeAsync(string sessionId)
    {
        var (_, beforeJson) = await CliProcessHelper.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
        var beforeNames = beforeJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (_, addJson) = await CliProcessHelper.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72");
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
