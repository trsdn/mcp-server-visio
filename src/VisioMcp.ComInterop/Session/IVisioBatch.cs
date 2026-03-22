namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Represents a batch of Office COM operations that share a single application instance.
/// </summary>
public interface IVisioBatch : IDisposable
{
    /// <summary>
    /// Gets the path to the primary document using the legacy presentation name.
    /// </summary>
    string PresentationPath { get; }

    /// <summary>
    /// Gets the path to the primary Visio document.
    /// </summary>
    string DocumentPath { get; }

    /// <summary>
    /// Gets the logger instance for diagnostic output.
    /// </summary>
    Microsoft.Extensions.Logging.ILogger Logger { get; }

    /// <summary>
    /// Gets all open documents keyed by normalized file path using the legacy property name.
    /// </summary>
    IReadOnlyDictionary<string, object> Presentations { get; }

    /// <summary>
    /// Gets all open Visio documents keyed by normalized file path.
    /// </summary>
    IReadOnlyDictionary<string, object> Documents { get; }

    /// <summary>
    /// Gets the COM document object for a specific file path using the legacy method name.
    /// </summary>
    object GetPresentation(string filePath);

    /// <summary>
    /// Gets the COM Visio document object for a specific file path.
    /// </summary>
    object GetDocument(string filePath);

    /// <summary>
    /// Executes a void COM operation within this batch.
    /// </summary>
    void Execute(Action<VisioContext, CancellationToken> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a COM operation within this batch.
    /// </summary>
    T Execute<T>(Func<VisioContext, CancellationToken, T> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the active document.
    /// </summary>
    void Save(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the underlying Office process is still alive.
    /// </summary>
    bool IsPowerPointProcessAlive();

    /// <summary>
    /// Gets the underlying Office process ID, if captured.
    /// </summary>
    int? PowerPointProcessId { get; }

    /// <summary>
    /// Gets the operation timeout for this batch.
    /// </summary>
    TimeSpan OperationTimeout { get; }
}
