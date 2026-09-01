using System.Reflection;
using VisioMcp.ComInterop.Session;
using Xunit;

namespace VisioMcp.ComInterop.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="VisioContext"/>.
///
/// The class is a data holder over two COM objects, so the interesting assertions are not about
/// behaviour but about the **shape of its public surface**. It used to expose every COM object
/// twice — once under a Visio name and once under the PowerPoint name it was migrated from
/// (<c>Presentation</c>, <c>PresentationPath</c>, <c>App</c>). Because the properties are
/// <c>dynamic</c>, <c>ctx.Document.Slides</c> compiled cleanly and failed only at runtime, so
/// the compiler could not help with the migration the repository was performing (#21).
///
/// The reflection test below is the guard that keeps them gone. COM is never touched, so these are
/// unit tests under Rule 30.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "ComInterop")]
[Trait("RequiresVisio", "false")]
public class VisioContextTests
{
    /// <summary>
    /// Names that would reintroduce the ambiguity #21 removed.
    /// </summary>
    private static readonly string[] ForbiddenMemberNames =
    [
        "Presentation",
        "PresentationPath",
        "Presentations",
        "GetPresentation",
        "App",
        "Slide",
        "Slides"
    ];

    [Fact]
    public void PublicSurface_ExposesNoPowerPointNamedMember()
    {
        var offenders = typeof(VisioContext)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Where(n => ForbiddenMemberNames.Contains(n, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"VisioContext exposes PowerPoint-named member(s): {string.Join(", ", offenders)}. "
            + "These are dynamic, so a call site using them compiles and fails only at runtime — "
            + "which is exactly the class of silent failure #21 removed.");
    }

    /// <summary>
    /// <see cref="IVisioBatch"/> carried the same duplication — <c>PresentationPath</c>,
    /// <c>Presentations</c> and <c>GetPresentation</c> beside their Document-named twins — and it
    /// is the type every Core command takes as its first parameter, so leaving it aliased would
    /// have defeated the point of removing the aliases from <see cref="VisioContext"/>.
    /// </summary>
    [Fact]
    public void BatchSurface_ExposesNoPowerPointNamedMember()
    {
        var offenders = typeof(IVisioBatch)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Where(n => ForbiddenMemberNames.Contains(n, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"IVisioBatch exposes PowerPoint-named member(s): {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void ConstructorParameters_AreNamedForVisio()
    {
        var parameters = typeof(VisioContext)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.Name ?? string.Empty)
            .ToArray();

        Assert.Equal(["documentPath", "application", "document"], parameters);
    }

    [Fact]
    public void PublicSurface_ExposesTheVisioNames()
    {
        var names = typeof(VisioContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("DocumentPath", names);
        Assert.Contains("Document", names);
        Assert.Contains("Application", names);

        // Exactly three properties: no alias may hide behind a fourth.
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void Constructor_NullDocumentPath_ThrowsNamingThatParameter()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(null!, null!, null!));

        Assert.Equal("documentPath", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullApplication_ThrowsNamingThatParameter()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(@"C:\test\drawing.vsdx", null!, null!));

        Assert.Equal("application", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullDocument_ThrowsNamingThatParameter()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(@"C:\test\drawing.vsdx", new object(), null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Theory]
    [InlineData(@"C:\test\drawing.vsdx")]
    [InlineData(@"\\server\share\drawing.vsdm")]
    [InlineData(@"D:\Documents\My Drawing.vsdx")]
    [InlineData("drawing.vsdx")]
    public void Constructor_ValidatesDocumentPathBeforeComObjects(string documentPath)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath, null!, null!));

        Assert.Equal("application", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithAllArguments_ExposesThemUnderTheVisioNames()
    {
        var application = new object();
        var document = new object();

        var context = new VisioContext(@"C:\test\drawing.vsdx", application, document);

        Assert.Equal(@"C:\test\drawing.vsdx", context.DocumentPath);
        Assert.Same(application, (object)context.Application);
        Assert.Same(document, (object)context.Document);
    }
}
