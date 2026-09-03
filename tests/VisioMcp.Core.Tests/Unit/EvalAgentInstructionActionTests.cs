using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that the evaluation agent instructions only name tool actions that exist.
///
/// The builder and judge instructions are the harness's contract with the model. An instruction
/// that names a non-existent action does not fail loudly — the agent tries it, gets an error,
/// improvises, and the loop scores whatever improvisation produced. The measurement is then of the
/// agent's recovery, not of the guidance under test.
///
/// The rule earns its place from how easily it is broken. While rewriting these files for #74 the
/// first draft told the builder to call <c>page(action: 'rename')</c> and
/// <c>stencil(action: 'open')</c>. Neither exists: the real names are <c>set-name</c>, and
/// <c>drop-master</c> with no separate open step. Both read as entirely plausible, and both came
/// from reasoning about what a diagram tool ought to offer rather than from the interface.
///
/// Actions are resolved from the <c>[ServiceCategory]</c> and <c>[ServiceAction]</c> attributes on
/// the Core interfaces, so this tracks the real surface rather than a copy of it that can drift.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class EvalAgentInstructionActionTests
{
    /// <summary>Matches the MCP form used throughout the instructions: <c>page(action: 'set-name')</c>.</summary>
    private static readonly Regex McpCallPattern = new(
        @"\b(?<tool>[a-z]+)\(action:\s*'(?<action>[a-z0-9-]+)'",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Matches the shorthand form: <c>design(get-archetype)</c>.</summary>
    private static readonly Regex ShorthandCallPattern = new(
        @"\b(?<tool>[a-z]+)\((?<action>[a-z]+-[a-z0-9-]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Matches the CLI form: <c>visiocli shape list-connectors</c>.</summary>
    private static readonly Regex CliCallPattern = new(
        @"visiocli\s+(?<tool>[a-z]+)\s+(?<action>[a-z][a-z0-9-]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CategoryPattern = new(
        @"ServiceCategory\(""(?<category>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ActionPattern = new(
        @"\[ServiceAction\(""(?<action>[^""]+)""\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// <c>session</c> is a CLI-only verb implemented by the daemon rather than a Core category,
    /// and <c>file</c> is the hand-written MCP tool whose actions are an enum, not
    /// <c>[ServiceAction]</c> attributes. Both are checked by other tests.
    /// </summary>
    private static readonly string[] UncheckedTools = ["session", "file"];

    [Fact]
    public void AgentInstructions_OnlyNameActionsThatExist()
    {
        var root = FindRepositoryRoot();
        var agentsDirectory = Path.Combine(root, "eval", "agents");

        Assert.True(
            Directory.Exists(agentsDirectory),
            $"Expected agent instructions at '{agentsDirectory}'.");

        var surface = ReadToolSurface(root);
        Assert.True(surface.Count > 0, "No tool categories found — the guard would pass vacuously.");

        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(agentsDirectory, "*.md", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(root, file);

            foreach (var pattern in new[] { McpCallPattern, ShorthandCallPattern, CliCallPattern })
            {
                foreach (Match match in pattern.Matches(text))
                {
                    var tool = match.Groups["tool"].Value;
                    var action = match.Groups["action"].Value;

                    if (UncheckedTools.Contains(tool) || !surface.TryGetValue(tool, out var actions))
                    {
                        continue;
                    }

                    if (!actions.Contains(action))
                    {
                        offenders.Add(
                            $"{relative}: {tool}({action}) does not exist. "
                            + $"Available: {string.Join(", ", actions.Order(StringComparer.Ordinal))}");
                    }
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Agent instructions name tool actions that do not exist. The agent will try them, "
            + "fail, improvise, and the loop will score the improvisation:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Distinct(StringComparer.Ordinal)));
    }

    private static Dictionary<string, HashSet<string>> ReadToolSurface(string root)
    {
        var commandsDirectory = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var surface = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (!Directory.Exists(commandsDirectory))
        {
            return surface;
        }

        foreach (var file in Directory.GetFiles(commandsDirectory, "I*Commands.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var categoryMatch = CategoryPattern.Match(text);

            if (!categoryMatch.Success)
            {
                continue;
            }

            var category = categoryMatch.Groups["category"].Value;
            var actions = ActionPattern
                .Matches(text)
                .Select(match => match.Groups["action"].Value)
                .ToHashSet(StringComparer.Ordinal);

            surface[category] = actions;
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
