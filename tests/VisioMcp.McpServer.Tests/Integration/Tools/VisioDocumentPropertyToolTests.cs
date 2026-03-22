using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "DocumentProperty")]
public sealed class VisioDocumentPropertyToolTests(ITestOutputHelper output)
{
    [Fact]
    public void DocumentPropertyBuiltInAndCustom_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioDocpropertyToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);

            var setResult = VisioDocpropertyTool.VisioDocproperty(
                action: DocpropertyAction.SetAll,
                session_id: sessionId,
                title: "Visio Tool",
                subject: "Metadata",
                author: "Copilot",
                keywords: "mcp,metadata",
                comments: "tool roundtrip",
                company: "GitHub",
                category: "Integration",
                property_name: null,
                property_value: null);
            output.WriteLine(setResult);

            var setJson = JsonDocument.Parse(setResult).RootElement;
            Assert.True(setJson.GetProperty("success").GetBoolean());

            var getResult = VisioDocpropertyTool.VisioDocproperty(
                action: DocpropertyAction.GetAll,
                session_id: sessionId,
                title: null,
                subject: null,
                author: null,
                keywords: null,
                comments: null,
                company: null,
                category: null,
                property_name: null,
                property_value: null);
            output.WriteLine(getResult);

            var getJson = JsonDocument.Parse(getResult).RootElement;
            Assert.True(getJson.GetProperty("success").GetBoolean());
            Assert.Equal("Visio Tool", getJson.GetProperty("title").GetString());
            Assert.Equal("Metadata", getJson.GetProperty("subject").GetString());
            Assert.Equal("Copilot", getJson.GetProperty("author").GetString());
            Assert.Equal("mcp,metadata", getJson.GetProperty("keywords").GetString());
            Assert.Equal("tool roundtrip", getJson.GetProperty("comments").GetString());
            Assert.Equal("GitHub", getJson.GetProperty("company").GetString());
            Assert.Equal("Integration", getJson.GetProperty("category").GetString());

            const string propertyName = "Owner Team";
            const string propertyValue = "Platform Ops";

            var setCustomResult = VisioDocpropertyTool.VisioDocproperty(
                action: DocpropertyAction.SetCustom,
                session_id: sessionId,
                title: null,
                subject: null,
                author: null,
                keywords: null,
                comments: null,
                company: null,
                category: null,
                property_name: propertyName,
                property_value: propertyValue);
            output.WriteLine(setCustomResult);

            var setCustomJson = JsonDocument.Parse(setCustomResult).RootElement;
            Assert.True(setCustomJson.GetProperty("success").GetBoolean());

            var getCustomResult = VisioDocpropertyTool.VisioDocproperty(
                action: DocpropertyAction.GetCustom,
                session_id: sessionId,
                title: null,
                subject: null,
                author: null,
                keywords: null,
                comments: null,
                company: null,
                category: null,
                property_name: propertyName,
                property_value: null);
            output.WriteLine(getCustomResult);

            var getCustomJson = JsonDocument.Parse(getCustomResult).RootElement;
            Assert.True(getCustomJson.GetProperty("success").GetBoolean());
            Assert.Contains(propertyName, getCustomJson.GetProperty("message").GetString(), StringComparison.Ordinal);
            Assert.Contains(propertyValue, getCustomJson.GetProperty("message").GetString(), StringComparison.Ordinal);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    private static string CreateSession(string path)
    {
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Create,
            path: path,
            session_id: null,
            page_name: null,
            page_index: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        var json = JsonDocument.Parse(result).RootElement;
        Assert.True(json.GetProperty("success").GetBoolean());
        return json.GetProperty("session_id").GetString()!;
    }

    private static void CloseSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        VisioFileTool.VisioFile(
            VisioFileAction.Close,
            path: null,
            session_id: sessionId,
            page_name: null,
            page_index: null,
            save: false,
            show: false,
            timeout_seconds: 300);
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
