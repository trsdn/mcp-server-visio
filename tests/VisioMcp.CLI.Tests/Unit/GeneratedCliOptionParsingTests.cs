using System.Text.Json;
using VisioMcp.CLI.Tests.Helpers;
using Xunit;

namespace VisioMcp.CLI.Tests.Unit;

/// <summary>
/// Regression coverage for generated CLI commands rejecting options removed from the Core interface.
/// </summary>
[Trait("Layer", "CLI")]
[Trait("Category", "Unit")]
[Trait("Feature", "ActionValidation")]
[Trait("Speed", "Fast")]
public sealed class GeneratedCliOptionParsingTests
{
    [Fact]
    public async Task CommentAdd_WithRemovedAuthorOption_IsRejectedBeforeServiceDispatch()
    {
        var result = await CliProcessHelper.RunAsync(
            "comment add --author x --session missing --page-index 1 --text probe");

        Assert.Equal(1, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        var error = json.RootElement.GetProperty("error").GetString();
        Assert.Contains("does not accept --author", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Session 'missing' not found", error, StringComparison.Ordinal);
    }
}
