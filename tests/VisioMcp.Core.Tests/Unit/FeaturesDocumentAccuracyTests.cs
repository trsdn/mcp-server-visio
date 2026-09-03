using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that every action <c>FEATURES.md</c> claims as shipped actually exists.
///
/// <c>FEATURES.md</c> is the file a reviewer, a contributor or a planning agent reads to decide
/// what to build. It drifts in both directions and both are costly: claiming something that does
/// not exist sends someone to use it, and *understating* what exists invites the work to be done
/// twice.
///
/// The understating kind is what prompted this. The connectors table read "Port / redesign" for
/// work that was already finished — and the issue reporting that (#38) was itself out of date,
/// listing connection points and generic ShapeSheet row access as still missing when both had
/// shipped. Two layers of stale claims about the same code.
///
/// So the rule is checked against the interfaces rather than restated: any row marked
/// <c>**Shipped**</c> must name a real tool, and every action it lists for that tool must carry a
/// matching <c>[ServiceAction]</c>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class FeaturesDocumentAccuracyTests
{
    /// <summary>
    /// Matches a claim of the form <c>`tool`: `action`, `action`</c>, capturing the tool and the
    /// run of actions that follows it.
    /// </summary>
    private static readonly Regex ClaimPattern = new(
        @"`(?<tool>[a-z]+)`:\s*(?<actions>(?:`[a-z][a-z0-9-]*`(?:,\s*)?)+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ActionTokenPattern = new(
        @"`(?<action>[a-z][a-z0-9-]*)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CategoryPattern = new(
        @"ServiceCategory\(""(?<category>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ServiceActionPattern = new(
        @"\[ServiceAction\(""(?<action>[^""]+)""\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ShippedClaims_NameActionsThatExist()
    {
        var root = FindRepositoryRoot();
        var featuresPath = Path.Combine(root, "FEATURES.md");

        Assert.True(File.Exists(featuresPath), $"Expected FEATURES.md at '{featuresPath}'.");

        var surface = ReadToolSurface(root);
        Assert.True(surface.Count > 0, "No tool categories found — the guard would pass vacuously.");

        var offenders = new List<string>();
        var claimsChecked = 0;

        foreach (var line in File.ReadAllLines(featuresPath))
        {
            if (!line.Contains("**Shipped**", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match claim in ClaimPattern.Matches(line))
            {
                var tool = claim.Groups["tool"].Value;

                if (!surface.TryGetValue(tool, out var actions))
                {
                    offenders.Add($"'{tool}' is not a tool");
                    continue;
                }

                foreach (Match token in ActionTokenPattern.Matches(claim.Groups["actions"].Value))
                {
                    var action = token.Groups["action"].Value;
                    claimsChecked++;

                    if (!actions.Contains(action))
                    {
                        offenders.Add($"{tool}({action}) does not exist");
                    }
                }
            }
        }

        Assert.True(
            claimsChecked > 0,
            "No shipped-action claims found in FEATURES.md. Either the table changed shape or this "
            + "guard is checking nothing — do not leave it passing vacuously.");

        Assert.True(
            offenders.Count == 0,
            "FEATURES.md claims actions that do not exist. It is what people read to decide what to "
            + "build:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Distinct(StringComparer.Ordinal)));
    }

    private static Dictionary<string, HashSet<string>> ReadToolSurface(string root)
    {
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var surface = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (!Directory.Exists(commandsRoot))
        {
            return surface;
        }

        foreach (var file in Directory.GetFiles(commandsRoot, "I*Commands.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var category = CategoryPattern.Match(text);

            if (!category.Success)
            {
                continue;
            }

            surface[category.Groups["category"].Value] = ServiceActionPattern
                .Matches(text)
                .Select(match => match.Groups["action"].Value)
                .ToHashSet(StringComparer.Ordinal);
        }

        return surface;
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
