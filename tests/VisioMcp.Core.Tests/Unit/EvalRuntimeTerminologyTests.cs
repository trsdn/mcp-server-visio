using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that the evaluation harness does not name PowerPoint.
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
/// Scope was <c>eval/lib/</c> while the agent instructions, configs and prompts were still
/// deck-shaped. All four PRs of #74 have landed, so it now covers the whole directory, including
/// the prose that tells the model what to build.
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
        "eval"
    ];

    /// <summary>
    /// Generated run artifacts are not source, and a private asset root may hold anything.
    /// </summary>
    private static readonly string[] ExcludedSegments =
    [
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}results{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}input{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}data{Path.DirectorySeparatorChar}"
    ];

    private static readonly string[] ScannedExtensions = ["*.mjs", "*.md", "*.json"];

    /// <summary>
    /// <c>POWERPNT</c> is the process name, and its absence is what proves the cleanup path
    /// actually targets the application the harness starts.
    ///
    /// <c>presentation</c> was on this list until the scope widened to include prompt text, where
    /// it matched "a layered application: presentation, application, domain and data layers" — the
    /// standard name for an architecture layer, in a prompt that is entirely correct. A guard that
    /// forces correct domain language to be reworded trains people to weaken the guard. The terms
    /// kept here all name the wrong product unambiguously.
    /// </summary>
    private static readonly string[] ForbiddenTerms =
    [
        "PowerPoint", "POWERPNT", "pptx", "pptm", "slide"
    ];

    [Fact]
    public void EvalHarness_DoesNotNameTheWrongProduct()
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

            foreach (var extension in ScannedExtensions)
            {
                foreach (var file in Directory.GetFiles(directory, extension, SearchOption.AllDirectories))
                {
                    if (ExcludedSegments.Any(segment =>
                            file.Contains(segment, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

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
        }

        Assert.True(
            offenders.Count == 0,
            "The evaluation harness still names PowerPoint. This harness drives Visio:"
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
