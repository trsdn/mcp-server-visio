using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that every fix location named in <c>eval/criteria.md</c> actually exists.
///
/// The rubric's gap table is the harness's only route from "the output was bad" to "change this
/// file". When a row points somewhere that does not exist, the improver agent is sent to a dead
/// end and the loop produces nothing — silently, because a missing target is indistinguishable
/// from a target that needed no change.
///
/// This is not speculative. Before #74 the table named <c>design(get-layout-grid)</c> and
/// <c>design(get-style-profile)</c>. Neither action has ever existed on this server; the real
/// surface is <c>list-archetypes</c>, <c>get-archetype</c>, <c>get-stencil-catalog</c>,
/// <c>get-diagram-patterns</c>, <c>list-palettes</c> and <c>get-palette</c>. Two of six rows were
/// fiction.
///
/// Checks both kinds of target the table uses:
/// <list type="bullet">
/// <item><c>design(action-name)</c> — resolved against the <c>[ServiceAction]</c> attributes on
/// <c>IDesignCommands</c>, so it tracks the real surface rather than a copy of it.</item>
/// <item><c>skills/shared/*.md</c> — resolved against the file system.</item>
/// </list>
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class EvalCriteriaFixLocationTests
{
    private static readonly Regex DesignActionPattern = new(
        @"`design\((?<action>[a-z-]+)\)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SkillFilePattern = new(
        @"`(?<path>skills/shared/[A-Za-z0-9._-]+\.md)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ServiceActionPattern = new(
        @"\[ServiceAction\(""(?<action>[^""]+)""\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void CriteriaFixLocations_NameThingsThatExist()
    {
        var root = FindRepositoryRoot();
        var criteriaPath = Path.Combine(root, "eval", "criteria.md");

        Assert.True(File.Exists(criteriaPath), $"Expected the scoring rubric at '{criteriaPath}'.");

        var criteria = File.ReadAllText(criteriaPath);
        var offenders = new List<string>();

        var availableActions = ReadDesignActions(root);
        Assert.True(
            availableActions.Count > 0,
            "No [ServiceAction] attributes found on IDesignCommands — the guard cannot verify "
            + "anything, which would let it pass vacuously.");

        foreach (Match match in DesignActionPattern.Matches(criteria))
        {
            var action = match.Groups["action"].Value;

            if (!availableActions.Contains(action))
            {
                offenders.Add(
                    $"design({action}) — not a design action. Available: "
                    + string.Join(", ", availableActions.Order(StringComparer.Ordinal)));
            }
        }

        foreach (Match match in SkillFilePattern.Matches(criteria))
        {
            var relative = match.Groups["path"].Value;
            var absolute = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolute))
            {
                offenders.Add($"{relative} — file does not exist");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "eval/criteria.md points improvement rounds at targets that do not exist:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Distinct(StringComparer.Ordinal)));
    }

    /// <summary>
    /// The rubric and the wire contract must agree on how many dimensions there are.
    ///
    /// They did not. <c>criteria.md</c> described ten dimensions and a maximum of 20, the judge
    /// instructions listed nine and a maximum of 18, and <c>JUDGE_DIMENSION_KEYS</c> enforced
    /// nine. Nothing failed: the validator simply rejected or zero-filled whatever the judge
    /// returned for the dimensions it did not know, so a rubric change could silently stop
    /// reaching the score.
    /// </summary>
    [Fact]
    public void CriteriaDimensionCount_MatchesTheWireContract()
    {
        var root = FindRepositoryRoot();

        var criteria = File.ReadAllText(Path.Combine(root, "eval", "criteria.md"));
        var contracts = File.ReadAllText(
            Path.Combine(root, "eval", "lib", "protocol", "contracts.mjs"));

        var rubricDimensions = DimensionHeadingPattern.Matches(criteria).Count;

        var keysBlock = JudgeDimensionKeysPattern.Match(contracts);
        Assert.True(
            keysBlock.Success,
            "JUDGE_DIMENSION_KEYS not found in eval/lib/protocol/contracts.mjs.");

        var contractDimensions = QuotedKeyPattern.Matches(keysBlock.Groups["body"].Value).Count;

        Assert.True(
            rubricDimensions > 0,
            "No numbered dimension headings found in eval/criteria.md — the guard would pass "
            + "vacuously.");

        Assert.True(
            rubricDimensions == contractDimensions,
            $"eval/criteria.md defines {rubricDimensions} dimensions but JUDGE_DIMENSION_KEYS "
            + $"declares {contractDimensions}. The validator caps maxScore at "
            + $"{contractDimensions * 2}, so the extra dimensions would be silently dropped.");

        Assert.Contains($"\"maxScore\": {contractDimensions * 2}", ReadJudgeInstructions(root));
    }

    private static string ReadJudgeInstructions(string root)
    {
        return File.ReadAllText(Path.Combine(root, "eval", "agents", "judge-instructions.md"));
    }

    private static readonly Regex DimensionHeadingPattern = new(
        @"^### \d+\. .+\(0[–-]2\)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex JudgeDimensionKeysPattern = new(
        @"JUDGE_DIMENSION_KEYS\s*=\s*Object\.freeze\(\[(?<body>[\s\S]*?)\]\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex QuotedKeyPattern = new(
        @"""[a-zA-Z]+""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static HashSet<string> ReadDesignActions(string root)
    {
        var interfacePath = Path.Combine(
            root, "src", "VisioMcp.Core", "Commands", "Design", "IDesignCommands.cs");

        if (!File.Exists(interfacePath))
        {
            return [];
        }

        var text = File.ReadAllText(interfacePath);

        return ServiceActionPattern
            .Matches(text)
            .Select(match => match.Groups["action"].Value)
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
