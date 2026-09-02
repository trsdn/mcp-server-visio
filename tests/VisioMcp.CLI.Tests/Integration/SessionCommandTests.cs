using System.Diagnostics;
using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.CLI.Tests.Integration;

[Trait("Layer", "CLI")]
[Trait("Category", "Integration")]
[Trait("Feature", "File")]
[Trait("RequiresVisio", "true")]
[Trait("Speed", "Medium")]
public sealed class SessionCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SessionCreate_WithShow_MarksSessionVisibleInSessionList()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliSessionShowTests_{Guid.NewGuid():N}.vsdx");
        using var scope = await SessionTestScope.StartAsync(output);
        string? sessionId = null;

        try
        {
            var (createResult, createJson) = await scope.RunJsonAsync(
                $"session create \"{filePath}\" --show",
                timeoutMs: 120000);

            output.WriteLine($"session create --show: {createResult.Stdout}");

            Assert.Equal(0, createResult.ExitCode);
            Assert.True(createJson.RootElement.GetProperty("success").GetBoolean());

            sessionId = createJson.RootElement.GetProperty("sessionId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sessionId));

            var (listResult, listJson) = await scope.WaitForSessionInListAsync(sessionId!);
            output.WriteLine($"session list: {listResult.Stdout}");

            Assert.Equal(0, listResult.ExitCode);
            Assert.True(listJson.RootElement.GetProperty("success").GetBoolean());

            var matchingSession = listJson.RootElement
                .GetProperty("sessions")
                .EnumerateArray()
                .First(session => session.GetProperty("sessionId").GetString() == sessionId);

            Assert.True(matchingSession.GetProperty("isVisioVisible").GetBoolean());
            Assert.True(matchingSession.GetProperty("isVisioVisible").GetBoolean());
        }
        finally
        {
            await scope.CloseSessionIfPresentAsync(sessionId);
            await SessionTestScope.TryDeleteFileAsync(filePath);
        }
    }

    [Fact]
    public async Task SessionClose_SaveFalse_DiscardsUnsavedChanges()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"CliSessionCloseDiscard_{Guid.NewGuid():N}.vsdx");
        using var scope = await SessionTestScope.StartAsync(output);
        string? sessionId = null;

        try
        {
            var (createResult, createJson) = await scope.RunJsonAsync(
                $"session create \"{filePath}\"",
                timeoutMs: 120000);

            Assert.Equal(0, createResult.ExitCode);
            Assert.True(createJson.RootElement.GetProperty("success").GetBoolean());

            sessionId = createJson.RootElement.GetProperty("sessionId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sessionId));

            var (initialCloseResult, initialCloseJson) = await scope.RunJsonAsync(
                $"session close --session {sessionId} --save true",
                timeoutMs: 120000);

            Assert.Equal(0, initialCloseResult.ExitCode);
            Assert.True(initialCloseJson.RootElement.GetProperty("success").GetBoolean());
            sessionId = null;
            await scope.WaitForNoActiveSessionsAsync();

            var (reopenBaselineResult, reopenBaselineJson) = await scope.RunJsonAsync(
                $"session open \"{filePath}\"",
                timeoutMs: 120000);

            Assert.Equal(0, reopenBaselineResult.ExitCode);
            Assert.True(reopenBaselineJson.RootElement.GetProperty("success").GetBoolean());

            sessionId = reopenBaselineJson.RootElement.GetProperty("sessionId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sessionId));

            var (addResult, addJson) = await scope.RunJsonAsync(
                $"shape add-shape --session {sessionId} --page-index 1 --auto-shape-type 1 --left 72 --top 72 --width 144 --height 72",
                timeoutMs: 120000);

            output.WriteLine($"shape add-shape: {addResult.Stdout}");

            Assert.Equal(0, addResult.ExitCode);
            Assert.True(addJson.RootElement.GetProperty("success").GetBoolean());

            var (closeResult, closeJson) = await scope.RunJsonAsync(
                $"session close --session {sessionId} --save false",
                timeoutMs: 120000);

            output.WriteLine($"session close --save false: {closeResult.Stdout}");

            Assert.Equal(0, closeResult.ExitCode);
            Assert.True(closeJson.RootElement.GetProperty("success").GetBoolean());
            sessionId = null;
            await scope.WaitForNoActiveSessionsAsync();

            var (reopenResult, reopenJson) = await scope.RunJsonAsync(
                $"session open \"{filePath}\"",
                timeoutMs: 120000);

            Assert.Equal(0, reopenResult.ExitCode);
            Assert.True(reopenJson.RootElement.GetProperty("success").GetBoolean());

            sessionId = reopenJson.RootElement.GetProperty("sessionId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sessionId));

            var (pageReadResult, pageReadJson) = await scope.RunJsonAsync(
                $"page read --session {sessionId} --page-index 1",
                timeoutMs: 120000);

            output.WriteLine($"page read after reopen: {pageReadResult.Stdout}");

            Assert.Equal(0, pageReadResult.ExitCode);
            Assert.True(pageReadJson.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(0, pageReadJson.RootElement.GetProperty("page").GetProperty("shapeCount").GetInt32());
        }
        finally
        {
            await scope.CloseSessionIfPresentAsync(sessionId);
            await SessionTestScope.TryDeleteFileAsync(filePath);
        }
    }

    private sealed class SessionTestScope : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _pipeName = $"VisioMcp-session-test-{Guid.NewGuid():N}";
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

        public async Task<CliResult> RunAsync(string args, int timeoutMs = 30000) =>
            await CliProcessHelper.RunAsync(args, timeoutMs, _environment);

        public async Task<(CliResult Result, JsonDocument Json)> RunJsonAsync(string args, int timeoutMs = 30000) =>
            await CliProcessHelper.RunJsonAsync(args, timeoutMs, _environment);

        public async Task<(CliResult Result, JsonDocument Json)> WaitForSessionInListAsync(string sessionId)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var (result, json) = await RunJsonAsync("session list", timeoutMs: 45000);
                if (result.ExitCode == 0 &&
                    json.RootElement.TryGetProperty("sessions", out var sessions) &&
                    sessions.ValueKind == JsonValueKind.Array &&
                    sessions.EnumerateArray().Any(session => session.GetProperty("sessionId").GetString() == sessionId))
                {
                    return (result, json);
                }

                if (attempt < 19)
                {
                    json.Dispose();
                    await Task.Delay(500);
                    continue;
                }

                return (result, json);
            }

            throw new InvalidOperationException("WaitForSessionInListAsync exhausted retries unexpectedly.");
        }

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
            _output.WriteLine($"Started session test daemon PID {process.Id}, pipe {_pipeName}");
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
                    // Daemon not ready yet.
                }

                await Task.Delay(500);
            }

            throw new TimeoutException("Session test daemon did not become ready within timeout.");
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
                _output.WriteLine($"Failed to stop session test daemon: {ex.Message}");
            }
            finally
            {
                _daemonProcess.Dispose();
                _daemonProcess = null;
            }
        }
    }
}
