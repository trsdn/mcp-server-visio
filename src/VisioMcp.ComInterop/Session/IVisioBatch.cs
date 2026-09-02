namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Represents a batch of Visio COM operations that share a single application instance.
/// </summary>
/// <remarks>
/// Each member is exposed under exactly one name. This interface previously carried
/// <c>PresentationPath</c>, <c>Presentations</c> and <c>GetPresentation</c> beside their
/// Document-named twins; since it is the first parameter of every Core command, leaving those in
/// place would have defeated the alias removal in #21. They had no callers.
/// </remarks>
public interface IVisioBatch : IDisposable
{
    /// <summary>
    /// Gets the path to the primary Visio document.
    /// </summary>
    string DocumentPath { get; }

    /// <summary>
    /// Gets the logger instance for diagnostic output.
    /// </summary>
    Microsoft.Extensions.Logging.ILogger Logger { get; }

    /// <summary>
    /// Gets all open Visio documents keyed by normalized file path.
    /// </summary>
    IReadOnlyDictionary<string, object> Documents { get; }

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
    /// Checks if the underlying Visio process is still alive.
    /// </summary>
    bool IsVisioProcessAlive();

    /// <summary>
    /// Gets the underlying Visio process ID, if captured.
    /// </summary>
    int? VisioProcessId { get; }

    /// <summary>
    /// Gets the operation timeout for this batch.
    /// </summary>
    TimeSpan OperationTimeout { get; }
}
