using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that no public tool action or parameter calls a Visio page a slide.
///
/// A Visio drawing has pages. When a tool offers <c>shape(copy-to-slide)</c> with a
/// <c>target_slide_index</c>, or asks <c>shapealign</c> for a <c>slide_index</c>, the agent is
/// being told the wrong noun for the primary object it is manipulating — in the one place it
/// cannot avoid reading, because that is the call it has to make (#71).
///
/// This is not cosmetic. The names are the model's map of the domain: an agent that believes it is
/// working with slides will reach for slide concepts that do not exist here, and the tool
/// description cannot argue it out of what the parameter list says.
///
/// Scoped to the public surface. The suppressed domains — <c>vba</c> and the rest — still carry
/// <c>slideIndex</c> and are excluded until they are ported (#66). They do not ship, so they cannot
/// mislead anyone.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class PublicSurfaceNamingTests
{
    private static readonly string[] PublicDomains =
    [
        "Cell", "Comment", "Design", "DocumentProperty", "Export", "File", "Hyperlink", "Image", "Layer",
        "Master", "Page", "Shape", "ShapeAlign", "Stencil", "Style", "Text", "Window"
    ];

    private static readonly Regex ActionPattern = new(
        @"\[ServiceAction\(""(?<action>[^""]+)""\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Matches a C# parameter named after a slide, in the method signatures the generator turns
    /// into CLI options and MCP parameters.
    /// </summary>
    private static readonly Regex ParameterPattern = new(
        @"\b\w+\s+(?<name>\w*[sS]lide\w*)\s*[,)]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void PublicActionsAndParameters_CallAPageAPage()
    {
        var root = FindRepositoryRoot();
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var offenders = new List<string>();
        var checkedDomains = 0;

        foreach (var domain in PublicDomains)
        {
            var directory = Path.Combine(commandsRoot, domain);

            if (!Directory.Exists(directory))
            {
                continue;
            }

            var interfaceFile = Directory
                .GetFiles(directory, "I*Commands.cs", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (interfaceFile is null)
            {
                continue;
            }

            checkedDomains++;
            var relative = Path.GetRelativePath(root, interfaceFile);
            var text = File.ReadAllText(interfaceFile);

            foreach (Match match in ActionPattern.Matches(text))
            {
                var action = match.Groups["action"].Value;

                if (action.Contains("slide", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{relative}: action '{action}'");
                }
            }

            foreach (Match match in ParameterPattern.Matches(text))
            {
                var name = match.Groups["name"].Value;
                offenders.Add($"{relative}: parameter '{name}'");
            }
        }

        Assert.True(
            checkedDomains > 0,
            "No public command interfaces found — the guard would pass vacuously.");

        Assert.True(
            offenders.Count == 0,
            "A public tool calls a Visio page a slide. The parameter list is the model's map of "
            + "the domain, and a tool description cannot argue it out of what the signature says:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Distinct(StringComparer.Ordinal)));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FEATURES.md"))
                && File.Exists(Path.Combine(current.FullName, "VisioMcp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Repository root not found walking up from '{AppContext.BaseDirectory}'.");
    }
}
