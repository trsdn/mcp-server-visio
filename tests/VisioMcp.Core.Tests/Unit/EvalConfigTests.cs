using System.Text.Json;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that every evaluation config names an archetype and instruction files that exist.
///
/// A config is the unit of work for the harness: one archetype, a set of prompts, and the three
/// agent instruction files. When it names an archetype the catalogue does not have, the harness
/// still runs — the builder is asked to produce something no guidance describes, the judge scores
/// it against an expectation nobody wrote, and the resulting number means nothing. The run
/// completes, which is the problem.
///
/// Before #74 there were 37 configs naming slide archetypes — <c>title-slide</c>, <c>waterfall
/// chart</c>, <c>swot-analysis</c> — none of which exist in a diagramming tool, and thirteen of
/// which were one-off A/B experiments.
///
/// Archetype IDs are resolved from <c>archetypes.json</c>, the same file the <c>design</c> tool
/// serves, so this cannot drift from what the runtime offers.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class EvalConfigTests
{
    [Fact]
    public void EveryConfig_NamesAnArchetypeAndInstructionsThatExist()
    {
        var root = FindRepositoryRoot();
        var configDirectory = Path.Combine(root, "eval", "configs");

        Assert.True(Directory.Exists(configDirectory), $"Expected configs at '{configDirectory}'.");

        var archetypeIds = ReadArchetypeIds(root);
        Assert.True(archetypeIds.Count > 0, "No archetypes found — the guard would pass vacuously.");

        var configFiles = Directory.GetFiles(configDirectory, "*.json");
        Assert.True(configFiles.Length > 0, "No configs found — the guard would pass vacuously.");

        var offenders = new List<string>();

        foreach (var file in configFiles)
        {
            var name = Path.GetFileName(file);
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var configRoot = document.RootElement;

            if (!configRoot.TryGetProperty("archetype", out var archetypeElement))
            {
                offenders.Add($"{name}: no 'archetype' property");
                continue;
            }

            var archetype = archetypeElement.GetString();

            if (string.IsNullOrWhiteSpace(archetype) || !archetypeIds.Contains(archetype))
            {
                offenders.Add(
                    $"{name}: archetype '{archetype}' is not in archetypes.json. "
                    + $"Available: {string.Join(", ", archetypeIds.Order(StringComparer.Ordinal))}");
            }

            foreach (var agent in new[] { "builder", "judge", "improver" })
            {
                if (!configRoot.TryGetProperty(agent, out var agentElement)
                    || !agentElement.TryGetProperty("instructionsFile", out var instructionsElement))
                {
                    continue;
                }

                var relative = instructionsElement.GetString();

                if (string.IsNullOrWhiteSpace(relative))
                {
                    continue;
                }

                var absolute = Path.Combine(
                    root, "eval", relative.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(absolute))
                {
                    offenders.Add($"{name}: {agent}.instructionsFile '{relative}' does not exist");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Evaluation configs point at things that do not exist. The harness will still run and "
            + "produce a score that means nothing:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    private static HashSet<string> ReadArchetypeIds(string root)
    {
        var path = Path.Combine(root, "src", "VisioMcp.Core", "Data", "archetypes.json");

        if (!File.Exists(path))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement
            .EnumerateArray()
            .Select(element => element.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.Ordinal);
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
