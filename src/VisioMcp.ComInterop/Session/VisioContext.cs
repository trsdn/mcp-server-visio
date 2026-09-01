namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Provides access to the active Visio COM objects for session operations.
/// </summary>
public sealed class VisioContext
{
    /// <summary>
    /// Creates a new <see cref="VisioContext"/>.
    /// </summary>
    /// <param name="documentPath">Full path to the active document.</param>
    /// <param name="app">Visio application COM object.</param>
    /// <param name="document">Visio document COM object.</param>
    public VisioContext(string documentPath, dynamic app, dynamic document)
    {
        DocumentPath = documentPath ?? throw new ArgumentNullException(nameof(documentPath));
        App = app ?? throw new ArgumentNullException(nameof(app));
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Gets the full path to the active Visio document.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// Gets the active Visio application COM object.
    /// </summary>
    public dynamic App { get; }

    /// <summary>
    /// Gets the active Visio application COM object.
    /// </summary>
    public dynamic Application => App;

    /// <summary>
    /// Gets the active Visio document COM object.
    /// </summary>
    public dynamic Document { get; }
}
