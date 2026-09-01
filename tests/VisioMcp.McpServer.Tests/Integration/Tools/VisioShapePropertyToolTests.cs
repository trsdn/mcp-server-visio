using System.Text.Json;
using VisioMcp.Generated;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "Shape")]
public sealed class VisioShapePropertyToolTests(ITestOutputHelper output)
{
    [Fact]
    public void ShapeProperty_SetGetListDelete_ReturnJsonSuccess()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"PptShapePropertyToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            sessionId = CreateSession(tempPath);
            var shapeName = AddBasicShape(sessionId);
            const string propertyName = "Cost Center";
            const string propertyValue = "IT-42";

            var setResult = InvokeShape(
                ShapeAction.SetProperty,
                sessionId,
                shape_name: shapeName,
                property_name: propertyName,
                property_value: propertyValue);
            output.WriteLine(setResult);

            var setJson = JsonDocument.Parse(setResult).RootElement;
            Assert.True(setJson.GetProperty("success").GetBoolean());

            var getResult = InvokeShape(
                ShapeAction.GetProperty,
                sessionId,
                shape_name: shapeName,
                property_name: propertyName);
            output.WriteLine(getResult);

            var getJson = JsonDocument.Parse(getResult).RootElement;
            Assert.True(getJson.GetProperty("success").GetBoolean());
            Assert.Equal(propertyName, getJson.GetProperty("property").GetProperty("propertyName").GetString());
            Assert.Equal(propertyValue, getJson.GetProperty("property").GetProperty("propertyValue").GetString());

            var listResult = InvokeShape(
                ShapeAction.ListProperties,
                sessionId,
                shape_name: shapeName);
            output.WriteLine(listResult);

            var listJson = JsonDocument.Parse(listResult).RootElement;
            Assert.True(listJson.GetProperty("success").GetBoolean());

            var property = listJson.GetProperty("properties").EnumerateArray()
                .First(item => string.Equals(item.GetProperty("propertyName").GetString(), propertyName, StringComparison.Ordinal));
            Assert.Equal(propertyValue, property.GetProperty("propertyValue").GetString());

            var deleteResult = InvokeShape(
                ShapeAction.DeleteProperty,
                sessionId,
                shape_name: shapeName,
                property_name: propertyName);
            output.WriteLine(deleteResult);

            var deleteJson = JsonDocument.Parse(deleteResult).RootElement;
            Assert.True(deleteJson.GetProperty("success").GetBoolean());

            var listAfterDeleteResult = InvokeShape(
                ShapeAction.ListProperties,
                sessionId,
                shape_name: shapeName);
            var listAfterDeleteJson = JsonDocument.Parse(listAfterDeleteResult).RootElement;
            var propertyNames = listAfterDeleteJson.GetProperty("properties").EnumerateArray()
                .Select(item => item.GetProperty("propertyName").GetString())
                .Where(name => name is not null)
                .ToList();

            Assert.DoesNotContain(propertyName, propertyNames);
        }
        finally
        {
            CloseSession(sessionId);
            DeleteFile(tempPath);
        }
    }

    private static string InvokeShape(
        ShapeAction action,
        string sessionId,
        string? shape_name = null,
        string? property_name = null,
        string? property_value = null)
    {
        return VisioShapeTool.VisioShape(
            action: action,
            session_id: sessionId,
            page_index: 1,
            shape_name: shape_name,
            left: null,
            top: null,
            width: null,
            height: null,
            text: null,
            auto_shape_type: 0,
            z_order_cmd: 0,
            shape_names: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            target_shape_name: null,
            property_name: property_name,
            property_value: property_value,
            connector_end: null);
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

    private static string AddBasicShape(string sessionId)
    {
        var beforeJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
        var beforeNames = beforeJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createResult = VisioShapeTool.VisioShape(
            action: ShapeAction.AddShape,
            session_id: sessionId,
            page_index: 1,
            shape_name: null,
            left: 72f,
            top: 72f,
            width: 144f,
            height: 72f,
            text: null,
            auto_shape_type: 1,
            z_order_cmd: 0,
            shape_names: null,
            connector_type: 0,
            start_shape_name: null,
            end_shape_name: null,
            merge_type: 0,
            target_shape_name: null,
            property_name: null,
            property_value: null,
            connector_end: null);
        var createJson = JsonDocument.Parse(createResult).RootElement;
        Assert.True(createJson.GetProperty("success").GetBoolean());

        var afterJson = JsonDocument.Parse(InvokeShape(ShapeAction.List, sessionId)).RootElement;
        return afterJson.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private static void CloseSession(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
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
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
