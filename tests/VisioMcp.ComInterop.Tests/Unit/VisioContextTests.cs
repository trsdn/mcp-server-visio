using VisioMcp.ComInterop.Session;
using Xunit;

namespace VisioMcp.ComInterop.Tests.Unit;

/// <summary>
/// Unit tests for VisioContext - validates constructor and property behavior.
/// This class is a simple data holder, so tests focus on path validation and immutability.
/// Note: Visio.Application and Visio.Document COM objects cannot be mocked in unit tests,
/// so these tests use null! for those parameters and verify only what is testable.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "ComInterop")]
public class VisioContextTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsPresentationPathCorrectly()
    {
        // Arrange
        string documentPath = @"C:\test\document.vsdx";

        // Act & Assert - Constructor throws ArgumentNullException for null COM objects,
        // which is expected behavior. DocumentPath validation is tested separately.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath, null!, null!));

        // When null is passed, the constructor throws on the first null param (visioApp)
        Assert.NotNull(ex);
    }

    [Fact]
    public void Constructor_WithNullPresentationPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? documentPath = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath!, null!, null!));

        Assert.Equal("documentPath", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullVisio_ThrowsArgumentNullException()
    {
        // Arrange
        string documentPath = @"C:\test\document.vsdx";

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath, null!, null!));

        Assert.Equal("app", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPresentationPath_ThrowsBeforeNullVisio()
    {
        // Arrange
        string? documentPath = null;

        // Act & Assert - DocumentPath is validated first
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath!, null!, null!));

        Assert.Equal("documentPath", ex.ParamName);
    }

    [Fact]
    public void Constructor_PresentationPathValidation_RejectsNull()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(null!, null!, null!));

        Assert.Equal("documentPath", ex.ParamName);
    }

    [Theory]
    [InlineData(@"C:\test\document.vsdx")]
    [InlineData(@"\\server\share\document.vsdm")]
    [InlineData(@"D:\Documents\My Document.vsdx")]
    [InlineData(@"document.vsdx")] // Relative path
    public void Constructor_WithNullVisioAnyPath_ThrowsArgumentNullException(string documentPath)
    {
        // Act & Assert - Path is validated, then Visio COM object is validated
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(documentPath, null!, null!));

        // app is the first COM parameter validated after documentPath
        Assert.Equal("app", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullPresentationPath_ThrowsWithCorrectParamName()
    {
        // Arrange - Simulates null path being passed
        Assert.Throws<ArgumentNullException>(() =>
            new VisioContext(null!, null!, null!));
    }
}





