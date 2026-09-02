using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

/// <summary>
/// Integration tests for listing stencil masters and dropping them onto a Visio page.
/// </summary>
[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Master")]
[Trait("RequiresVisio", "true")]
[Trait("Speed", "Medium")]
public sealed class StencilCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StencilListMastersAndDropMaster_WorksWithInstalledStencil()
    {
        var stencilPath = FindInstalledStencilPath();
        var filePath = Path.Join(Path.GetTempPath(), $"CliStencilCommandTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (listResult, listJson) = await CliProcessHelper.RunJsonAsync(
                $"stencil list-masters --session {sessionId} --stencil-path \"{stencilPath}\"");
            output.WriteLine($"stencil list-masters: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var masters = listJson.RootElement.GetProperty("masters").EnumerateArray().ToList();
            Assert.NotEmpty(masters);

            var masterName = masters
                .Select(master => master.GetProperty("name").GetString())
                .First(name => !string.IsNullOrWhiteSpace(name))!;

            var (_, beforeJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list --session {sessionId} --page-index 1");
            var beforeCount = beforeJson.RootElement.GetProperty("shapes").GetArrayLength();

            var (dropResult, dropJson) = await CliProcessHelper.RunJsonAsync(
                $"stencil drop-master --session {sessionId} --page-index 1 --stencil-path \"{stencilPath}\" --master-name \"{masterName}\" --x-position 144 --y-position 144");
            output.WriteLine($"stencil drop-master: {dropResult.Stdout}");

            Assert.Equal(0, dropResult.ExitCode);
            Assert.True(dropJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterJson) = await CliProcessHelper.RunJsonAsync(
                $"shape list --session {sessionId} --page-index 1");
            var afterCount = afterJson.RootElement.GetProperty("shapes").GetArrayLength();

            Assert.True(afterCount > beforeCount);
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

    private static string FindInstalledStencilPath()
    {
        var roots = new[]
        {
            @"C:\Program Files\Microsoft Office\root\Office16\Visio Content",
            @"C:\Program Files (x86)\Microsoft Office\root\Office16\Visio Content",
            @"C:\Program Files\Microsoft Office\Office16\Visio Content",
            @"C:\Program Files (x86)\Microsoft Office\Office16\Visio Content"
        };

        var preferredFiles = new[] { "BASIC_M.VSSX", "BLOCK_M.VSSX", "BASICORGCHART_M.VSSX" };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var preferredFile in preferredFiles)
            {
                var match = Directory.EnumerateFiles(root, preferredFile, SearchOption.AllDirectories).FirstOrDefault();
                if (match is not null)
                {
                    return match;
                }
            }
        }

        throw new InvalidOperationException("Could not find an installed Visio stencil file for stencil integration tests.");
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
