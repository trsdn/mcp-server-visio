using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

/// <summary>
/// Integration tests for the generated page CLI command surface.
/// Verifies a real Visio workflow: create session, create page, list pages, rename page.
/// </summary>
[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Page")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class PageCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PageCreateAndList_ReturnsNewPage()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliPageCommandTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (createResult, createJson) = await CliProcessHelper.RunJsonAsync(
                $"page create --session {sessionId} --position 0 --name Overview");
            output.WriteLine($"page create: {createResult.Stdout}");

            Assert.Equal(0, createResult.ExitCode);
            Assert.True(createJson.RootElement.GetProperty("success").GetBoolean());

            var (listResult, listJson) = await CliProcessHelper.RunJsonAsync($"page list --session {sessionId}");
            output.WriteLine($"page list: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var pages = listJson.RootElement.GetProperty("pages").EnumerateArray().ToList();
            Assert.Contains(pages, page => page.GetProperty("name").GetString() == "Overview");
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    [Fact]
    public async Task PageSetName_UpdatesListedPageName()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliPageRenameTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (_, createJson) = await CliProcessHelper.RunJsonAsync(
                $"page create --session {sessionId} --position 0 --name Draft");
            Assert.True(createJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listBeforeJson) = await CliProcessHelper.RunJsonAsync($"page list --session {sessionId}");
            var draftPage = listBeforeJson.RootElement
                .GetProperty("pages")
                .EnumerateArray()
                .First(page => page.GetProperty("name").GetString() == "Draft");
            var pageIndex = draftPage.GetProperty("pageIndex").GetInt32();

            var (renameResult, renameJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-name --session {sessionId} --page-index {pageIndex} --name Final");
            output.WriteLine($"page set-name: {renameResult.Stdout}");

            Assert.Equal(0, renameResult.ExitCode);
            Assert.True(renameJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listAfterJson) = await CliProcessHelper.RunJsonAsync($"page list --session {sessionId}");
            var pageNames = listAfterJson.RootElement
                .GetProperty("pages")
                .EnumerateArray()
                .Select(page => page.GetProperty("name").GetString())
                .ToList();

            Assert.Contains("Final", pageNames);
            Assert.DoesNotContain("Draft", pageNames);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    [Fact]
    public async Task PageGuides_AddMoveAndDelete_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliPageGuideTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (addResult, addJson) = await CliProcessHelper.RunJsonAsync(
                $"page add-guide --session {sessionId} --page-index 1 --guide-type 2 --x-position 0 --y-position 144");
            output.WriteLine($"page add-guide: {addResult.Stdout}");

            Assert.Equal(0, addResult.ExitCode);
            Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listJson) = await CliProcessHelper.RunJsonAsync(
                $"page list-guides --session {sessionId} --page-index 1");
            var guide = listJson.RootElement.GetProperty("guides").EnumerateArray()
                .Single(item => item.GetProperty("guideTypeName").GetString() == "horizontal");
            var guideName = guide.GetProperty("name").GetString();

            Assert.NotNull(guideName);
            Assert.Equal(144f, guide.GetProperty("y").GetSingle(), 1);

            var (moveResult, moveJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-guide-position --session {sessionId} --page-index 1 --guide-name \"{guideName}\" --x-position 0 --y-position 216");
            output.WriteLine($"page set-guide-position: {moveResult.Stdout}");

            Assert.Equal(0, moveResult.ExitCode);
            Assert.True(moveJson.RootElement.GetProperty("success").GetBoolean());

            var (_, movedListJson) = await CliProcessHelper.RunJsonAsync(
                $"page list-guides --session {sessionId} --page-index 1");
            var movedGuide = movedListJson.RootElement.GetProperty("guides").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == guideName);
            Assert.Equal(216f, movedGuide.GetProperty("y").GetSingle(), 1);

            var (deleteResult, deleteJson) = await CliProcessHelper.RunJsonAsync(
                $"page delete-guide --session {sessionId} --page-index 1 --guide-name \"{guideName}\"");
            output.WriteLine($"page delete-guide: {deleteResult.Stdout}");

            Assert.Equal(0, deleteResult.ExitCode);
            Assert.True(deleteJson.RootElement.GetProperty("success").GetBoolean());

            var (_, finalListJson) = await CliProcessHelper.RunJsonAsync(
                $"page list-guides --session {sessionId} --page-index 1");
            Assert.DoesNotContain(
                finalListJson.RootElement.GetProperty("guides").EnumerateArray(),
                item => item.GetProperty("name").GetString() == guideName);
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    [Fact]
    public async Task PageRoutingSettings_ReadAndUpdate_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliPageRoutingTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);

            var (_, initialJson) = await CliProcessHelper.RunJsonAsync(
                $"page get-routing-settings --session {sessionId} --page-index 1");
            var initial = initialJson.RootElement;

            var (_, routeJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-route-style --session {sessionId} --page-index 1 --route-style 1");
            Assert.True(routeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, extJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-connector-routing-extension --session {sessionId} --page-index 1 --connector-routing-extension 2");
            Assert.True(extJson.RootElement.GetProperty("success").GetBoolean());

            var (_, jumpCodeJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-line-jump-code --session {sessionId} --page-index 1 --line-jump-code 6");
            Assert.True(jumpCodeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, jumpStyleJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-line-jump-style --session {sessionId} --page-index 1 --line-jump-style 1");
            Assert.True(jumpStyleJson.RootElement.GetProperty("success").GetBoolean());

            var (_, walkJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-walk-preference --session {sessionId} --page-index 1 --walk-preference 1");
            Assert.True(walkJson.RootElement.GetProperty("success").GetBoolean());

            var (_, placeJson) = await CliProcessHelper.RunJsonAsync(
                $"page set-place-style --session {sessionId} --page-index 1 --place-style 1");
            Assert.True(placeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, currentJson) = await CliProcessHelper.RunJsonAsync(
                $"page get-routing-settings --session {sessionId} --page-index 1");
            var current = currentJson.RootElement;

            Assert.Equal(1, current.GetProperty("routeStyle").GetInt32());
            Assert.Equal(2, current.GetProperty("connectorRoutingExtension").GetInt32());
            Assert.Equal(6, current.GetProperty("lineJumpCode").GetInt32());
            Assert.Equal(1, current.GetProperty("lineJumpStyle").GetInt32());
            Assert.Equal(1, current.GetProperty("walkPreference").GetInt32());
            Assert.Equal(1, current.GetProperty("placeStyle").GetInt32());

            await CliProcessHelper.RunJsonAsync(
                $"page set-route-style --session {sessionId} --page-index 1 --route-style {initial.GetProperty("routeStyle").GetInt32()}");
            await CliProcessHelper.RunJsonAsync(
                $"page set-connector-routing-extension --session {sessionId} --page-index 1 --connector-routing-extension {initial.GetProperty("connectorRoutingExtension").GetInt32()}");
            await CliProcessHelper.RunJsonAsync(
                $"page set-line-jump-code --session {sessionId} --page-index 1 --line-jump-code {initial.GetProperty("lineJumpCode").GetInt32()}");
            await CliProcessHelper.RunJsonAsync(
                $"page set-line-jump-style --session {sessionId} --page-index 1 --line-jump-style {initial.GetProperty("lineJumpStyle").GetInt32()}");
            await CliProcessHelper.RunJsonAsync(
                $"page set-walk-preference --session {sessionId} --page-index 1 --walk-preference {initial.GetProperty("walkPreference").GetInt32()}");
            await CliProcessHelper.RunJsonAsync(
                $"page set-place-style --session {sessionId} --page-index 1 --place-style {initial.GetProperty("placeStyle").GetInt32()}");
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        await EnsureNoActiveSessionsAsync();

        var (result, json) = await CliProcessHelper.RunJsonAsync($"session create \"{filePath}\"");

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false");
        }

        await EnsureNoActiveSessionsAsync();

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static async Task EnsureNoActiveSessionsAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await CliProcessHelper.RunAsync("session list", timeoutMs: 45000);
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Stdout))
            {
                return;
            }

            JsonDocument? json = null;
            try
            {
                json = JsonDocument.Parse(result.Stdout);

                if (!json.RootElement.TryGetProperty("sessions", out var sessions) || sessions.ValueKind != JsonValueKind.Array)
                {
                    return;
                }

                var sessionIds = sessions.EnumerateArray()
                    .Select(session => session.GetProperty("sessionId").GetString())
                    .Where(sessionId => !string.IsNullOrWhiteSpace(sessionId))
                    .Cast<string>()
                    .ToArray();

                if (sessionIds.Length == 0)
                {
                    return;
                }

                foreach (var activeSessionId in sessionIds)
                {
                    await CliProcessHelper.RunAsync($"session close --session {activeSessionId} --save false", timeoutMs: 120000);
                }
            }
            catch (JsonException)
            {
                return;
            }
            finally
            {
                json?.Dispose();
            }

            await Task.Delay(500);
        }
    }
}
