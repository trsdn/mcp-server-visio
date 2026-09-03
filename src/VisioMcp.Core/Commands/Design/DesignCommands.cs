using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Data;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Design;

/// <summary>
/// Diagram design guidance, served from the embedded catalog (#98).
/// </summary>
/// <remarks>
/// Every action is a catalog lookup, so none touches Visio COM. The <c>batch</c> parameter is part
/// of the command contract rather than a dependency, which is what lets an agent ask which diagram
/// to draw before it has opened anything.
/// </remarks>
public class DesignCommands : IDesignCommands
{
    public ArchetypeListResult ListArchetypes(IVisioBatch batch)
    {
        var result = new ArchetypeListResult { Success = true };

        foreach (var archetype in DesignCatalogProvider.GetArchetypes())
        {
            result.Archetypes.Add(new ArchetypeListItem
            {
                Id = archetype.Id,
                Name = archetype.Name,
                When = archetype.When,
                Stencil = archetype.Stencil,
                Masters = archetype.Masters,
                Variants = archetype.Variants,
                ExampleTitle = archetype.ExampleTitle,
                HasDetail = DesignCatalogProvider.GetArchetypeDetail(archetype.Id) is not null
            });
        }

        return result;
    }

    public ArchetypeDetailResult GetArchetype(IVisioBatch batch, string archetypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);

        var archetype = DesignCatalogProvider.GetArchetype(archetypeId);
        if (archetype is null)
        {
            var available = DesignCatalogProvider.GetArchetypes().Select(a => a.Id);
            throw new ArgumentException(
                $"Archetype '{archetypeId}' not found. Available: {string.Join(", ", available)}.",
                nameof(archetypeId));
        }

        // The registry is the fallback rather than an empty string: an archetype without its own
        // detail file still has the cross-cutting rules, and returning nothing would read as
        // "no guidance exists".
        var detail = DesignCatalogProvider.GetArchetypeDetail(archetype.Id)
            ?? DesignCatalogProvider.GetArchetypeRegistry();

        return new ArchetypeDetailResult
        {
            Success = true,
            Id = archetype.Id,
            Name = archetype.Name,
            When = archetype.When,
            Stencil = archetype.Stencil,
            Masters = archetype.Masters,
            Variants = archetype.Variants,
            ExampleTitle = archetype.ExampleTitle,
            Detail = detail
        };
    }

    public DesignReferenceResult GetStencilCatalog(IVisioBatch batch)
    {
        return new DesignReferenceResult
        {
            Success = true,
            Content = DesignCatalogProvider.GetStencilCatalog()
        };
    }

    public DesignReferenceResult GetDiagramPatterns(IVisioBatch batch)
    {
        return new DesignReferenceResult
        {
            Success = true,
            Content = DesignCatalogProvider.GetDiagramPatterns()
        };
    }

    public PaletteListResult ListPalettes(IVisioBatch batch)
    {
        var result = new PaletteListResult { Success = true };

        foreach (var palette in DesignCatalogProvider.GetPalettes())
        {
            result.Palettes.Add(new PaletteListItem
            {
                Id = palette.Id,
                Name = palette.Name,
                BestFor = palette.BestFor
            });
        }

        return result;
    }

    public PaletteDetailResult GetPalette(IVisioBatch batch, string paletteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paletteId);

        var palette = DesignCatalogProvider.GetPalette(paletteId);
        if (palette is null)
        {
            var available = DesignCatalogProvider.GetPalettes().Select(p => p.Id);
            throw new ArgumentException(
                $"Palette '{paletteId}' not found. Available: {string.Join(", ", available)}.",
                nameof(paletteId));
        }

        return new PaletteDetailResult
        {
            Success = true,
            Id = palette.Id,
            Name = palette.Name,
            BestFor = palette.BestFor,
            Colors = palette.Colors
        };
    }
}
