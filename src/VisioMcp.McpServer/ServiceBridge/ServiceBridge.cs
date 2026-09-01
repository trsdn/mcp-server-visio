using System.Text.Json;
using VisioMcp.Service;

namespace VisioMcp.McpServer.ServiceBridge;

/// <summary>
/// Bridge that holds the in-process VisioMcp Service for direct method calls.
/// No named pipe — MCP tools call the service directly (same process).
/// </summary>
public static class ServiceBridge
{
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static Service.VisioMcpService? _service;

    /// <summary>
    /// JSON serializer options for deserializing service responses.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = ServiceProtocol.JsonOptions;

    /// <summary>
    /// Ensures the in-process VisioMcp Service is created.
    /// Called automatically on first request.
    /// </summary>
    public static async Task<bool> EnsureServiceAsync(CancellationToken cancellationToken = default)
    {
        if (_service != null)
        {
            return true;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_service != null)
            {
                return true;
            }

            _service = new Service.VisioMcpService();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Sends a command to the VisioMcp Service directly (in-process, no pipe).
    /// </summary>
    public static async Task<ServiceResponse> SendAsync(
        string command,
        string? sessionId = null,
        object? args = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureServiceAsync(cancellationToken))
        {
            return new ServiceResponse
            {
                Success = false,
                ErrorMessage = "Failed to start VisioMcp Service in-process."
            };
        }

        var request = new ServiceRequest
        {
            Command = command,
            SessionId = sessionId,
            Args = args != null ? JsonSerializer.Serialize(args, JsonOptions) : null
        };

        // Apply timeout if specified
        if (timeoutSeconds.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds.Value));
            try
            {
                return await _service!.ProcessAsync(request);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return new ServiceResponse
                {
                    Success = false,
                    ErrorMessage = $"Operation timed out after {timeoutSeconds} seconds."
                };
            }
        }

        return await _service!.ProcessAsync(request);
    }

    /// <summary>
    /// Sends a session-scoped command to the service.
    /// </summary>
    public static async Task<ServiceResponse> WithSessionAsync(
        string sessionId,
        string command,
        object? args = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new ServiceResponse
            {
                Success = false,
                ErrorMessage = "sessionId is required. Use file 'open' action to start a session."
            };
        }

        return await SendAsync(command, sessionId, args, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Opens a session via the service.
    /// </summary>
    public static async Task<ServiceResponse> OpenSessionAsync(
        string documentPath,
        bool show = false,
        string? pageName = null,
        int? pageIndex = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.open", null, new
        {
            filePath = documentPath,
            show,
            pageName,
            pageIndex,
            timeoutSeconds
        }, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Creates a new file and opens a session via the service.
    /// </summary>
    public static async Task<ServiceResponse> CreateSessionAsync(
        string documentPath,
        bool macroEnabled = false,
        bool show = false,
        string? pageName = null,
        int? pageIndex = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.create", null, new
        {
            filePath = documentPath,
            macroEnabled,
            show,
            pageName,
            pageIndex,
            timeoutSeconds
        }, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Closes a session via the service.
    /// </summary>
    public static async Task<ServiceResponse> CloseSessionAsync(
        string sessionId,
        bool save = true,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.close", sessionId, new { save }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Lists active sessions via the service.
    /// </summary>
    public static async Task<ServiceResponse> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.list", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Saves a session via the service.
    /// </summary>
    public static async Task<ServiceResponse> SaveSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.save", sessionId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Tests if a file can be opened via the service.
    /// </summary>
    public static async Task<ServiceResponse> TestFileAsync(
        string documentPath,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("session.test", null, new { filePath = documentPath }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Disposes the in-process VisioMcp Service.
    /// Unsaved sessions are discarded unless the caller explicitly saved first.
    /// </summary>
    public static void Dispose()
    {
        var service = Interlocked.Exchange(ref _service, null);
        service?.Dispose();
    }
}
