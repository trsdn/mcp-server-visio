using Xunit;

namespace VisioMcp.ComInterop.Tests;

/// <summary>
/// <c>SafeGetString</c> and <c>SafeGetInt</c> read a fixed set of property names. Anything outside
/// that set must throw rather than return a default.
/// </summary>
/// <remarks>
/// These previously ended their switch with <c>_ => null</c> and <c>_ => 0</c>, so asking for a
/// property they did not handle returned an empty string or zero as though the COM object had said
/// so. That shipped a wrong answer with no error: a master's populated <c>UniqueID</c> was reported
/// as empty, and the calling code had no way to tell the difference. Found in #34.
///
/// These are unit tests: they assert the behaviour of the name lookup, which runs before any COM
/// call and needs no Visio (Rule 30).
/// </remarks>
public class SafeGetPropertyNameTests
{
    [Theory]
    [InlineData("UniqueID")]
    [InlineData("NameU")]
    [InlineData("Prompt")]
    public void SafeGetString_SupportsThePropertiesCallersActuallyRead(string propertyName)
    {
        // A null object still exercises the name lookup; it must not be rejected as unsupported.
        var value = ComUtilities.SafeGetString(null, propertyName);

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void SafeGetString_UnsupportedProperty_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => ComUtilities.SafeGetString(null, "Angle"));

        // The message has to say what to do, because the alternative used to be a silent "".
        Assert.Contains("Angle", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Supported:", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SafeGetInt_UnsupportedProperty_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => ComUtilities.SafeGetInt(null, "ID"));

        Assert.Contains("ID", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Supported:", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SafeGetString_StillAbsorbsComFailures()
    {
        // The point of "Safe" is tolerating a COM object that cannot answer, and that must survive.
        var value = ComUtilities.SafeGetString(null, "Name");

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void SafeGetInt_StillAbsorbsComFailures()
    {
        Assert.Equal(0, ComUtilities.SafeGetInt(null, "Count"));
    }
}
