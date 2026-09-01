using System.Diagnostics.CodeAnalysis;
using VisioMcp.ComInterop.Session;

namespace VisioMcp.ComInterop.Tests.Integration.Session;

/// <summary>
/// Provides the shared Visio document used by the session integration tests.
/// </summary>
/// <remarks>
/// The document is produced by Visio itself on first use rather than being checked into the
/// repository as a binary asset. A generated file is guaranteed to be a valid drawing for the
/// installed Visio version, and it removes the risk of the fixture silently drifting away from
/// the format the tests actually exercise.
/// </remarks>
public static class VisioTestFixture
{
    private static readonly Lock Gate = new();
    private static string? _staticTestFile;

    /// <summary>
    /// Returns the path of the shared static test document, creating it on first use.
    /// </summary>
    /// <returns>The absolute path of the shared document.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Visio integration tests are Windows-only.")]
    public static string EnsureStaticTestFile()
    {
        lock (Gate)
        {
            if (_staticTestFile != null && File.Exists(_staticTestFile))
            {
                return _staticTestFile;
            }

            var testFolder = Path.Join(AppContext.BaseDirectory, "Integration", "Session", "TestFiles");
            Directory.CreateDirectory(testFolder);

            var path = Path.Join(testFolder, "batch-test-static.vsdx");

            if (!File.Exists(path))
            {
                // Visio writes the file, so the fixture always matches the installed version's format.
                VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => true);
            }

            _staticTestFile = path;
            return path;
        }
    }

    /// <summary>
    /// Creates a private copy of the shared test document for a single test instance.
    /// </summary>
    /// <param name="namePrefix">Prefix used to make the temporary file recognizable.</param>
    /// <returns>The absolute path of the copy.</returns>
    public static string CreateCopy(string namePrefix)
    {
        var source = EnsureStaticTestFile();
        var copy = Path.Join(Path.GetTempPath(), $"{namePrefix}-{Guid.NewGuid():N}.vsdx");
        File.Copy(source, copy, overwrite: true);
        return copy;
    }
}
