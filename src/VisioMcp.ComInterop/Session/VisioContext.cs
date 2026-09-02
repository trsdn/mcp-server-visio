namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Provides access to the active Visio COM objects for session operations.
/// </summary>
/// <remarks>
/// This type deliberately exposes each COM object under exactly one name. It previously carried
/// PowerPoint aliases — <c>Presentation</c>, <c>PresentationPath</c> and <c>App</c> — alongside the
/// Visio names, for compatibility during the migration. Because the properties are
/// <c>dynamic</c>, that made PowerPoint-era code look correct at the call site:
/// <c>ctx.Document.Slides</c> compiled cleanly and failed only when executed, so the compiler
/// could not help with the migration in progress. The aliases were removed in #21; do not
/// reintroduce them. <c>VisioContextTests</c> fails the build if any returns.
/// </remarks>
public sealed class VisioContext
{
    /// <summary>
    /// Creates a new <see cref="VisioContext"/>.
    /// </summary>
    /// <param name="documentPath">Full path to the active Visio document.</param>
    /// <param name="application">Visio <c>Application</c> COM object.</param>
    /// <param name="document">Visio <c>Document</c> COM object.</param>
    public VisioContext(string documentPath, dynamic application, dynamic document)
    {
        DocumentPath = documentPath ?? throw new ArgumentNullException(nameof(documentPath));
        Application = application ?? throw new ArgumentNullException(nameof(application));
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Gets the full path to the active Visio document.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// Gets the active Visio <c>Application</c> COM object.
    /// </summary>
    public dynamic Application { get; }

    /// <summary>
    /// Gets the active Visio <c>Document</c> COM object.
    /// </summary>
    public dynamic Document { get; }
}
