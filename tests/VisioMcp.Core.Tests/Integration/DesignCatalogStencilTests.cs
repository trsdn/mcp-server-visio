using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Design;
using VisioMcp.Core.Commands.Stencil;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Every stencil and master the design catalog names must actually exist (#98).
///
/// This is the assertion that stops the catalog from becoming fiction. A guidance file naming a
/// master that is not installed is worse than no guidance: the agent follows it, builds the page,
/// and fails at the first <c>drop-master</c>.
///
/// Integration test against real Visio (Rule 30) — the claim is about the installed stencils, so
/// it cannot be checked any other way.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Design")]
public sealed class DesignCatalogStencilTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly DesignCommands _design = new();
    private readonly StencilCommands _stencils = new();

    [Fact]
    public void EveryArchetypeStencil_IsInstalled()
    {
        using var batch = CreateDocument();

        foreach (var archetype in _design.ListArchetypes(null!).Archetypes)
        {
            var listed = _stencils.ListMasters(batch, archetype.Stencil);

            Assert.True(
                listed.Success,
                $"Archetype '{archetype.Id}' names stencil '{archetype.Stencil}', which did not open: {listed.ErrorMessage}");
            Assert.NotEmpty(listed.Masters);
        }
    }

    [Fact]
    public void EveryArchetypeMaster_ExistsInItsStencil()
    {
        using var batch = CreateDocument();

        foreach (var archetype in _design.ListArchetypes(null!).Archetypes)
        {
            var available = _stencils.ListMasters(batch, archetype.Stencil)
                .Masters.Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var master in archetype.Masters)
            {
                Assert.True(
                    available.Contains(master),
                    $"Archetype '{archetype.Id}' names master '{master}' in '{archetype.Stencil}', "
                    + $"which has: {string.Join(", ", available.Take(20))}");
            }
        }
    }

    [Fact]
    public void TheStencilsListedAsAbsent_AreActuallyAbsent()
    {
        using var batch = CreateDocument();

        // If one of these ships in a future Visio, the catalog is telling agents to avoid something
        // they could use — a quieter failure than naming a missing stencil, but still wrong.
        var claimedAbsent = new[] { "CROSFN_M.VSSX", "TIMEL_M.VSSX", "VALUE_M.VSSX", "MIND_M.VSSX" };
        var unexpectedlyPresent = new List<string>();

        foreach (var stencil in claimedAbsent)
        {
            try
            {
                if (_stencils.ListMasters(batch, stencil).Success)
                {
                    unexpectedlyPresent.Add(stencil);
                }
            }
            catch (Exception)
            {
                // Absent, as the catalog claims.
            }
        }

        Assert.True(
            unexpectedlyPresent.Count == 0,
            $"stencil-catalog.md lists these as not installed, but they opened: {string.Join(", ", unexpectedlyPresent)}. "
            + "Move them into the installed section.");
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"DesignCatalogStencilTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // The file may still be briefly held after the batch disposes.
            }
        }
    }
}
