using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that release-generating workflow YAML cannot reintroduce stale PowerPoint branding or
/// dead documentation hosts.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class WorkflowTerminologyTests
{
    private static readonly Regex ForbiddenWorkflowTerminology = new(
        @"PowerPoint|ppt-|mcpserver\.dev",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    [Fact]
    public void WorkflowYaml_DoesNotShipPowerPointBrandingOrDeadHosts()
    {
        var root = FindRepositoryRoot();
        var workflowsRoot = Path.Combine(root, ".github", "workflows");

        Assert.True(
            Directory.Exists(workflowsRoot),
            $"Expected workflow files under '{workflowsRoot}'.");

        var workflowFiles = Directory.GetFiles(workflowsRoot, "*.yml", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            workflowFiles.Count > 0,
            $"Expected at least one workflow YAML file under '{workflowsRoot}'.");

        var offenders = new List<string>();

        foreach (var path in workflowFiles)
        {
            var relative = Path.GetRelativePath(root, path);
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                if (ForbiddenWorkflowTerminology.IsMatch(lines[index]))
                {
                    offenders.Add($"{relative}:{index + 1}: {lines[index].Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Workflow YAML feeds required checks and shipped release surfaces. Do not reintroduce "
            + "PowerPoint branding, ppt-prefixed release assets, or mcpserver.dev links:"
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
