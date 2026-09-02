using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that no runtime message hands an agent or a user the wrong product name.
///
/// #23 covered tool descriptions and #37 covered parameter descriptions. Neither reaches the
/// strings returned when something goes wrong — <c>ServiceResponse.ErrorMessage</c>, thrown
/// exception messages, and log output. Those said things like *"PowerPoint process for session 'X'
/// has died"*, and <c>FileAccessValidator</c> told a user whose <c>.vsdx</c> was locked to close
/// **PowerPoint** while Visio held the lock (#76).
///
/// This surface arguably matters more than the descriptions. A description is read once, while the
/// agent has full context and is choosing a tool. An error is read when the operation has already
/// failed and the agent is deciding how to recover — a degraded state in which the wrong product
/// name sends recovery in the wrong direction.
///
/// Scoped to the runtime layers and the public commands. The suppressed legacy domains are
/// excluded: they describe themselves as "Legacy PowerPoint..." accurately, and do not ship.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class RuntimeMessageTerminologyTests
{
    private static readonly string[] ScannedDirectories =
    [
        Path.Combine("src", "VisioMcp.Service"),
        Path.Combine("src", "VisioMcp.ComInterop"),
        Path.Combine("src", "VisioMcp.McpServer", "Tools"),
        Path.Combine("src", "VisioMcp.CLI")
    ];

    private static readonly string[] ForbiddenTerms = ["PowerPoint", "presentation", "presentations"];

    /// <summary>
    /// Messages that name PowerPoint on purpose, to explain why an action has no Visio equivalent.
    /// Naming the origin is the point; an agent told only "not supported" would retry.
    /// </summary>
    private static readonly string[] IntentionalMentions =
    [
        "has no Visio equivalent",
        "are a PowerPoint feature",
        "Visio pages have no layout inheritance",
        "IsPowerPointVisible"
    ];

    public static TheoryData<string> ScannedFiles()
    {
        var data = new TheoryData<string>();
        var root = FindRepositoryRoot();

        foreach (var relativeDirectory in ScannedDirectories)
        {
            var directory = Path.Combine(root, relativeDirectory);

            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                data.Add(Path.GetRelativePath(root, file));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ScannedFiles))]
    public void RuntimeStrings_DoNotNameTheWrongProduct(string relativePath)
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), relativePath));
        var offenders = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (IntentionalMentions.Any(m => line.Contains(m, StringComparison.Ordinal)))
            {
                continue;
            }

            // Only string literals reach a user or an agent; comments do not.
            foreach (Match literal in Regex.Matches(line, "\"([^\"\\\\]|\\\\.)*\""))
            {
                var hit = ForbiddenTerms.FirstOrDefault(t =>
                    Regex.IsMatch(literal.Value, $@"\b{Regex.Escape(t)}\b", RegexOptions.IgnoreCase));

                if (hit is not null)
                {
                    offenders.Add($"  line {i + 1}: {line.Trim()}");
                    break;
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{relativePath} returns a message naming the wrong product:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders)
            + Environment.NewLine
            + "If the mention is deliberate — explaining that an action has no Visio equivalent — "
            + "add its phrasing to IntentionalMentions.");
    }

    [Fact]
    public void Discovery_FindsRuntimeFilesToScan()
    {
        // A gate that checks nothing passes vacuously — the failure mode #15 shipped with.
        Assert.True(ScannedFiles().Count >= 20, $"Expected to scan the runtime layers, found {ScannedFiles().Count} files.");
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
