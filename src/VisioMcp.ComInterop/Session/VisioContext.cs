namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Provides access to the active Office COM objects for session operations.
/// During the migration, legacy property names remain for compatibility with callers.
/// </summary>
public sealed class VisioContext
{
    /// <summary>
    /// Creates a new <see cref="VisioContext"/>.
    /// </summary>
    /// <param name="presentationPath">Full path to the active document.</param>
    /// <param name="app">Application COM object.</param>
    /// <param name="presentation">Document COM object.</param>
    public VisioContext(string presentationPath, dynamic app, dynamic presentation)
    {
        PresentationPath = presentationPath ?? throw new ArgumentNullException(nameof(presentationPath));
        App = app ?? throw new ArgumentNullException(nameof(app));
        Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
    }

    /// <summary>
    /// Gets the full path to the active document using the legacy name.
    /// </summary>
    public string PresentationPath { get; }

    /// <summary>
    /// Gets the full path to the active Visio document.
    /// </summary>
    public string DocumentPath => PresentationPath;

    /// <summary>
    /// Gets the active application COM object.
    /// </summary>
    public dynamic App { get; }

    /// <summary>
    /// Gets the active application COM object.
    /// </summary>
    public dynamic Application => App;

    /// <summary>
    /// Gets the active document COM object using the legacy presentation name.
    /// </summary>
    public dynamic Presentation { get; }

    /// <summary>
    /// Gets the active Visio document COM object.
    /// </summary>
    public dynamic Document => Presentation;
}
