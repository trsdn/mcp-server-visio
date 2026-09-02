using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using VisioMcp.McpServer.Tools;
using Xunit;

namespace VisioMcp.McpServer.Tests.Integration;

/// <summary>
/// Asserts that no PowerPoint terminology reaches an LLM through the MCP surface.
///
/// <c>FEATURES.md</c> has stated this rule since before the migration began — *"do not keep
/// PowerPoint terminology in user-facing text"* — and #23 exists because the rule was never
/// enforced. Prose cannot enforce itself, so this test reads the descriptions the SDK actually
/// registers: tool-level <see cref="DescriptionAttribute"/> and every parameter's.
///
/// It caught a live defect on the `shape` tool, whose description advertised twelve
/// <c>MsoAutoShapeType</c> values — Triangle, Hexagon, Chevron, Heart — of which exactly one
/// (9, ellipse) does anything. Every other value silently draws a rectangle.
///
/// No COM is touched, so this is reflection over an already-loaded assembly rather than a Visio
/// integration test.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "McpServer")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class McpDescriptionTerminologyTests
{
    /// <summary>
    /// Terms that describe a PowerPoint product rather than a Visio one. Matched whole-word and
    /// case-insensitively.
    /// </summary>
    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint",
        "presentation",
        "presentations",
        "pptx",
        "pptm",
        "MsoShapeType",
        "MsoAutoShapeType",
        "slide master",
        "slide layout"
    ];

    /// <summary>
    /// Every description the MCP SDK registers, keyed by where it came from.
    /// </summary>
    public static TheoryData<string, string> RegisteredDescriptions()
    {
        var data = new TheoryData<string, string>();

        foreach (var (source, text) in EnumerateDescriptions())
        {
            data.Add(source, text);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RegisteredDescriptions))]
    public void Description_UsesNoPowerPointTerminology(string source, string description)
    {
        var offenders = ForbiddenTerms
            .Where(term => Regex.IsMatch(description, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{source} describes this as a PowerPoint product. Offending term(s): "
            + $"{string.Join(", ", offenders)}.{Environment.NewLine}Description: {description}");
    }

    [Fact]
    public void Discovery_FindsDescriptionsToCheck()
    {
        var all = EnumerateDescriptions().ToList();

        // A gate that checks nothing passes vacuously — the failure mode #15 shipped with.
        Assert.NotEmpty(all);

        // Every public tool must carry a tool-level description, or an LLM is choosing blind.
        var toolLevel = all.Where(d => d.Source.EndsWith("(tool)", StringComparison.Ordinal)).ToList();
        Assert.True(
            toolLevel.Count >= 10,
            $"Expected a description on each of the ~11 public tools, found {toolLevel.Count}.");
    }

    private static IEnumerable<(string Source, string Text)> EnumerateDescriptions()
    {
        var assembly = typeof(VisioFileTool).Assembly;

        var toolMethods = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false).Length > 0)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

        foreach (var method in toolMethods)
        {
            var toolName = method.GetCustomAttribute<McpServerToolAttribute>()!.Name ?? method.Name;

            var toolDescription = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(toolDescription))
            {
                yield return ($"'{toolName}' (tool)", toolDescription);
            }

            foreach (var parameter in method.GetParameters())
            {
                var parameterDescription = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrWhiteSpace(parameterDescription))
                {
                    yield return ($"'{toolName}' parameter '{parameter.Name}'", parameterDescription);
                }
            }
        }
    }
}
