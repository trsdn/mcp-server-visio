namespace VisioMcp.ComInterop;

/// <summary>
/// Constants for Visio COM interop operations.
/// </summary>
public static class ComInteropConstants
{
    #region Timeouts

    /// <summary>
    /// Timeout for Visio.Quit() operation (30 seconds).
    /// With DisplayAlerts=false, Visio quits quickly. This timeout catches hung scenarios.
    /// </summary>
    public static readonly TimeSpan VisioQuitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for STA thread join after quit.
    /// CRITICAL: Must be >= VisioQuitTimeout to ensure Dispose() waits for CloseAndQuit() to complete.
    /// Set to VisioQuitTimeout + 15s margin for document close and COM cleanup.
    /// </summary>
    public static readonly TimeSpan StaThreadJoinTimeout = VisioQuitTimeout + TimeSpan.FromSeconds(15);

    /// <summary>
    /// Timeout for save operations (5 minutes).
    /// Large documents may take longer to save.
    /// </summary>
    public static readonly TimeSpan SaveOperationTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default timeout for individual Visio operations (5 minutes).
    /// Most operations complete in under 30 seconds, but this provides buffer for slow machines.
    /// Can be overridden when creating a session via timeoutSeconds parameter.
    /// </summary>
    public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum wait time for session creation file lock acquisition (5 seconds).
    /// </summary>
    public static readonly TimeSpan SessionFileLockTimeout = TimeSpan.FromSeconds(5);

    #endregion

    #region Sleep Intervals

    /// <summary>
    /// Delay between file lock acquisition retries (100ms).
    /// </summary>
    public const int FileLockRetryDelayMs = 100;

    /// <summary>
    /// Delay between session lock acquisition retries (200ms).
    /// </summary>
    public const int SessionLockRetryDelayMs = 200;

    #endregion
}


