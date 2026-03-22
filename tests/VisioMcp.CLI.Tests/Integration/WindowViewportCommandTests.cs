using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Collection("Service")]
[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Window")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class WindowViewportCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task WindowViewport_ZoomPanAndFit_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliWindowViewportTests_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(filePath);
            var shapeName = await AddRectangleShapeAsync(sessionId, 432, 288, 144, 72);
            var shapeCenter = await ReadShapeCenterAsync(sessionId, shapeName);

            var (setZoomResult, setZoomJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-zoom --session {sessionId} --zoom-percent 150");
            output.WriteLine($"window set-zoom: {setZoomResult.Stdout}");
            Assert.Equal(0, setZoomResult.ExitCode);
            Assert.True(setZoomJson.RootElement.GetProperty("success").GetBoolean());

            var (_, zoomJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-zoom --session {sessionId} --page-index 1");
            Assert.Equal(150f, zoomJson.RootElement.GetProperty("zoomPercent").GetSingle(), 1);

            var (_, beforePanJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-viewport --session {sessionId} --page-index 1");
            var beforeCenter = ReadCenter(beforePanJson.RootElement);

            var (panToShapeResult, panToShapeJson) = await CliProcessHelper.RunJsonAsync(
                $"window pan-to-shape --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
            output.WriteLine($"window pan-to-shape: {panToShapeResult.Stdout}");
            Assert.Equal(0, panToShapeResult.ExitCode);
            Assert.True(panToShapeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterPanJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-viewport --session {sessionId} --page-index 1");
            var afterCenter = ReadCenter(afterPanJson.RootElement);
            Assert.True(Distance(afterCenter, shapeCenter) < Distance(beforeCenter, shapeCenter));

            var (panOffsetResult, panOffsetJson) = await CliProcessHelper.RunJsonAsync(
                $"window pan-by-offset --session {sessionId} --page-index 1 --offset-x 72 --offset-y 0");
            output.WriteLine($"window pan-by-offset: {panOffsetResult.Stdout}");
            Assert.Equal(0, panOffsetResult.ExitCode);
            Assert.True(panOffsetJson.RootElement.GetProperty("success").GetBoolean());

            var (_, afterOffsetJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-viewport --session {sessionId} --page-index 1");
            var afterOffsetCenter = ReadCenter(afterOffsetJson.RootElement);
            Assert.NotEqual(afterPanJson.RootElement.GetProperty("centerX").GetSingle(), afterOffsetCenter.X);

            var (selectResult, selectJson) = await CliProcessHelper.RunJsonAsync(
                $"shape select-shapes --session {sessionId} --page-index 1 --shape-names \"{shapeName}\"");
            output.WriteLine($"shape select-shapes: {selectResult.Stdout}");
            Assert.Equal(0, selectResult.ExitCode);
            Assert.True(selectJson.RootElement.GetProperty("success").GetBoolean());

            var (fitSelectionResult, fitSelectionJson) = await CliProcessHelper.RunJsonAsync(
                $"window fit-selection --session {sessionId} --page-index 1");
            output.WriteLine($"window fit-selection: {fitSelectionResult.Stdout}");
            Assert.Equal(0, fitSelectionResult.ExitCode);
            Assert.True(fitSelectionJson.RootElement.GetProperty("success").GetBoolean());

            var (fitPageResult, fitPageJson) = await CliProcessHelper.RunJsonAsync(
                $"window fit-page --session {sessionId} --page-index 1");
            output.WriteLine($"window fit-page: {fitPageResult.Stdout}");
            Assert.Equal(0, fitPageResult.ExitCode);
            Assert.True(fitPageJson.RootElement.GetProperty("success").GetBoolean());

            var (_, fittedViewportJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-viewport --session {sessionId} --page-index 1");
            Assert.Equal(1, fittedViewportJson.RootElement.GetProperty("viewFit").GetInt32());
            Assert.True(fittedViewportJson.RootElement.GetProperty("width").GetSingle() > 0);
            Assert.True(fittedViewportJson.RootElement.GetProperty("height").GetSingle() > 0);

            var (_, initialAidsJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-visual-aids --session {sessionId} --page-index 1");
            var initialAids = initialAidsJson.RootElement;

            var (_, gridOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-grid-visible --session {sessionId} --page-index 1 --visible false");
            Assert.True(gridOffJson.RootElement.GetProperty("success").GetBoolean());

            var (_, guidesOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-guides-visible --session {sessionId} --page-index 1 --visible false");
            Assert.True(guidesOffJson.RootElement.GetProperty("success").GetBoolean());

            var (_, rulersOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-rulers-visible --session {sessionId} --page-index 1 --visible false");
            Assert.True(rulersOffJson.RootElement.GetProperty("success").GetBoolean());

            var (_, aidsOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-drawing-aids --session {sessionId} --enabled false");
            Assert.True(aidsOffJson.RootElement.GetProperty("success").GetBoolean());

            var (_, visualAidsOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-visual-aids --session {sessionId} --page-index 1");
            Assert.False(visualAidsOffJson.RootElement.GetProperty("gridVisible").GetBoolean());
            Assert.False(visualAidsOffJson.RootElement.GetProperty("guidesVisible").GetBoolean());
            Assert.False(visualAidsOffJson.RootElement.GetProperty("rulersVisible").GetBoolean());
            Assert.False(visualAidsOffJson.RootElement.GetProperty("drawingAidsEnabled").GetBoolean());

            var (_, gridRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-grid-visible --session {sessionId} --page-index 1 --visible {initialAids.GetProperty("gridVisible").GetBoolean().ToString().ToLowerInvariant()}");
            Assert.True(gridRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, guidesRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-guides-visible --session {sessionId} --page-index 1 --visible {initialAids.GetProperty("guidesVisible").GetBoolean().ToString().ToLowerInvariant()}");
            Assert.True(guidesRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, rulersRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-rulers-visible --session {sessionId} --page-index 1 --visible {initialAids.GetProperty("rulersVisible").GetBoolean().ToString().ToLowerInvariant()}");
            Assert.True(rulersRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, aidsRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-drawing-aids --session {sessionId} --enabled {initialAids.GetProperty("drawingAidsEnabled").GetBoolean().ToString().ToLowerInvariant()}");
            Assert.True(aidsRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, initialSnapJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-snap-settings --session {sessionId}");
            var initialSnap = initialSnapJson.RootElement;

            var (_, gridSnapOffJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-grid-snap-strength --session {sessionId} --strength 1");
            Assert.True(gridSnapOffJson.RootElement.GetProperty("success").GetBoolean());

            var (_, guidesSnapJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-guides-snap-strength --session {sessionId} --strength 17");
            Assert.True(guidesSnapJson.RootElement.GetProperty("success").GetBoolean());

            var (_, geometrySnapJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-geometry-snap-strength --session {sessionId} --strength 19");
            Assert.True(geometrySnapJson.RootElement.GetProperty("success").GetBoolean());

            var (_, snapSettingsJson) = await CliProcessHelper.RunJsonAsync(
                $"window get-snap-settings --session {sessionId}");
            Assert.Equal(1, snapSettingsJson.RootElement.GetProperty("gridSnapStrength").GetInt32());
            Assert.Equal(17, snapSettingsJson.RootElement.GetProperty("guidesSnapStrength").GetInt32());
            Assert.Equal(19, snapSettingsJson.RootElement.GetProperty("geometrySnapStrength").GetInt32());

            var (_, gridSnapRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-grid-snap-strength --session {sessionId} --strength {initialSnap.GetProperty("gridSnapStrength").GetInt32()}");
            Assert.True(gridSnapRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, guidesSnapRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-guides-snap-strength --session {sessionId} --strength {initialSnap.GetProperty("guidesSnapStrength").GetInt32()}");
            Assert.True(guidesSnapRestoreJson.RootElement.GetProperty("success").GetBoolean());

            var (_, geometrySnapRestoreJson) = await CliProcessHelper.RunJsonAsync(
                $"window set-geometry-snap-strength --session {sessionId} --strength {initialSnap.GetProperty("geometrySnapStrength").GetInt32()}");
            Assert.True(geometrySnapRestoreJson.RootElement.GetProperty("success").GetBoolean());
        }
        finally
        {
            await CloseSessionAsync(sessionId, filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(string filePath)
    {
        await EnsureNoActiveSessionsAsync();

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

    private static async Task<(float X, float Y)> ReadShapeCenterAsync(string sessionId, string shapeName)
    {
        var (_, readJson) = await CliProcessHelper.RunJsonAsync(
            $"shape read --session {sessionId} --page-index 1 --shape-name \"{shapeName}\"");
        var shape = readJson.RootElement.GetProperty("shape");
        return (shape.GetProperty("left").GetSingle(), shape.GetProperty("top").GetSingle());
    }

    private static (float X, float Y) ReadCenter(JsonElement viewportJson)
        => (viewportJson.GetProperty("centerX").GetSingle(), viewportJson.GetProperty("centerY").GetSingle());

    private static double Distance((float X, float Y) first, (float X, float Y) second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static async Task CloseSessionAsync(string? sessionId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await CliProcessHelper.RunAsync($"session close --session {sessionId} --save false", timeoutMs: 120000);
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
            var (result, json) = await CliProcessHelper.RunJsonAsync("session list", timeoutMs: 45000);
            try
            {
                Assert.Equal(0, result.ExitCode);

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
            finally
            {
                json.Dispose();
            }

            await Task.Delay(500);
        }
    }
}
