using Xunit;

namespace VisioMcp.SkillGeneration.Tests;

/// <summary>
/// Guards the reference documents shipped inside both skill packages.
///
/// <c>SkillMdQualityTests</c> already checks <c>SKILL.md</c>, which is generated. The
/// <c>references/</c> folders are not: they are copied verbatim from <c>skills/shared/</c> and
/// installed on a user's machine by <c>npx skills add</c>. Nothing checked them, and they had
/// drifted two migrations behind the product — one file documented <c>powerquery</c>,
/// <c>pivottable</c>, <c>range</c> and <c>slicer</c> commands from the Excel ancestor while
/// claiming to be auto-generated, and another was 9.5 KB of PowerPoint deck-review advice (#23).
/// </summary>
public class SkillReferenceQualityTests
{
    private static readonly string SkillsFolder = Path.Combine(AppContext.BaseDirectory, "skills");

    /// <summary>
    /// Terms describing a different Office product. Migration history may name PowerPoint, but a
    /// reference document that is *about* slides or Excel ranges is stale, not historical.
    /// </summary>
    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint",
        "presentation",
        "pptx",
        "pptm",
        "powerquery",
        "pivottable",
        "worksheetstyle",
        "conditionalformat",
        "namedrange"
    ];

    public static TheoryData<string> ShippedReferenceFiles()
    {
        var data = new TheoryData<string>();

        foreach (var package in new[] { "visio-cli", "visio-mcp" })
        {
            var folder = Path.Combine(SkillsFolder, package, "references");

            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(folder, "*.md"))
            {
                data.Add(Path.Combine(package, "references", Path.GetFileName(file)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedReferenceFiles))]
    public void ShippedReference_DescribesVisioRatherThanAnotherOfficeProduct(string relativePath)
    {
        var fullPath = Path.Combine(SkillsFolder, relativePath);
        var content = File.ReadAllText(fullPath);

        var offenders = ForbiddenTerms
            .Where(term => content.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"'{relativePath}' is installed verbatim by `npx skills add` and mentions: "
            + $"{string.Join(", ", offenders)}. A shipped reference describing a different Office "
            + "product is worse than no reference, because the agent trusts it.");
    }

    [Fact]
    public void BothPackages_ShipTheSameSharedReferences()
    {
        var cli = ReferenceNames("visio-cli");
        var mcp = ReferenceNames("visio-mcp");

        Assert.NotEmpty(cli);
        Assert.NotEmpty(mcp);

        // Each package may add its own host-specific file (cli-commands, claude-desktop), but every
        // file copied from skills/shared must reach both — otherwise the two shipped skills
        // describe different products, which is the defect behind #57.
        var shared = SharedReferenceNames();
        Assert.NotEmpty(shared);

        foreach (var name in shared)
        {
            Assert.Contains(name, cli);
            Assert.Contains(name, mcp);
        }
    }

    [Fact]
    public void NoReference_PointsAtAFileThatNoLongerExists()
    {
        foreach (var package in new[] { "visio-cli", "visio-mcp" })
        {
            var folder = Path.Combine(SkillsFolder, package, "references");

            if (!Directory.Exists(folder))
            {
                continue;
            }

            var present = Directory.GetFiles(folder, "*.md")
                .Select(Path.GetFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var skillPath = Path.Combine(SkillsFolder, package, "SKILL.md");
            var skillText = File.ReadAllText(skillPath);

            foreach (var match in System.Text.RegularExpressions.Regex.Matches(skillText, @"\./references/([\w.\-]+\.md)").Cast<System.Text.RegularExpressions.Match>())
            {
                var target = match.Groups[1].Value;

                Assert.True(
                    present.Contains(target),
                    $"{package}/SKILL.md links to './references/{target}', which is not shipped. "
                    + "A dead link in an installed skill sends the agent looking for guidance that "
                    + "is not there.");
            }
        }
    }

    private static HashSet<string> ReferenceNames(string package)
    {
        var folder = Path.Combine(SkillsFolder, package, "references");

        return Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.md").Select(f => Path.GetFileName(f)!).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
    }

    private static HashSet<string> SharedReferenceNames()
    {
        var folder = Path.Combine(SkillsFolder, "shared");

        return Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.md").Select(f => Path.GetFileName(f)!).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
    }
}
