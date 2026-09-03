using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that the shipped CLI skill documents every public command.
///
/// <c>skills/visio-cli/SKILL.md</c> is what an agent is handed when it installs the skill. Its
/// command reference documented 5 of 15 commands (#57), so a third of the surface was
/// undiscoverable to the very audience the skill exists for — <c>export</c>, <c>layer</c>,
/// <c>window</c>, <c>docproperty</c> and <c>shapealign</c> were simply absent.
///
/// The cause was not neglect. <c>Build-AgentSkills.ps1</c> regenerates that reference from
/// <c>visiocli --help</c>, but looked for the binary under <c>net10.0-windows</c> while the project
/// targets <c>net9.0-windows</c>. It never found it, warned, and returned. The reference stopped
/// tracking the surface from that moment, and nothing failed — the same silent-skip shape as the
/// LLM test harness that read <c>CLI_COMMAND</c> while the gate set <c>VISIO_CLI_COMMAND</c>.
///
/// A guard on the output is the durable fix: whatever breaks the generator next, this fails.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class CliSkillCoverageTests
{
    private static readonly Regex CategoryPattern = new(
        @"ServiceCategory\(""(?<category>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PublicSurfacePattern = new(
        @"PublicSurface\s*=\s*(?<value>true|false)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void CliSkill_DocumentsEveryPublicCommand()
    {
        var root = FindRepositoryRoot();
        var skillPath = Path.Combine(root, "skills", "visio-cli", "SKILL.md");

        Assert.True(File.Exists(skillPath), $"Expected the CLI skill at '{skillPath}'.");

        var skill = File.ReadAllText(skillPath);
        var commands = ReadPublicCommands(root);

        Assert.True(
            commands.Count > 0,
            "No public command categories found — the guard would pass vacuously.");

        var missing = commands
            .Where(command => !Regex.IsMatch(
                skill,
                $@"^###\s+{Regex.Escape(command)}\s*$",
                RegexOptions.Multiline))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"skills/visio-cli/SKILL.md documents {commands.Count - missing.Count} of "
            + $"{commands.Count} public commands. An agent handed this skill cannot discover the "
            + "rest. Missing: "
            + string.Join(", ", missing)
            + Environment.NewLine
            + "Regenerate with scripts/Build-AgentSkills.ps1, or add the section by hand.");
    }

    private static List<string> ReadPublicCommands(string root)
    {
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var commands = new List<string>();

        if (!Directory.Exists(commandsRoot))
        {
            return commands;
        }

        foreach (var file in Directory.GetFiles(commandsRoot, "I*Commands.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var category = CategoryPattern.Match(text);

            if (!category.Success)
            {
                continue;
            }

            // Suppressed domains are compiled but not offered, so the skill should not name them.
            var surface = PublicSurfacePattern.Match(text);

            if (surface.Success
                && string.Equals(surface.Groups["value"].Value, "false", StringComparison.Ordinal))
            {
                continue;
            }

            commands.Add(category.Groups["category"].Value);
        }

        return commands;
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
