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
public sealed class ShapeConnectorCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShapeConnectorListAndRead_RoundTripsTopology()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeConnectorTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var startShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var endShape = await AddRectangleShapeAsync(sessionId, 288, 72, 144, 72);

            var (addConnectorResult, addConnectorJson) = await CliProcessHelper.RunJsonAsync(
                $"shape add-connector --session {sessionId} --page-index 1 --connector-type 1 --start-shape-name \"{startShape}\" --end-shape-name \"{endShape}\"");
            output.WriteLine($"shape add-connector: {addConnectorResult.Stdout}");

            Assert.Equal(0, addConnectorResult.ExitCode);
            Assert.True(addConnectorJson.RootElement.GetProperty("success").GetBoolean());

            var (listResult, listJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list-connectors --session {sessionId} --page-index 1");
            output.WriteLine($"shape list-connectors: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var connector = listJson.RootElement.GetProperty("connectors").EnumerateArray()
                .First(item =>
                    string.Equals(item.GetProperty("startShapeName").GetString(), startShape, StringComparison.Ordinal)
                    && string.Equals(item.GetProperty("endShapeName").GetString(), endShape, StringComparison.Ordinal));

            var connectorName = connector.GetProperty("name").GetString();
            Assert.False(string.IsNullOrWhiteSpace(connectorName));

            var (readResult, readJson) = await CliProcessHelper.RunJsonAsync(
                $"shape read-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\"");
            output.WriteLine($"shape read-connector: {readResult.Stdout}");

            Assert.Equal(0, readResult.ExitCode);
            Assert.True(readJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(connectorName, readJson.RootElement.GetProperty("connector").GetProperty("name").GetString());
            Assert.Equal(startShape, readJson.RootElement.GetProperty("connector").GetProperty("startShapeName").GetString());
            Assert.Equal(endShape, readJson.RootElement.GetProperty("connector").GetProperty("endShapeName").GetString());
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    [Fact]
    public async Task ShapeConnectorDisconnectAndReconnect_RoundTripsEndpoints()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliShapeConnectorReconnectTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var startShape = await AddRectangleShapeAsync(sessionId, 72, 72, 144, 72);
            var endShape = await AddRectangleShapeAsync(sessionId, 288, 72, 144, 72);
            var replacementStart = await AddRectangleShapeAsync(sessionId, 72, 216, 144, 72);
            var replacementEnd = await AddRectangleShapeAsync(sessionId, 288, 216, 144, 72);

            var (_, addConnectorJson) = await CliProcessHelper.RunJsonAsync(
                $"shape add-connector --session {sessionId} --page-index 1 --connector-type 1 --start-shape-name \"{startShape}\" --end-shape-name \"{endShape}\"");
            Assert.True(addConnectorJson.RootElement.GetProperty("success").GetBoolean());

            var connectorName = await FindConnectorNameAsync(sessionId, startShape, endShape);

            var (_, disconnectStartJson) = await CliProcessHelper.RunJsonAsync(
                $"shape disconnect-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\" --connector-end start");
            Assert.True(disconnectStartJson.RootElement.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(disconnectStartJson.RootElement.GetProperty("connector"), null, endShape);

            var (_, reconnectStartJson) = await CliProcessHelper.RunJsonAsync(
                $"shape reconnect-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\" --connector-end start --target-shape-name \"{replacementStart}\"");
            Assert.True(reconnectStartJson.RootElement.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(reconnectStartJson.RootElement.GetProperty("connector"), replacementStart, endShape);

            var (_, disconnectEndJson) = await CliProcessHelper.RunJsonAsync(
                $"shape disconnect-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\" --connector-end end");
            Assert.True(disconnectEndJson.RootElement.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(disconnectEndJson.RootElement.GetProperty("connector"), replacementStart, null);

            var (_, reconnectEndJson) = await CliProcessHelper.RunJsonAsync(
                $"shape reconnect-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\" --connector-end end --target-shape-name \"{replacementEnd}\"");
            Assert.True(reconnectEndJson.RootElement.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(reconnectEndJson.RootElement.GetProperty("connector"), replacementStart, replacementEnd);

            var (_, readJson) = await CliProcessHelper.RunJsonAsync(
                $"shape read-connector --session {sessionId} --page-index 1 --shape-name \"{connectorName}\"");
            Assert.True(readJson.RootElement.GetProperty("success").GetBoolean());
            AssertConnectorEndpoints(readJson.RootElement.GetProperty("connector"), replacementStart, replacementEnd);
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
        var (_, beforeJson) = await CliProcessHelper.RunJsonAsync(
            $"shape list --session {sessionId} --page-index 1");
        var beforeNames = beforeJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (addResult, addJson) = await CliProcessHelper.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left {left} --top {top} --width {width} --height {height}");

        Assert.Equal(0, addResult.ExitCode);
        Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

        var (_, afterJson) = await CliProcessHelper.RunJsonAsync(
            $"shape list --session {sessionId} --page-index 1");
        return afterJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static async Task<string> FindConnectorNameAsync(string sessionId, string? expectedStartShapeName, string? expectedEndShapeName)
    {
        var (_, listJson) = await CliProcessHelper.RunJsonAsync(
            $"shape list-connectors --session {sessionId} --page-index 1");

        return listJson.RootElement.GetProperty("connectors").EnumerateArray()
            .First(item =>
                string.Equals(GetOptionalString(item, "startShapeName"), expectedStartShapeName, StringComparison.Ordinal)
                && string.Equals(GetOptionalString(item, "endShapeName"), expectedEndShapeName, StringComparison.Ordinal))
            .GetProperty("name")
            .GetString()!;
    }

    private static void AssertConnectorEndpoints(JsonElement connectorJson, string? expectedStartShapeName, string? expectedEndShapeName)
    {
        Assert.Equal(expectedStartShapeName, GetOptionalString(connectorJson, "startShapeName"));
        Assert.Equal(expectedEndShapeName, GetOptionalString(connectorJson, "endShapeName"));
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        string? value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
