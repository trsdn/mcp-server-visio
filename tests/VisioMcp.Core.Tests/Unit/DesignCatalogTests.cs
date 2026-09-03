using VisioMcp.Core.Commands.Design;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// The diagram design catalog (#98).
///
/// The catalog previously described PowerPoint slide design — <c>big-number</c>, <c>title-slide</c>,
/// text-density profiles, deck narrative sequences. None of that has a diagram meaning.
///
/// These are unit tests: the catalog is embedded data and needs no Visio (Rule 30). The claim that
/// every named stencil is actually installed is asserted separately, against a live instance.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Design")]
public class DesignCatalogTests
{
    private readonly DesignCommands _design = new();

    [Fact]
    public void ListArchetypes_ReturnsTheDiagramFamilies()
    {
        var listed = _design.ListArchetypes(null!);

        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.Contains(listed.Archetypes, a => a.Id == "flowchart");
        Assert.Contains(listed.Archetypes, a => a.Id == "org-chart");
        Assert.Contains(listed.Archetypes, a => a.Id == "bpmn-process");
    }

    [Fact]
    public void ListArchetypes_HasNoSlideArchetypesLeft()
    {
        var ids = _design.ListArchetypes(null!).Archetypes.Select(a => a.Id).ToList();

        foreach (var slideOnly in new[] { "big-number", "title-slide", "appendix", "quote", "kpi-card-dashboard" })
        {
            Assert.DoesNotContain(slideOnly, ids);
        }
    }

    [Fact]
    public void EveryArchetype_NamesAStencilAndItsMasters()
    {
        // An archetype that does not say what to drop is advice an agent cannot act on.
        foreach (var archetype in _design.ListArchetypes(null!).Archetypes)
        {
            Assert.False(string.IsNullOrWhiteSpace(archetype.Stencil), $"{archetype.Id} names no stencil.");
            Assert.NotEmpty(archetype.Masters);
            Assert.EndsWith(".VSSX", archetype.Stencil, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EveryArchetype_HasADetailFile()
    {
        foreach (var archetype in _design.ListArchetypes(null!).Archetypes)
        {
            Assert.True(archetype.HasDetail, $"{archetype.Id} has no detail file.");
        }
    }

    [Fact]
    public void GetArchetype_ReturnsTheDetailNotJustTheSummary()
    {
        var detail = _design.GetArchetype(null!, "flowchart");

        Assert.True(detail.Success, detail.ErrorMessage);
        Assert.Equal("BASFLO_M.VSSX", detail.Stencil);
        Assert.Contains("Decision", detail.Masters);

        // The detail file is where the layout, build order and anti-patterns live.
        Assert.Contains("Anti-patterns", detail.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryArchetypeDetail_WarnsAboutSomething()
    {
        // A guidance file that only says what to do, never what goes wrong, is a template.
        foreach (var archetype in _design.ListArchetypes(null!).Archetypes)
        {
            var detail = _design.GetArchetype(null!, archetype.Id).Detail;
            Assert.Contains("Anti-patterns", detail, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GetArchetype_WithAnUnknownId_ListsTheKnownOnes()
    {
        var ex = Assert.Throws<ArgumentException>(() => _design.GetArchetype(null!, "big-number"));

        Assert.Contains("flowchart", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StencilCatalog_SeparatesInstalledFromAbsent()
    {
        var catalog = _design.GetStencilCatalog(null!);

        Assert.True(catalog.Success, catalog.ErrorMessage);
        Assert.Contains("BASFLO_M.VSSX", catalog.Content, StringComparison.Ordinal);

        // Naming a stencil that is not installed without saying so is how an agent fails after
        // it has already built the page.
        Assert.Contains("Not installed", catalog.Content, StringComparison.Ordinal);
        Assert.Contains("CROSFN_M.VSSX", catalog.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagramPatterns_CoverTheVisioTechniquesThatMatter()
    {
        var patterns = _design.GetDiagramPatterns(null!);

        Assert.True(patterns.Success, patterns.ErrorMessage);
        foreach (var topic in new[] { "layer(", "set-background", "set-property", "connect-shapes" })
        {
            Assert.Contains(topic, patterns.Content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Registry_TellsAnAgentToConnectRatherThanOnlyPlace()
    {
        // The most common way generated output is wrong while looking right.
        var detail = _design.GetArchetype(null!, "flowchart").Detail;
        var patterns = _design.GetDiagramPatterns(null!).Content;

        Assert.True(
            detail.Contains("connect-shapes", StringComparison.Ordinal)
            || patterns.Contains("connect-shapes", StringComparison.Ordinal));
    }

    [Fact]
    public void ListPalettes_StillWorks()
    {
        var listed = _design.ListPalettes(null!);

        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.NotEmpty(listed.Palettes);
    }

    [Fact]
    public void GetPalette_ReturnsHexColors()
    {
        var first = _design.ListPalettes(null!).Palettes[0].Id;

        var palette = _design.GetPalette(null!, first);

        Assert.True(palette.Success, palette.ErrorMessage);
        Assert.NotEmpty(palette.Colors);
    }

    [Fact]
    public void GetPalette_WithAnUnknownId_ListsTheKnownOnes()
    {
        var ex = Assert.Throws<ArgumentException>(() => _design.GetPalette(null!, "no-such-palette"));

        Assert.Contains("Available:", ex.Message, StringComparison.Ordinal);
    }
}
