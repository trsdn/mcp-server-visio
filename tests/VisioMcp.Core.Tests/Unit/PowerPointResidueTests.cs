using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that no Core command domain reaches for the PowerPoint object model except the ones
/// still openly awaiting a port.
///
/// Visio's <c>Document</c> has no <c>Slides</c> collection and there is no <c>Presentation</c>, so
/// any such call throws <c>RuntimeBinderException</c> the moment it runs. The suppressed domains get
/// away with it only because <c>PublicSurface = false</c> keeps them off both the MCP and CLI
/// surfaces — the code is broken, it simply is not reachable yet.
///
/// The allow-list below is deliberately two-way. A domain outside it may not use the PowerPoint
/// object model, and a domain inside it *must* — so the list cannot quietly outlive the port that
/// clears it. That is the same shape as <c>FeaturesDocumentAccuracyTests</c>, which fails rather
/// than passing vacuously when it finds nothing to check, and it exists because this repository has
/// repeatedly paid for lists that drifted out of step with the code they describe (#38, #57, #117).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class PowerPointResidueTests
{
    /// <summary>
    /// Domains that legitimately still contain PowerPoint calls because their port is open.
    /// Remove an entry as part of the port that eliminates it — the test fails if an entry is
    /// listed but clean, so a stale entry cannot survive.
    /// </summary>
    private static readonly string[] AwaitingPort =
    [
        "Image" // #64
    ];

    private static readonly Regex PowerPointCall = new(
        @"\.Slides\b|ctx\.Presentation\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void OnlyDomainsAwaitingAPort_UseThePowerPointObjectModel()
    {
        var offenders = ScanDomains();

        var unexpected = offenders.Keys
            .Where(domain => !AwaitingPort.Contains(domain, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unexpected.Count == 0,
            "These Core domains call the PowerPoint object model but are not listed as awaiting a "
            + "port. Visio has no Slides collection, so every one of these throws "
            + "RuntimeBinderException when reached: "
            + string.Join(", ", unexpected.Select(d => $"{d} ({offenders[d]} usages)")));
    }

    [Fact]
    public void EveryDomainListedAsAwaitingAPort_StillHasPowerPointCalls()
    {
        var offenders = ScanDomains();

        var stale = AwaitingPort
            .Where(domain => !offenders.ContainsKey(domain))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            stale.Count == 0,
            "These domains are listed as awaiting a port but no longer contain any PowerPoint "
            + "calls. Remove them from AwaitingPort — a list that outlives what it describes is "
            + "worse than none, because it is trusted: "
            + string.Join(", ", stale));
    }

    /// <summary>
    /// Domain folder name -&gt; number of PowerPoint object model usages found in it.
    /// </summary>
    private static Dictionary<string, int> ScanDomains()
    {
        var commandsRoot = Path.Combine(FindRepositoryRoot(), "src", "VisioMcp.Core", "Commands");

        Assert.True(
            Directory.Exists(commandsRoot),
            $"Expected the Core command domains at '{commandsRoot}'.");

        var offenders = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var file in Directory.GetFiles(commandsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var count = PowerPointCall.Matches(File.ReadAllText(file)).Count;

            if (count == 0)
            {
                continue;
            }

            var domain = Path.GetFileName(Path.GetDirectoryName(file))!;
            offenders[domain] = offenders.GetValueOrDefault(domain) + count;
        }

        return offenders;
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
