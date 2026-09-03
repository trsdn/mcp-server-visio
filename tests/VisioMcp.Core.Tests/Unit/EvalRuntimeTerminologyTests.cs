using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that the evaluation harness's shared library layer does not name PowerPoint.
///
/// <c>eval/lib/</c> is the part of the harness that is domain-neutral by design — the
/// orchestrator, ledger, metrics and protocol contracts. It carried PowerPoint anyway, in the
/// places that matter most: the runtime polled for a <c>POWERPNT</c> process while the toolchain
/// starts <c>VISIO</c>, so it counted zero of the wrong thing and cleaned up nothing; and the
/// artifact contract threaded a <c>pptxPath</c> through every record.
///
/// A stale field name here is not cosmetic. The contract is passed by object literal, so renaming
/// one side without the other produces <c>undefined</c> rather than an error, and the harness
/// carries on scoring an artifact it never located.
///
/// Scope is deliberately <c>eval/lib/</c> only. The agent instructions, configs, prompts and
/// entry-point prose are still deck-shaped and are rewritten in the remaining PRs of #74; widen
/// this list as each lands, so the parts already ported cannot regress while the rest is in
/// flight.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class EvalRuntimeTerminologyTests
{
    private static readonly string[] ScannedDirectories =
    [
        Path.Combine("eval", "lib")
    ];

    /// <summary>
    /// <c>POWERPNT</c> is the process name, and its absence is what proves the cleanup path
    /// actually targets the application the harness starts.
    /// </summary>
    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint", "POWERPNT", "pptx", "pptm", "presentation"
    ];

    [Fact]
    public void EvalLibrary_DoesNotNameTheWrongProduct()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var relativeDirectory in ScannedDirectories)
        {
            var directory = Path.Combine(root, relativeDirectory);

            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(directory, "*.mjs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);

                for (var index = 0; index < lines.Length; index++)
                {
                    foreach (var term in ForbiddenTerms)
                    {
                        if (!lines[index].Contains(term, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var relative = Path.GetRelativePath(root, file);
                        offenders.Add($"{relative}:{index + 1}: {lines[index].Trim()}");
                        break;
                    }
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "The evaluation library still names PowerPoint. This layer drives Visio:"
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
