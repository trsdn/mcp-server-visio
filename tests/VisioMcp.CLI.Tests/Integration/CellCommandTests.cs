using System.Globalization;
using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

/// <summary>
/// Integration tests for the Visio cell/ShapeSheet workflow.
/// Verifies shape-scoped cell read/write/list operations through the generated CLI.
/// </summary>
[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Cell")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class CellCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CellReadWriteAndFormula_WorksOnShape()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliCellCommandTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var shapeName = await CreateShapeAsync(sessionId);

            var (readBeforeResult, readBeforeJson) = await CliProcessHelper.RunJsonAsync(
                $"cell read --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --cell-name Width");
            output.WriteLine($"cell read before: {readBeforeResult.Stdout}");

            Assert.Equal(0, readBeforeResult.ExitCode);
            Assert.True(readBeforeJson.RootElement.GetProperty("success").GetBoolean());

            var beforeValue = ParseCellValue(readBeforeJson.RootElement);
            Assert.True(beforeValue > 0);

            var (writeResult, writeJson) = await CliProcessHelper.RunJsonAsync(
                $"cell write --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --cell-name Width --value 3");
            output.WriteLine($"cell write: {writeResult.Stdout}");

            Assert.Equal(0, writeResult.ExitCode);
            Assert.True(writeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, readAfterJson) = await CliProcessHelper.RunJsonAsync(
                $"cell read --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --cell-name Width");
            var afterValue = ParseCellValue(readAfterJson.RootElement);
            Assert.Equal(3d, afterValue, 3);

            var (formulaSetResult, formulaSetJson) = await CliProcessHelper.RunJsonAsync(
                $"cell set-formula --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --cell-name Height --formula \"4 in\"");
            output.WriteLine($"cell set-formula: {formulaSetResult.Stdout}");

            Assert.Equal(0, formulaSetResult.ExitCode);
            Assert.True(formulaSetJson.RootElement.GetProperty("success").GetBoolean());

            var (_, formulaReadJson) = await CliProcessHelper.RunJsonAsync(
                $"cell read-formula --session {sessionId} --page-index 1 --shape-name \"{shapeName}\" --cell-name Height");

            Assert.True(formulaReadJson.RootElement.GetProperty("success").GetBoolean());
            var formula = formulaReadJson.RootElement.GetProperty("cell").GetProperty("formula").GetString();
            Assert.NotNull(formula);
            Assert.Contains("4", formula!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    [Fact]
    public async Task CellList_IncludesCommonGeometryCells()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliCellListTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var shapeName = await CreateShapeAsync(sessionId);

            var (listResult, listJson) = await CliProcessHelper.RunJsonAsync(
                $"cell list --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
            output.WriteLine($"cell list: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var cellNames = listJson.RootElement
                .GetProperty("cells")
                .EnumerateArray()
                .Select(cell => cell.GetProperty("cellName").GetString())
                .Where(name => name is not null)
                .ToList();

            Assert.Contains("Width", cellNames);
            Assert.Contains("Height", cellNames);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        var (result, json) = await CliProcessHelper.RunJsonAsync($"session create \"{filePath}\"");

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task<string> CreateShapeAsync(string sessionId)
    {
        var (_, listBeforeJson) = await CliProcessHelper.RunJsonAsync(
            $"shape list --session {sessionId} --page-index 1");
        var beforeShapes = listBeforeJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();

        var (_, addJson) = await CliProcessHelper.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72");
        Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

        var (_, listAfterJson) = await CliProcessHelper.RunJsonAsync(
            $"shape list --session {sessionId} --page-index 1");
        var afterShapes = listAfterJson.RootElement.GetProperty("shapes").EnumerateArray().ToList();

        return afterShapes
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeShapes.Select(shape => shape.GetProperty("name").GetString()))
            .First()!;
    }

    private static double ParseCellValue(JsonElement root)
    {
        var valueText = root.GetProperty("cell").GetProperty("value").GetString();
        Assert.NotNull(valueText);
        return double.Parse(valueText!, CultureInfo.InvariantCulture);
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false");
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
