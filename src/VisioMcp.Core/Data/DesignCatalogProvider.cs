using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace VisioMcp.Core.Data;

/// <summary>
/// Provides access to the embedded diagram design catalog (archetypes, palettes, style profiles,
/// stencil catalog, diagram patterns). Data is loaded once and cached for the process lifetime.
/// </summary>
internal static class DesignCatalogProvider
{
    private static readonly Assembly Assembly = typeof(DesignCatalogProvider).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static List<ArchetypeEntry>? _archetypes;
    private static List<PaletteEntry>? _palettes;
    private static string? _archetypeRegistry;
    private static readonly ConcurrentDictionary<string, string?> _archetypeDetails = new(StringComparer.OrdinalIgnoreCase);
    private static string? _diagramPatterns;
    private static string? _stencilCatalog;

    // ── Archetypes ─────────────────────────────────────

    public static List<ArchetypeEntry> GetArchetypes()
    {
        _archetypes ??= LoadJson<List<ArchetypeEntry>>("archetypes.json");
        return _archetypes;
    }

    public static ArchetypeEntry? GetArchetype(string archetypeId)
    {
        return GetArchetypes().Find(a => string.Equals(a.Id, archetypeId, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetArchetypeRegistry()
    {
        _archetypeRegistry ??= LoadText("archetypes.registry.md");
        return _archetypeRegistry;
    }

    public static string? GetArchetypeDetail(string archetypeId)
    {
        return _archetypeDetails.GetOrAdd(archetypeId, id => TryLoadText($"archetypes.{id}.md"));
    }

    // ── Color Palettes ─────────────────────────────────

    public static List<PaletteEntry> GetPalettes()
    {
        _palettes ??= LoadJson<List<PaletteEntry>>("color-palettes.json");
        return _palettes;
    }

    public static PaletteEntry? GetPalette(string paletteId)
    {
        return GetPalettes().Find(p => string.Equals(p.Id, paletteId, StringComparison.OrdinalIgnoreCase));
    }

    // ── Diagram Patterns ───────────────────────────────

    public static string GetDiagramPatterns()
    {
        _diagramPatterns ??= LoadText("diagram-patterns.md");
        return _diagramPatterns;
    }

    // ── Stencil Catalog ────────────────────────────────

    public static string GetStencilCatalog()
    {
        _stencilCatalog ??= LoadText("stencil-catalog.md");
        return _stencilCatalog;
    }

    // ── Internal helpers ───────────────────────────────

    private static T LoadJson<T>(string fileName) where T : class
    {
        var resourceName = GetResourceName(fileName);
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize: {resourceName}");
    }

    private static string LoadText(string fileName)
    {
        var resourceName = GetResourceName(fileName);
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? TryLoadText(string fileName)
    {
        var resourceName = GetResourceName(fileName);
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetResourceName(string fileName)
    {
        return $"VisioMcp.Core.Data.{fileName.Replace('\\', '.').Replace('/', '.')}";
    }
}

// ── Data models ────────────────────────────────────────

internal sealed class ArchetypeEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string When { get; set; } = "";

    /// <summary>Stencil file to drop this archetype's masters from, such as BASFLO_M.VSSX.</summary>
    public string Stencil { get; set; } = "";

    /// <summary>Master names within that stencil, verified present on a stock Visio install.</summary>
    public List<string> Masters { get; set; } = [];

    public List<string> Variants { get; set; } = [];
    public string ExampleTitle { get; set; } = "";
}

internal sealed class PaletteEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string BestFor { get; set; } = "";
    public Dictionary<string, string> Colors { get; set; } = [];
}
