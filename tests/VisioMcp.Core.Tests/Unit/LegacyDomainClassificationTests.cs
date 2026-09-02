using System.Reflection;
using VisioMcp.Core.Attributes;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Keeps <c>FEATURES.md</c>'s legacy-domain classification table honest against the code.
///
/// #22 observed that the suppressed domains were "not unmanaged dead code" — they were
/// documented and guarded — but that the real defect was "the absence of a decision and a date".
/// Writing dispositions into a Markdown table fixes that once; it does not keep it fixed. A
/// domain deleted, ported, or newly suppressed leaves the table describing a repository that no
/// longer exists, and a stale disposition table is worse than none because it is trusted.
///
/// These tests make the table an enforced invariant: the set of domains carrying
/// <c>[McpTool(PublicSurface = false)]</c> must equal the set of rows in the table, exactly.
/// They are deliberately COM-free (Rule 30) — reflection over the Core assembly plus a file read.
///
/// <para><b>Known limit.</b> These tests check that a disposition exists, not that it is
/// <i>complete</i>. A domain whose actions split across two verdicts can hide the unresolved half
/// behind one row, and nothing here notices: <c>accessibility</c> concealed two Delete actions
/// behind a Remap row (#77), and <c>design</c> concealed fourteen (#78). Verifying completeness
/// would mean asserting, per action, that a shipped equivalent exists — which the table does not
/// model. When adding a Remap row, name the shipped action it remaps to in the evidence column so
/// a reader can check it by hand.</para>
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class LegacyDomainClassificationTests
{
    private const string TableBeginMarker = "<!-- BEGIN:LEGACY-DOMAIN-CLASSIFICATION -->";
    private const string TableEndMarker = "<!-- END:LEGACY-DOMAIN-CLASSIFICATION -->";

    private static readonly string[] ValidDispositions = ["Port", "Remap", "Delete"];

    /// <summary>
    /// A single parsed row of the classification table.
    /// </summary>
    private sealed record ClassificationRow(string Domain, string Disposition, string Owner, string Evidence, string Tracking);

    [Fact]
    public void EverySuppressedDomain_IsClassifiedInFeaturesMd()
    {
        var suppressed = DiscoverSuppressedDomains();
        var classified = ParseClassificationTable().Select(r => r.Domain).ToHashSet(StringComparer.Ordinal);

        var undocumented = suppressed.Except(classified).OrderBy(d => d, StringComparer.Ordinal).ToList();

        Assert.True(
            undocumented.Count == 0,
            $"These domains are suppressed via [McpTool(PublicSurface = false)] but have no row in the "
            + $"FEATURES.md classification table: {string.Join(", ", undocumented)}. "
            + "Suppressing a domain without recording a disposition is how #22 happened.");
    }

    [Fact]
    public void ClassificationTable_HasNoStaleRows()
    {
        var suppressed = DiscoverSuppressedDomains();
        var classified = ParseClassificationTable();

        var stale = classified
            .Select(r => r.Domain)
            .Where(d => !suppressed.Contains(d))
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"FEATURES.md classifies these domains as suppressed legacy, but they no longer carry "
            + $"[McpTool(PublicSurface = false)] in the tree: {string.Join(", ", stale)}. "
            + "Delete the row if the domain was removed, or move it into the validated table if it "
            + "was published.");
    }

    [Fact]
    public void EveryClassifiedDomain_DeclaresADispositionAnOwnerAndEvidence()
    {
        var rows = ParseClassificationTable();

        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(
                ValidDispositions.Contains(row.Disposition, StringComparer.Ordinal),
                $"Domain '{row.Domain}' has disposition '{row.Disposition}'. "
                + $"Expected one of: {string.Join(", ", ValidDispositions)}.");

            Assert.True(
                row.Owner.StartsWith('@'),
                $"Domain '{row.Domain}' has owner '{row.Owner}'. An owner must be a GitHub handle "
                + "(for example '@trsdn'); #22 asks for accountability, not a placeholder.");

            Assert.True(
                row.Evidence.Length > 0,
                $"Domain '{row.Domain}' records no Visio evidence. State the COM member that does or "
                + "does not exist, so the next reader does not have to re-probe Visio to trust the verdict.");

            Assert.True(
                row.Tracking.Length > 0,
                $"Domain '{row.Domain}' has no tracking reference. Port and Remap need a follow-up "
                + "issue; Delete needs the PR that removes it.");
        }
    }

    /// <summary>
    /// Reflects over Core for interfaces carrying both <c>[ServiceCategory]</c> and
    /// <c>[McpTool(PublicSurface = false)]</c>.
    /// </summary>
    private static HashSet<string> DiscoverSuppressedDomains()
    {
        var assembly = typeof(McpToolAttribute).Assembly;

        var domains = assembly
            .GetTypes()
            .Where(t => t.IsInterface)
            .Select(t => new
            {
                Category = t.GetCustomAttribute<ServiceCategoryAttribute>(),
                Tool = t.GetCustomAttribute<McpToolAttribute>()
            })
            .Where(x => x.Category is not null && x.Tool is not null && !x.Tool!.PublicSurface)
            .Select(x => x.Category!.Category)
            .ToHashSet(StringComparer.Ordinal);

        // A discovery routine that finds nothing must fail loudly rather than vacuously pass — the
        // exact failure mode audit-core-coverage.ps1 shipped with (#15).
        Assert.NotEmpty(domains);

        return domains;
    }

    private static List<ClassificationRow> ParseClassificationTable()
    {
        var featuresPath = Path.Combine(FindRepositoryRoot(), "FEATURES.md");
        Assert.True(File.Exists(featuresPath), $"FEATURES.md not found at '{featuresPath}'.");

        var content = File.ReadAllText(featuresPath);

        var begin = content.IndexOf(TableBeginMarker, StringComparison.Ordinal);
        var end = content.IndexOf(TableEndMarker, StringComparison.Ordinal);

        Assert.True(
            begin >= 0 && end > begin,
            $"FEATURES.md must delimit the legacy-domain classification table with "
            + $"'{TableBeginMarker}' and '{TableEndMarker}' so this gate can parse it without "
            + "guessing at heading text.");

        var block = content[(begin + TableBeginMarker.Length)..end];

        var rows = new List<ClassificationRow>();

        foreach (var rawLine in block.Split('\n'))
        {
            var line = rawLine.Trim();

            if (!line.StartsWith('|'))
            {
                continue;
            }

            var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();

            // Header row, alignment row, and anything malformed are skipped rather than parsed.
            if (cells.Length < 5 || cells[0].Length == 0 || cells[0].StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var domain = cells[0].Trim('`', '*', ' ');

            if (string.Equals(domain, "Domain", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            rows.Add(new ClassificationRow(
                Domain: domain,
                Disposition: cells[1].Trim('`', '*', ' '),
                Owner: cells[2].Trim('`', '*', ' '),
                Evidence: cells[3].Trim(),
                Tracking: cells[4].Trim()));
        }

        return rows;
    }

    /// <summary>
    /// Walks up from the test output directory to the repository root, identified by the pair of
    /// files that only exist together at the root.
    /// </summary>
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
