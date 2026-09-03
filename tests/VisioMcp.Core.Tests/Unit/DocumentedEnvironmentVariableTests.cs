using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that documentation does not name an environment variable that no code reads.
///
/// This repository keeps rediscovering the same failure: prose that describes a capability which
/// does not exist. <c>mcpb/README.md</c> advertised tools that were never built, the stencil
/// catalog listed masters that are not installed (#98), and the reference-catalog fixture pointed
/// at PNGs under a directory that was never committed.
///
/// Environment variables are the sharpest form of it. A contributor who sets
/// <c>VISIO_CLI_COMMAND</c> because the testing instructions say to gets **no error** — the
/// variable is simply ignored and the harness silently uses a different binary than the one they
/// meant to test. That is how <c>Test-LlmRegressionGate.ps1</c> came to build binaries and then
/// evaluate something else entirely: the gate set <c>VISIO_CLI_COMMAND</c> while
/// <c>conftest.py</c> read <c>CLI_COMMAND</c>. Nothing failed. The run was just meaningless.
///
/// So the invariant is: if the docs tell you to set it, something has to read it.
///
/// Excluded, deliberately:
/// <list type="bullet">
/// <item><c>CHANGELOG.md</c> — it records variables that were *removed*, which is its job.</item>
/// <item><c>.github/ISSUE_TEMPLATE</c> — proposals describe code that does not exist yet, and one
/// template lists <c>VISIO_BUSY</c> as a proposed error code rather than a variable.</item>
/// <item>This file — the comments above name variables in order to explain the rule, and scanning
/// itself would let the guard vouch for every name it mentions. That is not hypothetical: the
/// first run of this test passed <c>VISIO_CLI_COMMAND</c> purely because the paragraph above
/// spells it out.</item>
/// </list>
///
/// Known limit: a mention inside any *other* file's comment still counts as a read. Stripping
/// comments across five languages costs more than it returns, and a variable named in code
/// comments is at least visible to whoever maintains that code.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class DocumentedEnvironmentVariableTests
{
    /// <summary>
    /// Uppercase only, so the MCP server identifier <c>visio_mcp_server</c> and the skill name
    /// <c>visio_mcp_skill</c> are not mistaken for variables.
    /// </summary>
    private static readonly Regex EnvironmentVariablePattern = new(
        @"\b(?:PPTMCP|VISIOMCP|VISIO)_[A-Z0-9_]{2,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] ExcludedDirectorySegments =
    [
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}ISSUE_TEMPLATE{Path.DirectorySeparatorChar}"
    ];

    private static readonly string[] ExcludedFileNames = ["CHANGELOG.md"];

    private static readonly string[] CodeFileExtensions =
    [
        "*.cs", "*.mjs", "*.js", "*.py", "*.ps1", "*.psm1", "*.json", "*.yml", "*.yaml"
    ];

    [Fact]
    public void DocumentedEnvironmentVariables_AreReadBySomeCode()
    {
        var root = FindRepositoryRoot();
        var codeText = ReadAllCode(root);
        var offenders = new List<string>();

        foreach (var file in EnumerateDocumentation(root))
        {
            var text = File.ReadAllText(file);

            foreach (Match match in EnvironmentVariablePattern.Matches(text))
            {
                var name = match.Value;

                if (codeText.Contains(name, StringComparison.Ordinal))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(root, file);
                var entry = $"{relative}: {name}";

                if (!offenders.Contains(entry))
                {
                    offenders.Add(entry);
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Documentation names environment variables that no code reads. Either wire them up or "
            + "stop telling contributors to set them — an ignored variable fails silently:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Order(StringComparer.Ordinal)));
    }

    private static IEnumerable<string> EnumerateDocumentation(string root)
    {
        return Directory
            .EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(file => !IsExcluded(file));
    }

    private static string ReadAllCode(string root)
    {
        var builder = new StringBuilder();

        foreach (var extension in CodeFileExtensions)
        {
            foreach (var file in Directory.EnumerateFiles(root, extension, SearchOption.AllDirectories))
            {
                if (IsExcluded(file) || IsThisGuard(file))
                {
                    continue;
                }

                builder.AppendLine(File.ReadAllText(file));
            }
        }

        return builder.ToString();
    }

    private static bool IsThisGuard(string path)
    {
        return string.Equals(
            Path.GetFileName(path),
            $"{nameof(DocumentedEnvironmentVariableTests)}.cs",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcluded(string path)
    {
        if (ExcludedFileNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return ExcludedDirectorySegments.Any(segment =>
            path.Contains(segment, StringComparison.OrdinalIgnoreCase));
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
