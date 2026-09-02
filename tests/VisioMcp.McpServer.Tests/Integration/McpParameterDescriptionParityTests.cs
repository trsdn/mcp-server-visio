using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using VisioMcp.McpServer.Tools;
using Xunit;

namespace VisioMcp.McpServer.Tests.Integration;

/// <summary>
/// Asserts that the MCP schema documents parameters as well as the CLI does.
///
/// The two entry points are generated from the same Core interfaces and the same XML
/// <c>&lt;param&gt;</c> docs, but by different generators in different compilations:
/// <c>ServiceRegistryGenerator</c> runs *inside* the Core compilation and can read XML docs;
/// <c>McpToolGenerator</c> runs in the MCP server compilation where Core is a **metadata
/// reference**, and XML docs are not carried in metadata. The result was that
/// <c>shape(find-by-type)</c>'s <c>shape_type</c> read
/// <c>"Visio VisShapeTypes integer: 1=Page, 2=Group…"</c> in the CLI skill and
/// <c>"(required for: find-by-type)"</c> in the MCP schema — the same parameter, the same source
/// doc, two different answers, with the LLM-facing surface getting the worse one (#37).
///
/// This is a Rule 24 parity invariant: a description available to one entry point must be
/// available to both. No COM is touched.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "McpServer")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class McpParameterDescriptionParityTests
{
    /// <summary>
    /// A description carrying no information beyond "you must pass this for these actions".
    /// </summary>
    private static bool IsPlaceholder(string? description) =>
        string.IsNullOrWhiteSpace(description)
        || Regex.IsMatch(description, @"^\(required(\s+for:[^)]*)?\)$");

    public static TheoryData<string, string, string> DocumentedCliParameters()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (tool, parameter, description) in ParseCliSkillParameters())
        {
            if (!IsPlaceholder(description))
            {
                data.Add(tool, parameter, description);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DocumentedCliParameters))]
    public void ParameterDocumentedForTheCli_IsAlsoDocumentedForMcp(string tool, string cliParameter, string cliDescription)
    {
        var mcpParameter = cliParameter.Replace('-', '_');

        if (!TryGetMcpParameterDescription(tool, mcpParameter, out var mcpDescription))
        {
            // The CLI and MCP surfaces do not expose an identical parameter set (for example the
            // CLI's --session). Absence is #57's concern, not this test's.
            return;
        }

        Assert.False(
            IsPlaceholder(mcpDescription),
            $"'{tool}' parameter '{mcpParameter}' is documented for the CLI but not for MCP."
            + $"{Environment.NewLine}  CLI: {cliDescription}"
            + $"{Environment.NewLine}  MCP: {mcpDescription ?? "(null)"}"
            + $"{Environment.NewLine}The MCP schema is the LLM-facing surface; it must not be the worse-documented one.");
    }

    [Fact]
    public void Discovery_FindsDocumentedCliParametersToCompare()
    {
        var documented = DocumentedCliParameters();

        // A gate that checks nothing passes vacuously — the failure mode #15 shipped with.
        Assert.True(
            documented.Count >= 30,
            $"Expected the CLI skill to document many parameters, found {documented.Count}. "
            + "Either SKILL.md was not generated or the parser no longer matches its format.");
    }

    /// <summary>
    /// Every MCP parameter must say something beyond "you must pass this".
    /// </summary>
    /// <remarks>
    /// This is the ratchet. #37 started at 115 placeholder descriptions out of 151; carrying
    /// descriptions across the metadata boundary and writing the missing <c>&lt;param&gt;</c> docs
    /// took it to zero. Without a gate the rate creeps back the moment someone adds an action,
    /// exactly as the PowerPoint terminology did before #23 put a test on it.
    /// </remarks>
    [Fact]
    public void NoMcpParameter_IsDocumentedOnlyAsRequired()
    {
        var offenders = new List<string>();

        var assembly = typeof(VisioFileTool).Assembly;

        var toolMethods = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false).Length > 0)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

        var checkedCount = 0;

        foreach (var method in toolMethods)
        {
            var toolName = method.GetCustomAttribute<McpServerToolAttribute>()!.Name ?? method.Name;

            foreach (var parameter in method.GetParameters())
            {
                checkedCount++;

                var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;

                if (IsPlaceholder(description))
                {
                    offenders.Add($"  {toolName}.{parameter.Name}: {description ?? "(no description)"}");
                }
            }
        }

        Assert.True(checkedCount > 100, $"Expected to check the full MCP parameter surface, saw {checkedCount}.");

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} MCP parameter(s) carry no description beyond a required-for suffix:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders)
            + Environment.NewLine
            + "Add a <param> doc on the Core interface; the generator carries it through.");
    }

    private static bool TryGetMcpParameterDescription(string toolName, string parameterName, out string? description)
    {
        description = null;

        var assembly = typeof(VisioFileTool).Assembly;

        var method = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false).Length > 0)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .FirstOrDefault(m => string.Equals(m.GetCustomAttribute<McpServerToolAttribute>()?.Name, toolName, StringComparison.Ordinal));

        var parameter = method?.GetParameters()
            .FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.Ordinal));

        if (parameter is null)
        {
            return false;
        }

        description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return true;
    }

    /// <summary>
    /// Reads the generated CLI skill's per-tool parameter tables.
    /// </summary>
    private static List<(string Tool, string Parameter, string Description)> ParseCliSkillParameters()
    {
        var skillPath = Path.Combine(FindRepositoryRoot(), "skills", "visio-cli", "SKILL.md");
        Assert.True(File.Exists(skillPath), $"Generated CLI skill not found at '{skillPath}'.");

        var results = new List<(string, string, string)>();
        var currentTool = string.Empty;

        foreach (var rawLine in File.ReadAllLines(skillPath))
        {
            var line = rawLine.Trim();

            var heading = Regex.Match(line, @"^###\s+([a-z][a-z0-9]*)$");
            if (heading.Success)
            {
                currentTool = heading.Groups[1].Value;
                continue;
            }

            if (currentTool.Length == 0 || !line.StartsWith('|'))
            {
                continue;
            }

            var row = Regex.Match(line, @"^\|\s*`--([a-z0-9-]+)`\s*\|(.*)\|$");
            if (!row.Success)
            {
                continue;
            }

            results.Add((currentTool, row.Groups[1].Value, row.Groups[2].Value.Trim()));
        }

        return results;
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
