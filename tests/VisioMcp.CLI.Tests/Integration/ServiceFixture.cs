using VisioMcp.Service;
using Xunit;

namespace VisioMcp.CLI.Tests.Integration;

/// <summary>
/// Fixture that starts an in-process VisioMcp service for CLI integration tests.
/// Uses the CLI pipe name so CLI commands can connect to it.
/// </summary>
public sealed class ServiceFixture : IAsyncLifetime, IDisposable
{
    private VisioMcpService? _service;

    public async Task InitializeAsync()
    {
        var pipeName = ServiceSecurity.GetCliPipeName();
        _service = new VisioMcpService();
        _ = Task.Run(() => _service.RunAsync(pipeName));

        // Wait for pipe server to be ready
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(100);
            using var client = new ServiceClient(pipeName, connectTimeout: TimeSpan.FromSeconds(1));
            if (await client.PingAsync())
            {
                return;
            }
        }

        throw new InvalidOperationException("VisioMcp service did not start within timeout.");
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _service?.RequestShutdown();
        _service?.Dispose();
        _service = null;
    }
}

/// <summary>
/// Collection definition for tests that require the VisioMcp service.
/// Apply [Collection("Service")] to test classes that call visiocli commands.
/// </summary>
[CollectionDefinition("Service")]
public sealed class ServiceTestGroup : ICollectionFixture<ServiceFixture>;
