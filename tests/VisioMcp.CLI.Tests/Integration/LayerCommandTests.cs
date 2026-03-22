using System.Diagnostics;
using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "Layer")]
[Trait("RequiresPowerPoint", "true")]
[Trait("Speed", "Medium")]
public sealed class LayerCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Layer_CreateAssignConfigureDelete_RoundTrips()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliLayerCommandTests_{Guid.NewGuid():N}.vsdx");
        using var scope = await SessionTestScope.StartAsync(output);
        string? sessionId = null;

        try
        {
            (sessionId, _) = await CreateSessionAsync(scope, filePath);
            var shapeName = await AddRectangleShapeAsync(scope, sessionId);

            var (createResult, createJson) = await scope.RunJsonAsync(
                $"layer create --session {sessionId} --page-index 1 --layer-name Workflow --color-index 4 --visible true --printable false --locked true");
            output.WriteLine($"layer create: {createResult.Stdout}");

            Assert.Equal(0, createResult.ExitCode);
            Assert.True(createJson.RootElement.GetProperty("success").GetBoolean());

            var (listResult, listJson) = await scope.RunJsonAsync(
                $"layer list --session {sessionId} --page-index 1");
            output.WriteLine($"layer list: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var layer = listJson.RootElement.GetProperty("layers").EnumerateArray()
                .First(item => item.GetProperty("name").GetString() == "Workflow");
            Assert.Equal(4, layer.GetProperty("colorIndex").GetInt32());
            Assert.True(layer.GetProperty("visible").GetBoolean());
            Assert.False(layer.GetProperty("printable").GetBoolean());
            Assert.True(layer.GetProperty("locked").GetBoolean());

            var (addResult, addJson) = await scope.RunJsonAsync(
                $"layer add-shape --session {sessionId} --page-index 1 --layer-name Workflow --shape-name \"{shapeName}\" --preserve-membership true");
            output.WriteLine($"layer add-shape: {addResult.Stdout}");

            Assert.Equal(0, addResult.ExitCode);
            Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

            var (_, readJson) = await scope.RunJsonAsync(
                $"layer read --session {sessionId} --page-index 1 --layer-name Workflow");
            var readLayer = readJson.RootElement.GetProperty("layer");
            Assert.Equal(1, readLayer.GetProperty("memberCount").GetInt32());
            Assert.Contains(
                readLayer.GetProperty("shapeNames").EnumerateArray().Select(item => item.GetString()),
                item => string.Equals(item, shapeName, StringComparison.Ordinal));

            var (_, setVisibilityJson) = await scope.RunJsonAsync(
                $"layer set-visibility --session {sessionId} --page-index 1 --layer-name Workflow --visible false");
            Assert.True(setVisibilityJson.RootElement.GetProperty("success").GetBoolean());

            var (_, setPrintJson) = await scope.RunJsonAsync(
                $"layer set-print --session {sessionId} --page-index 1 --layer-name Workflow --printable true");
            Assert.True(setPrintJson.RootElement.GetProperty("success").GetBoolean());

            var (_, setLockJson) = await scope.RunJsonAsync(
                $"layer set-lock --session {sessionId} --page-index 1 --layer-name Workflow --locked false");
            Assert.True(setLockJson.RootElement.GetProperty("success").GetBoolean());

            var (_, setColorJson) = await scope.RunJsonAsync(
                $"layer set-color --session {sessionId} --page-index 1 --layer-name Workflow --color-index 7");
            Assert.True(setColorJson.RootElement.GetProperty("success").GetBoolean());

            var (_, readAfterConfigJson) = await scope.RunJsonAsync(
                $"layer read --session {sessionId} --page-index 1 --layer-name Workflow");
            var configuredLayer = readAfterConfigJson.RootElement.GetProperty("layer");
            Assert.False(configuredLayer.GetProperty("visible").GetBoolean());
            Assert.True(configuredLayer.GetProperty("printable").GetBoolean());
            Assert.False(configuredLayer.GetProperty("locked").GetBoolean());
            Assert.Equal(7, configuredLayer.GetProperty("colorIndex").GetInt32());

            var (_, removeJson) = await scope.RunJsonAsync(
                $"layer remove-shape --session {sessionId} --page-index 1 --layer-name Workflow --shape-name \"{shapeName}\" --preserve-membership true");
            Assert.True(removeJson.RootElement.GetProperty("success").GetBoolean());

            var (_, readAfterRemoveJson) = await scope.RunJsonAsync(
                $"layer read --session {sessionId} --page-index 1 --layer-name Workflow");
            Assert.Equal(0, readAfterRemoveJson.RootElement.GetProperty("layer").GetProperty("memberCount").GetInt32());

            var (deleteResult, deleteJson) = await scope.RunJsonAsync(
                $"layer delete --session {sessionId} --page-index 1 --layer-name Workflow");
            output.WriteLine($"layer delete: {deleteResult.Stdout}");
            Assert.True(deleteJson.RootElement.GetProperty("success").GetBoolean());

            var (_, listAfterDeleteJson) = await scope.RunJsonAsync(
                $"layer list --session {sessionId} --page-index 1");
            Assert.DoesNotContain(
                listAfterDeleteJson.RootElement.GetProperty("layers").EnumerateArray(),
                item => item.GetProperty("name").GetString() == "Workflow");
        }
        finally
        {
            await scope.CloseSessionIfPresentAsync(sessionId);
            await SessionTestScope.TryDeleteFileAsync(filePath);
        }
    }

    private static async Task<(string SessionId, JsonElement Root)> CreateSessionAsync(SessionTestScope scope, string filePath)
    {
        var (result, json) = await scope.RunJsonAsync($"session create \"{filePath}\"", timeoutMs: 120000);

        Assert.Equal(0, result.ExitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        return (json.RootElement.GetProperty("sessionId").GetString()!, json.RootElement.Clone());
    }

    private static async Task<string> AddRectangleShapeAsync(SessionTestScope scope, string sessionId)
    {
        var (_, beforeJson) = await scope.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
        var beforeNames = beforeJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (_, addJson) = await scope.RunJsonAsync(
            $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72");
        Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

        var (_, afterJson) = await scope.RunJsonAsync($"shape list --session {sessionId} --page-index 1");
        return afterJson.RootElement.GetProperty("shapes").EnumerateArray()
            .Select(shape => shape.GetProperty("name").GetString())
            .Except(beforeNames, StringComparer.OrdinalIgnoreCase)
            .First()!;
    }

    private sealed class SessionTestScope : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _pipeName = $"VisioMcp-layer-test-{Guid.NewGuid():N}";
        private readonly Dictionary<string, string> _environment;
        private Process? _daemonProcess;

        private SessionTestScope(ITestOutputHelper output)
        {
            _output = output;
            _environment = new Dictionary<string, string>
            {
                ["VisioMcp_CLI_PIPE"] = _pipeName
            };
        }

        public static async Task<SessionTestScope> StartAsync(ITestOutputHelper output)
        {
            var scope = new SessionTestScope(output);
            scope._daemonProcess = scope.StartDaemon();
            await scope.WaitForDaemonReadyAsync();
            return scope;
        }

        public async Task<CliResult> RunAsync(string args, int timeoutMs = 120000) =>
            await CliProcessHelper.RunAsync(args, timeoutMs, _environment);

        public async Task<(CliResult Result, JsonDocument Json)> RunJsonAsync(string args, int timeoutMs = 120000) =>
            await CliProcessHelper.RunJsonAsync(args, timeoutMs, _environment);

        public async Task WaitForNoActiveSessionsAsync()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var (result, json) = await RunJsonAsync("session list", timeoutMs: 45000);
                try
                {
                    if (result.ExitCode == 0 &&
                        json.RootElement.TryGetProperty("sessions", out var sessions) &&
                        sessions.ValueKind == JsonValueKind.Array &&
                        sessions.GetArrayLength() == 0)
                    {
                        return;
                    }
                }
                finally
                {
                    json.Dispose();
                }

                await Task.Delay(500);
            }
        }

        public async Task CloseSessionIfPresentAsync(string? sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await RunAsync($"session close --session {sessionId} --save false", timeoutMs: 120000);
                await WaitForNoActiveSessionsAsync();
            }
        }

        public static async Task TryDeleteFileAsync(string filePath)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    return;
                }
                catch (IOException) when (attempt < 19)
                {
                    await Task.Delay(500);
                }
                catch (IOException)
                {
                    return;
                }
            }
        }

        private Process StartDaemon()
        {
            var exePath = CliProcessHelper.GetExePath();
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"service run --pipe-name {_pipeName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)!
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();
            _output.WriteLine($"Started layer test daemon PID {process.Id}, pipe {_pipeName}");
            return process;
        }

        private async Task WaitForDaemonReadyAsync()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    var (result, json) = await RunJsonAsync("service status", timeoutMs: 5000);
                    var isRunning = result.ExitCode == 0 &&
                        json.RootElement.TryGetProperty("running", out var runningElement) &&
                        runningElement.GetBoolean();
                    json.Dispose();

                    if (isRunning)
                    {
                        return;
                    }
                }
                catch
                {
                }

                await Task.Delay(500);
            }

            throw new TimeoutException("Layer test daemon did not become ready within timeout.");
        }

        public void Dispose()
        {
            if (_daemonProcess is null)
            {
                return;
            }

            try
            {
                if (!_daemonProcess.HasExited)
                {
                    _daemonProcess.Kill(entireProcessTree: true);
                    _daemonProcess.WaitForExit(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to stop layer test daemon: {ex}");
            }
        }
    }
}
