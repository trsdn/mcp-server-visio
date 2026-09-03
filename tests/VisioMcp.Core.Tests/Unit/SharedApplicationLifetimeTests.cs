using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Asserts that no command releases the session's Visio <c>Application</c>.
///
/// <c>ctx.Application</c> is owned by the batch and shared by every command in the session.
/// Releasing it destroys the RCW for the whole session, and each later command that touches the
/// application object fails with <c>InvalidComObjectException: COM object that has been separated
/// from its underlying RCW cannot be used</c> — permanently, until the session is closed.
///
/// The failure is unusually hard to attribute. Commands that work through <c>ctx.Document</c>
/// continue to succeed, so a session looks healthy while an arbitrary subset of it is broken, and
/// the command that caused the damage is not the one that reports it. Reproduced on a fresh
/// session as:
///
/// <code>
/// window get-view  -> RuntimeBinderException  (the call that does the damage)
/// page   list      -> succeeds                (document-based, unaffected)
/// window get-view  -> InvalidComObjectException
/// </code>
///
/// Six actions in <c>WindowCommands</c> did this — <c>get-info</c>, <c>minimize</c>,
/// <c>restore</c>, <c>maximize</c>, <c>set-view</c> and <c>get-view</c> (#109). They were removed
/// rather than repaired, because they targeted PowerPoint's <c>Application.WindowState</c> and
/// <c>Window.ViewType</c>, neither of which Visio has, and Visio's own <c>Window.WindowState</c>
/// silently ignores writes.
///
/// Objects a command <em>fetches</em> — pages, shapes, windows, cells — must still be released;
/// this rule is only about the application it was handed.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Documentation")]
public class SharedApplicationLifetimeTests
{
    [Fact]
    public void NoCommand_ReleasesTheSessionApplication()
    {
        var root = FindRepositoryRoot();
        var commandsRoot = Path.Combine(root, "src", "VisioMcp.Core", "Commands");

        Assert.True(Directory.Exists(commandsRoot), $"Expected commands at '{commandsRoot}'.");

        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(commandsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            var applicationLocals = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                // dynamic app = ctx.Application;  /  dynamic settings = ctx.Application.Settings;
                var assignment = System.Text.RegularExpressions.Regex.Match(
                    line,
                    @"dynamic\??\s+(?<name>\w+)\s*=\s*(?:\(\w+\))?\s*ctx\.Application\s*;");

                if (assignment.Success)
                {
                    applicationLocals.Add(assignment.Groups["name"].Value);
                    continue;
                }

                var release = System.Text.RegularExpressions.Regex.Match(
                    line,
                    @"ComUtilities\.Release\(ref\s+(?<name>\w+)");

                if (release.Success && applicationLocals.Contains(release.Groups["name"].Value))
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(root, file)}:{index + 1}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "A command releases ctx.Application. That object belongs to the session and is shared "
            + "by every command in it: releasing it breaks the session for all later "
            + "application-based commands, while document-based ones keep working — so the damage "
            + "is not attributed to the command that caused it."
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
