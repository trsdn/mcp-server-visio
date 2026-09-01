using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Keeps the contributor instructions describing the product this repository actually builds.
///
/// #23 made the argument better than a comment can: <c>.github/copilot-instructions.md</c> opened
/// with *"VisioMcp is a Windows-only toolset for programmatic PowerPoint automation"*, and
/// <c>.github/instructions/</c> carried ~280 further PowerPoint references. Every agent
/// contributing to this repository was told it was building a PowerPoint tool — which is a
/// plausible reason the legacy persisted through several rounds of migration work.
///
/// Instructions are read by agents, so they are an LLM-facing surface in the same sense the MCP
/// descriptions are. This test treats them that way. It is COM-free (Rule 30): a directory walk
/// and a string search.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class ContributorInstructionTerminologyTests
{
    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint",
        "POWERPNT",
        ".pptx",
        ".pptm",
        "IPptBatch",
        "PptSession",
        "PptToolsBase",
        "PptShutdownService",
        "ctx.Presentation"
    ];

    /// <summary>
    /// Lines permitted to name PowerPoint because they describe migration history, which #23's
    /// acceptance criteria explicitly allow. Matched as substrings so wording may be adjusted
    /// without editing this list, but a *new* mention will not match and will fail.
    /// </summary>
    private static readonly string[] MigrationHistoryExemptions =
    [
        "migrating to the PowerPoint PIA"
    ];

    public static TheoryData<string> InstructionFiles()
    {
        var data = new TheoryData<string>();

        var root = Path.Combine(FindRepositoryRoot(), ".github");

        foreach (var file in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories))
        {
            data.Add(Path.GetRelativePath(root, file));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(InstructionFiles))]
    public void InstructionFile_DescribesAVisioProduct(string relativePath)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), ".github", relativePath);
        var lines = File.ReadAllLines(fullPath);

        var offenders = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (MigrationHistoryExemptions.Any(e => line.Contains(e, StringComparison.Ordinal)))
            {
                continue;
            }

            var hit = ForbiddenTerms.FirstOrDefault(t => line.Contains(t, StringComparison.OrdinalIgnoreCase));

            if (hit is not null)
            {
                offenders.Add($"  line {i + 1}: {line.Trim()}");
            }
        }

        Assert.True(
            offenders.Count == 0,
            $".github/{relativePath} tells contributors this is a PowerPoint product:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders)
            + Environment.NewLine
            + "If the mention genuinely describes migration history, add it to MigrationHistoryExemptions.");
    }

    [Fact]
    public void Discovery_FindsInstructionFilesToCheck()
    {
        var files = InstructionFiles();

        // A gate that checks nothing passes vacuously — the failure mode #15 shipped with.
        Assert.True(files.Count >= 15, $"Expected the ~20 .github markdown files, found {files.Count}.");
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
