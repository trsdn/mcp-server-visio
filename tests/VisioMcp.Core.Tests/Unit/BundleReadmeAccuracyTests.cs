using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that the README shipped inside the Claude Desktop bundle states the real size of the
/// tool surface, and that no shipped link points at a host we do not publish.
///
/// This guard exists because <c>mcpb/README.md</c> — the first thing anyone reads after installing
/// the bundle — opened with *"# PowerPoint (Windows)"* and claimed **"25 tools with 225
/// operations"** for a Visio server with 15 tools. The manifest beside it was already correct, so
/// nothing flagged the discrepancy.
///
/// It also caught a second defect: <c>homepage</c>, <c>documentation</c> and
/// <c>privacy_policies</c> in the shipped manifest all pointed at <c>VisioMcpserver.dev</c>, a
/// domain produced by a mechanical rename of the PowerPoint ancestor's <c>pptmcpserver.dev</c>. It
/// does not resolve, and neither does GitHub Pages for this repository. Users clicking "Docs" in
/// the CLI tray reached nothing.
///
/// Counting rule: the fourteen generated tools contribute their <c>[ServiceAction]</c> attributes,
/// and the hand-written <c>file</c> tool contributes its action enum instead of
/// <c>IFileCommands</c>, which exposes only <c>test</c>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class BundleReadmeAccuracyTests
{
    /// <summary>
    /// Domains are discovered from <c>PublicSurface</c> rather than listed here. A hardcoded list
    /// would have to be edited by hand every time a domain is published or suppressed, and a guard
    /// that needs manual updating to stay honest is the thing this repository keeps being caught by
    /// (#38, #57, #117).
    /// </summary>
    private static readonly Regex SuppressedPattern = new(
        @"PublicSurface\s*=\s*false",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Marks a file as a domain interface. Needed because the glob <c>I*Commands.cs</c> also
    /// matches implementation classes whose name happens to start with I — <c>ImageCommands.cs</c>
    /// is the live example.
    /// </summary>
    private static readonly Regex ServiceCategoryPattern = new(
        @"\[ServiceCategory\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ServiceActionPattern = new(
        @"\[ServiceAction\(""[^""]+""\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FileToolActionPattern = new(
        @"JsonStringEnumMemberName\(""[^""]+""\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ClaimPattern = new(
        @"\*\*(?<tools>\d+) tools with (?<actions>\d+) actions\*\*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void BundleReadme_StatesTheRealSurfaceSize()
    {
        var root = FindRepositoryRoot();
        var readmePath = Path.Combine(root, "mcpb", "README.md");

        Assert.True(File.Exists(readmePath), $"Expected the bundle README at '{readmePath}'.");

        var claim = ClaimPattern.Match(File.ReadAllText(readmePath));

        Assert.True(
            claim.Success,
            "The bundle README no longer states its surface size in the form "
            + "'**N tools with M actions**'. Either restore the claim or delete this guard — do "
            + "not leave an unchecked number in the first thing a user reads.");

        var claimedTools = int.Parse(claim.Groups["tools"].Value, CultureInfo.InvariantCulture);
        var claimedActions = int.Parse(claim.Groups["actions"].Value, CultureInfo.InvariantCulture);

        var (actualTools, actualActions) = CountPublicSurface(root);

        Assert.True(
            claimedTools == actualTools && claimedActions == actualActions,
            $"mcpb/README.md claims {claimedTools} tools with {claimedActions} actions; the public "
            + $"surface is {actualTools} tools with {actualActions} actions. This README ships "
            + "inside the bundle and is the first thing a user reads.");
    }

    [Fact]
    public void ShippedLinks_DoNotPointAtAHostWeDoNotPublish()
    {
        var root = FindRepositoryRoot();

        // There is no GitHub Pages site for this repository: the Pages API returns 404 and
        // trsdn.github.io/mcp-server-visio does not resolve. Until one exists, every documentation
        // link must resolve inside the repository itself.
        string[] shippedFiles =
        [
            Path.Combine("mcpb", "README.md"),
            Path.Combine("mcpb", "manifest.json"),
            Path.Combine("skills", "visio-cli", "README.md"),
            Path.Combine("skills", "visio-mcp", "README.md"),
            Path.Combine("src", "VisioMcp.Agent", "package.json"),
            Path.Combine("src", "VisioMcp.CLI", "Infrastructure", "CliServiceTray.cs")
        ];

        var offenders = new List<string>();

        foreach (var relative in shippedFiles)
        {
            var path = Path.Combine(root, relative);

            if (!File.Exists(path))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains("mcpserver.dev", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{relative}:{index + 1}: {lines[index].Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Shipped files link to a host that does not resolve. There is no GitHub Pages site for "
            + "this repository, so documentation links must point into the repository:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    private static (int Tools, int Actions) CountPublicSurface(string root)
    {
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");
        var tools = 0;
        var actions = 0;

        Assert.True(
            Directory.Exists(commandsRoot),
            $"Expected the Core command domains at '{commandsRoot}'.");

        foreach (var interfaceFile in Directory.GetFiles(commandsRoot, "I*Commands.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(interfaceFile);

            // The glob also matches implementation classes such as ImageCommands.cs.
            if (!ServiceCategoryPattern.IsMatch(text))
            {
                continue;
            }

            // Suppressed domains are compiled but not offered, so they are not part of the surface.
            if (SuppressedPattern.IsMatch(text))
            {
                continue;
            }

            tools++;

            var domain = Path.GetFileName(Path.GetDirectoryName(interfaceFile))!;

            // The hand-written file tool replaces IFileCommands, which exposes only 'test'.
            if (string.Equals(domain, "File", StringComparison.Ordinal))
            {
                actions += CountFileToolActions(root);
                continue;
            }

            actions += ServiceActionPattern.Matches(text).Count;
        }

        Assert.True(tools > 0, "No public command domains were discovered — the guard would pass vacuously.");

        return (tools, actions);
    }

    private static int CountFileToolActions(string root)
    {
        var path = Path.Combine(root, "src", "VisioMcp.McpServer", "Tools", "VisioFileTool.cs");

        return File.Exists(path)
            ? FileToolActionPattern.Matches(File.ReadAllText(path)).Count
            : 0;
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
