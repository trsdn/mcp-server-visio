using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that prose documentation describes Visio, and that no test carries a PowerPoint-named
/// trait.
///
/// <c>ContributorInstructionTerminologyTests</c> already enforces this, but only under
/// <c>.github/</c>. Everything a contributor or user actually reads first — <c>SECURITY.md</c>,
/// <c>docs/</c>, <c>examples/</c>, the per-project READMEs — was never checked, and it showed:
///
/// <list type="bullet">
/// <item><c>SECURITY.md</c> described the threat model of <c>PowerPoint.Application</c>.</item>
/// <item><c>docs/CONTRIBUTING.md</c> opened by calling this "the go-to command-line tool for
/// coding agents to interact with Microsoft PowerPoint files" and linked the PowerPoint VBA
/// object model.</item>
/// <item><c>docs/DEVELOPMENT.md</c> told contributors to tag tests
/// <c>[Trait("RequiresPowerPoint", "true")]</c>. The convention is <c>RequiresVisio</c>, used by 48
/// tests, and three tests had followed the documentation instead.</item>
/// <item><c>docs/AZURE_SELFHOSTED_RUNNER_SETUP.md</c> told the operator to add the
/// <c>powerpoint</c> label to the integration runner. <c>integration-tests.yml</c> requires
/// <c>[self-hosted, windows, visio]</c>, so a runner built by following the documentation would
/// never have matched the job it exists to run.</item>
/// </list>
///
/// That last one is the pattern worth naming: a mechanical rename fixes the words while leaving
/// the *mechanism* wrong. The same pass first produced "Visio.InvisibleApp runs with
/// <c>Visible=false</c> and <c>DisplayAlerts=false</c>" — plausible, Visio-shaped, and still
/// false, because <c>InvisibleApp</c> is windowless by construction and the code sets
/// <c>AlertResponse = 7</c>.
///
/// Excluded, deliberately: this file. Its comments name the wrong product in order to explain the
/// rule, and scanning itself would let the guard fail on its own explanation — the same
/// self-reference that made an earlier guard vouch for every variable it mentioned.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class ProseDocumentationTerminologyTests
{
    private static readonly string[] ScannedRoots =
    [
        "docs", "examples", "mcpb", "skills", "tests"
    ];

    private static readonly string[] ScannedRootFiles =
    [
        "SECURITY.md", "CONTRIBUTING.md"
    ];

    /// <summary>
    /// These documents exist to explain the migration from the PowerPoint ancestor, so naming it
    /// is the point. <c>FEATURES.md</c> classifies the inherited domains and lists their original
    /// names; <c>README.md</c> states the cleanup strategy; the testing ADR records why a rule was
    /// changed; the archetype pipeline explains which stages were removed and why.
    ///
    /// An allow-list is honest about that, where a cleverer heuristic would be guessing.
    /// </summary>
    private static readonly string[] HistoricalDocuments =
    [
        "FEATURES.md", "README.md", "ADR-001-TESTING-STRATEGY.md", "ARCHETYPE-PIPELINE.md",
        "CHANGELOG.md"
    ];

    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint", "POWERPNT", ".pptx", ".pptm", "IPptBatch"
    ];

    [Fact]
    public void ProseDocumentation_DescribesVisio()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var file in EnumerateProse(root))
        {
            if (HistoricalDocuments.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var term in ForbiddenTerms)
                {
                    if (!lines[index].Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    offenders.Add($"{Path.GetRelativePath(root, file)}:{index + 1}: {lines[index].Trim()}");
                    break;
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Prose documentation still describes PowerPoint. This is what contributors and users "
            + "read first:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Tests_DoNotCarryAPowerPointNamedTrait()
    {
        var root = FindRepositoryRoot();
        var testsRoot = Path.Combine(root, "tests");
        var offenders = new List<string>();

        if (!Directory.Exists(testsRoot))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(file))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains("RequiresPowerPoint", StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{index + 1}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Tests are tagged 'RequiresPowerPoint'. The convention is 'RequiresVisio', so these are "
            + "invisible to the filters used in CI and in the pre-commit hook:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    private static IEnumerable<string> EnumerateProse(string root)
    {
        foreach (var name in ScannedRootFiles)
        {
            var path = Path.Combine(root, name);

            if (File.Exists(path))
            {
                yield return path;
            }
        }

        foreach (var relative in ScannedRoots)
        {
            var directory = Path.Combine(root, relative);

            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories))
            {
                if (!IsExcluded(file))
                {
                    yield return file;
                }
            }
        }
    }

    private static bool IsExcluded(string path)
    {
        if (string.Equals(
                Path.GetFileName(path),
                $"{nameof(ProseDocumentationTerminologyTests)}.cs",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string[] segments =
        [
            $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}TestAssets{Path.DirectorySeparatorChar}"
        ];

        return segments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase));
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
