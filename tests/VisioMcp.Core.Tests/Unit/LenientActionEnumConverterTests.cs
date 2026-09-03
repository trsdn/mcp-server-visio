using System.Text.Json;
using VisioMcp.Core.Json;
using VisioMcp.Generated;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Covers the converter that lets an unrecognised tool action be reported instead of thrown.
///
/// Before this, an action the enum did not define failed during argument binding — before any tool
/// code ran — so the MCP surface answered <c>"An error occurred invoking 'text'"</c> while the CLI
/// answered with the list of valid actions for the identical input (#55).
///
/// Two details were only discoverable by running it, and both are asserted here because either
/// would silently undo the fix:
///
/// <list type="number">
/// <item><b>The sentinel must survive nullable wrapping.</b> System.Text.Json unwraps
/// <c>Nullable&lt;T&gt;</c> before consulting a converter registered for <c>T</c>, so a converter
/// that returned <c>null</c> for "unrecognised" never got the chance. Returning an undeclared enum
/// value does survive, and the generated tools test it with <see cref="Enum.IsDefined"/>.</item>
/// <item><b>The sentinel must stay out of the schema.</b> It is <c>-1</c>, not a declared member,
/// so the published enum still lists exactly the valid actions — a well-behaved client's discovery
/// is unaffected.</item>
/// </list>
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class LenientActionEnumConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Insert(0, new LenientActionEnumConverterFactory());
        return options;
    }

    [Fact]
    public void KnownAction_BindsToItsEnumMember()
    {
        var action = JsonSerializer.Deserialize<TextAction?>("\"insert-page-number\"", CreateOptions());

        Assert.Equal(TextAction.InsertPageNumber, action);
    }

    [Fact]
    public void UnknownAction_BindsToAnUndefinedValueRatherThanThrowing()
    {
        var action = JsonSerializer.Deserialize<TextAction?>("\"list\"", CreateOptions());

        Assert.NotNull(action);
        Assert.False(
            Enum.IsDefined(action!.Value),
            "An unrecognised action must land on a value Enum.IsDefined rejects — that is how the "
            + "generated tools detect it and answer with the valid-action list.");
    }

    [Fact]
    public void MissingAction_StaysNull()
    {
        var action = JsonSerializer.Deserialize<TextAction?>("null", CreateOptions());

        Assert.Null(action);
    }

    /// <summary>
    /// The sentinel is <c>-1</c>; generated enums number upward from zero, so it can never collide
    /// with a real action.
    /// </summary>
    [Fact]
    public void SentinelDoesNotCollideWithAnyGeneratedAction()
    {
        foreach (var value in Enum.GetValues<TextAction>())
        {
            Assert.NotEqual(-1, Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void Factory_ClaimsOnlyTheNonNullableForm()
    {
        var factory = new LenientActionEnumConverterFactory();

        Assert.True(factory.CanConvert(typeof(TextAction)));

        // Claiming Nullable<T> while returning a converter for T makes System.Text.Json throw at
        // startup: "handles type X but is being asked to convert type Nullable<X>".
        Assert.False(factory.CanConvert(typeof(TextAction?)));
    }

    [Fact]
    public void Factory_IgnoresEnumsThatAreNotToolActions()
    {
        var factory = new LenientActionEnumConverterFactory();

        Assert.False(factory.CanConvert(typeof(DayOfWeek)));
        Assert.False(factory.CanConvert(typeof(string)));
    }
}
