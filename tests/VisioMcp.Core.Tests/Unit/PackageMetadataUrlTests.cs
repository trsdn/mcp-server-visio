using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that every GitHub URL in shipped package metadata points at this repository.
///
/// <c>Directory.Build.props</c> set <c>PackageProjectUrl</c> and <c>RepositoryUrl</c> to
/// <c>github.com/torstenmahr/mcp-server-visio</c> (#145). Neither that repository nor the user
/// <c>torstenmahr</c> exists — both return 404. Because <c>Directory.Build.props</c> applies to every
/// project, both published NuGet packages would have carried it, and those two properties become the
/// "Project website" and "Source repository" links on nuget.org. **NuGet package metadata cannot be
/// edited after publication**, so a single wrong character would have been permanent for that version.
///
/// It was caught by a pre-release audit rather than by a test, which is the point of this file.
/// Three dead URLs have now shipped from the same mechanical rename — <c>pptmcpserver.dev</c>, then
/// <c>VisioMcpserver.dev</c> (#121), then this one — and each existing guard was scoped to the file
/// where the previous instance happened to be found:
///
/// <list type="bullet">
/// <item><c>ContributorInstructionTerminologyTests</c> scans <c>.github/**/*.md</c>, markdown only.</item>
/// <item><c>WorkflowTerminologyTests</c> scans <c>.github/workflows/</c>, added by #121.</item>
/// <item><c>ShippedLinks_DoNotPointAtAHostWeDoNotPublish</c> scans an explicit six-file list.</item>
/// </list>
///
/// None covered build properties. This one covers the metadata that reaches a package registry.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class PackageMetadataUrlTests
{
    /// <summary>The only GitHub repository this project may claim to live in.</summary>
    private const string ExpectedRepository = "trsdn/mcp-server-visio";

    /// <summary>
    /// Files whose contents are published to a registry or shipped to a user, and therefore cannot
    /// be corrected in place after release.
    /// </summary>
    private static readonly string[] ShippedMetadataFiles =
    [
        "Directory.Build.props",
        Path.Combine("mcpb", "manifest.json"),
        Path.Combine("vscode-extension", "package.json"),
        Path.Combine("src", "VisioMcp.Agent", "package.json"),
        Path.Combine("packages", "visio-cli-skill", "package.json"),
        Path.Combine("packages", "visio-mcp-skill", "package.json")
    ];

    private static readonly Regex GitHubRepositoryUrl = new(
        @"github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+?)(?:\.git)?(?=[""'<\s/]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ShippedPackageMetadata_OnlyClaimsThisRepository()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();
        var checkedUrls = 0;
        var scannedFiles = 0;

        foreach (var relative in ShippedMetadataFiles)
        {
            var path = Path.Combine(root, relative);

            if (!File.Exists(path))
            {
                continue;
            }

            scannedFiles++;
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in GitHubRepositoryUrl.Matches(lines[index]))
                {
                    checkedUrls++;

                    var actual = $"{match.Groups["owner"].Value}/{match.Groups["repo"].Value}";

                    if (!string.Equals(actual, ExpectedRepository, StringComparison.Ordinal))
                    {
                        offenders.Add($"{relative}:{index + 1}: {match.Value}");
                    }
                }
            }
        }

        // A guard that silently scans nothing is worse than none, because it is trusted. If the
        // files move, this fails rather than passing vacuously - the same contract as
        // FeaturesDocumentAccuracyTests and CliSkillCoverageTests.
        Assert.True(
            scannedFiles >= 4,
            $"Expected to scan the shipped metadata files, found only {scannedFiles}. Have they "
            + "moved or been renamed? Update ShippedMetadataFiles rather than leaving this guard "
            + "scanning nothing.");

        Assert.True(
            checkedUrls > 0,
            "No GitHub URLs were found in the shipped metadata, so this guard checked nothing.");

        Assert.True(
            offenders.Count == 0,
            $"Shipped package metadata references a GitHub repository other than '{ExpectedRepository}'. "
            + "These values are published to NuGet, npm and the VS Code Marketplace, and NuGet "
            + "metadata cannot be edited after release:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
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
