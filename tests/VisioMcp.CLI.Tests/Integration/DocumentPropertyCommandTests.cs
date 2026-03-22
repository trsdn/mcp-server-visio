using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "DocumentProperty")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class DocumentPropertyCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DocumentPropertyBuiltInAndCustom_RoundTripsMetadata()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliDocumentPropertyTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (setResult, setJson) = await CliProcessHelper.RunJsonAsync(
                $"docproperty set --session {sessionId} --title \"Visio Native\" --subject \"Metadata\" --author \"Copilot\" --keywords \"visio,docproperty\" --comments \"cli roundtrip\" --company \"GitHub\" --category \"Integration\"");
            output.WriteLine($"docproperty set: {setResult.Stdout}");

            Assert.Equal(0, setResult.ExitCode);
            Assert.True(setJson.RootElement.GetProperty("success").GetBoolean());

            var (getResult, getJson) = await CliProcessHelper.RunJsonAsync($"docproperty get --session {sessionId}");
            output.WriteLine($"docproperty get: {getResult.Stdout}");

            Assert.Equal(0, getResult.ExitCode);
            Assert.True(getJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Visio Native", getJson.RootElement.GetProperty("title").GetString());
            Assert.Equal("Metadata", getJson.RootElement.GetProperty("subject").GetString());
            Assert.Equal("Copilot", getJson.RootElement.GetProperty("author").GetString());
            Assert.Equal("visio,docproperty", getJson.RootElement.GetProperty("keywords").GetString());
            Assert.Equal("cli roundtrip", getJson.RootElement.GetProperty("comments").GetString());
            Assert.Equal("GitHub", getJson.RootElement.GetProperty("company").GetString());
            Assert.Equal("Integration", getJson.RootElement.GetProperty("category").GetString());

            const string propertyName = "Owner Team";
            const string propertyValue = "Platform Ops";

            var (setCustomResult, setCustomJson) = await CliProcessHelper.RunJsonAsync(
                $"docproperty set-custom --session {sessionId} --property-name \"{propertyName}\" --property-value \"{propertyValue}\"");
            output.WriteLine($"docproperty set-custom: {setCustomResult.Stdout}");

            Assert.Equal(0, setCustomResult.ExitCode);
            Assert.True(setCustomJson.RootElement.GetProperty("success").GetBoolean());

            var (getCustomResult, getCustomJson) = await CliProcessHelper.RunJsonAsync(
                $"docproperty get-custom --session {sessionId} --property-name \"{propertyName}\"");
            output.WriteLine($"docproperty get-custom: {getCustomResult.Stdout}");

            Assert.Equal(0, getCustomResult.ExitCode);
            Assert.True(getCustomJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains(propertyName, getCustomJson.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
            Assert.Contains(propertyValue, getCustomJson.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
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
