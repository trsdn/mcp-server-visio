using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that every public command domain has a case in the service's dispatch switch.
///
/// The MCP tool and the CLI settings are both generated from the Core interface, so a new domain
/// appears on both surfaces automatically. The dispatch switch in
/// <c>VisioMcp.Service/VisioMcpService.cs</c> is **hand-written**, and nothing checked it: a domain
/// could be fully generated, pass the coverage audit, ship its MCP tool and CLI command, and still
/// answer every call with <c>"Unknown command category"</c>.
///
/// That is not hypothetical. Rule 24 records it happening to the `duplicate` action, and it happened
/// again while porting <c>headerfooter</c> (#63): the audit reported no gaps, the CLI accepted the
/// command and rejected wrong options correctly, and the daemon still had no idea the domain
/// existed. <c>audit-core-coverage.ps1</c> checks generated dispatch files, which is a different
/// thing.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class ServiceDispatchCoverageTests
{
    private static readonly Regex CategoryPattern = new(
        @"\[ServiceCategory\(""(?<category>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SuppressedPattern = new(
        @"PublicSurface\s*=\s*false",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Domains reached by a route other than the category switch. <c>file</c> and <c>session</c>
    /// are handled ahead of it.
    /// </summary>
    private static readonly string[] DispatchedElsewhere = ["file", "session"];

    [Fact]
    public void EveryPublicDomain_HasACaseInTheServiceDispatchSwitch()
    {
        var root = FindRepositoryRoot();
        var servicePath = Path.Combine(root, "src", "VisioMcp.Service", "VisioMcpService.cs");

        Assert.True(File.Exists(servicePath), $"Expected the service at '{servicePath}'.");

        var service = File.ReadAllText(servicePath);
        var publicCategories = ReadPublicCategories(root);

        Assert.True(
            publicCategories.Count > 0,
            "No public command categories found — the guard would pass vacuously.");

        var missing = publicCategories
            .Where(category => !DispatchedElsewhere.Contains(category, StringComparer.Ordinal))
            .Where(category => !service.Contains($"\"{category}\" =>", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "These domains are on the public surface but have no case in the dispatch switch in "
            + "src/VisioMcp.Service/VisioMcpService.cs, so every call to them returns "
            + "\"Unknown command category\" even though the MCP tool and CLI command both exist: "
            + string.Join(", ", missing));
    }

    private static List<string> ReadPublicCategories(string root)
    {
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var categories = new List<string>();

        if (!Directory.Exists(commandsRoot))
        {
            return categories;
        }

        foreach (var file in Directory.GetFiles(commandsRoot, "I*Commands.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var category = CategoryPattern.Match(text);

            // The glob also matches implementation classes such as ImageCommands.cs.
            if (!category.Success || SuppressedPattern.IsMatch(text))
            {
                continue;
            }

            categories.Add(category.Groups["category"].Value);
        }

        return categories;
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
