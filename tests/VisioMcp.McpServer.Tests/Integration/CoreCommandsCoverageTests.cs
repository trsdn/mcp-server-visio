using System.Reflection;
using VisioMcp.Core.Attributes;
using VisioMcp.Generated;
using Xunit;

namespace VisioMcp.McpServer.Tests.Integration;

/// <summary>
/// CRITICAL: Automated verification that every Core Commands method is exposed via a generated action,
/// and that every generated action maps to an action string.
///
/// These tests discover categories by reflection instead of hard-coding one test per domain, so they
/// stay correct when a command domain is added, renamed, or removed.
/// </summary>
public class CoreCommandsCoverageTests
{
    private static readonly Type RegistryType = typeof(ServiceRegistry);

    /// <summary>
    /// Every interface marked with [ServiceCategory] must have a generated registry class
    /// whose action enum covers all of its [ServiceAction] methods.
    /// </summary>
    [Theory]
    [MemberData(nameof(ServiceCategoryInterfaces))]
    public void ServiceCategoryInterface_AllMethodsHaveEnumValues(string categoryPascal, string interfaceName)
    {
        var interfaceType = ResolveInterface(interfaceName);
        var registry = ResolveRegistryClass(categoryPascal);

        Assert.True(registry != null,
            $"{interfaceName} declares category '{categoryPascal}' but ServiceRegistry.{categoryPascal} was not generated.");

        var actionEnum = ResolveActionEnum(registry!);
        Assert.True(actionEnum != null,
            $"ServiceRegistry.{categoryPascal} has no ToActionString(enum) method, so its action enum cannot be resolved.");

        var coreMethodCount = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Count(m => m.GetCustomAttributes(typeof(ServiceActionAttribute), false).Length > 0);
        var enumValueCount = Enum.GetValues(actionEnum!).Length;

        Assert.True(enumValueCount >= coreMethodCount,
            $"{interfaceName} has {coreMethodCount} [ServiceAction] methods but {actionEnum!.Name} has only {enumValueCount} enum values.");
    }

    /// <summary>
    /// Rule 15: every generated action enum value must have a ToActionString mapping.
    /// A missing mapping throws at runtime instead of returning JSON to the MCP client.
    /// </summary>
    [Theory]
    [MemberData(nameof(RegistryCategories))]
    public void ActionEnum_AllValuesHaveMappings(string categoryPascal)
    {
        var registry = ResolveRegistryClass(categoryPascal)!;
        var toActionString = ResolveToActionString(registry)!;
        var actionEnum = ResolveActionEnum(registry)!;

        foreach (var action in Enum.GetValues(actionEnum))
        {
            var mapped = InvokeToActionString(toActionString, action, categoryPascal);
            Assert.False(string.IsNullOrEmpty(mapped),
                $"ServiceRegistry.{categoryPascal}.ToActionString({action}) returned an empty string.");
        }
    }

    /// <summary>
    /// Guards against PowerPoint-era leftovers silently reappearing in the Core command surface.
    /// </summary>
    [Fact]
    public void CoreCommands_ContainNoPresentationEraCategories()
    {
        var presentationOnly = new[]
        {
            "Slide", "Slideshow", "Slideimport", "Slidetable", "Master", "Transition",
            "Animation", "Notes", "Placeholder", "Customshow", "Headerfooter", "Smartart",
        };

        var present = RegistryType.GetNestedTypes(BindingFlags.Public)
            .Select(t => t.Name)
            .Intersect(presentationOnly, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(present.Count == 0,
            $"PowerPoint-era command categories are back on the generated surface: {string.Join(", ", present)}.");
    }

    /// <summary>
    /// Guards against PowerPoint-era leftovers silently reappearing as individual actions.
    /// Every name below routes through Document.Slides, TextFrame, ActionSettings, SlideRange
    /// or ppViewType, none of which exist on a Visio document.
    /// </summary>
    [Fact]
    public void ActionEnums_ContainNoPresentationEraActions()
    {
        var forbidden = new[]
        {
            // Shape: PowerPoint drawing-effect and slide-scoped actions
            "SetFill", "ReadFill", "SetLine", "ReadLine", "SetRotation", "SetShadow", "ReadShadow",
            "SetGradientFill", "SetGlow", "SetReflection", "SetOpacity", "SetSoftEdge", "Set3D",
            "AddTextEffect", "Scale", "Flip", "SetTextFrame", "SetAltText", "CopyToSlide",
            "CopyFormatting", "FindByType", "SetLockAspectRatio", "SetActionSettings",
            // Text: PowerPoint TextFrame/placeholder-scoped actions
            "Format", "FormatAdvanced", "SetSpacing", "ReadSpacing", "SetBullets", "ReadBullets",
            "InsertLink", "ChangeCase", "InsertSymbol", "InsertDateTime", "InsertSlideNumber",
            "AltTextAudit", "EmptyPlaceholderAudit",
            // Window: ppViewType-based view switching (SlideSorter/NotesPage/SlideMaster)
            "SetView", "GetView",
        };

        var offenders = new List<string>();
        foreach (var nested in RegistryType.GetNestedTypes(BindingFlags.Public))
        {
            var actionEnum = ResolveActionEnum(nested);
            if (actionEnum == null)
                continue;

            foreach (var name in Enum.GetNames(actionEnum))
            {
                if (forbidden.Contains(name, StringComparer.Ordinal))
                    offenders.Add($"{actionEnum.Name}.{name}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Visio-era actions are still on the generated surface and would fail at runtime against Visio: "
            + string.Join(", ", offenders));
    }

    public static TheoryData<string, string> ServiceCategoryInterfaces()
    {
        var data = new TheoryData<string, string>();
        foreach (var type in typeof(ServiceCategoryAttribute).Assembly.GetTypes())
        {
            if (!type.IsInterface)
                continue;

            var attr = type.GetCustomAttribute<ServiceCategoryAttribute>();
            if (attr == null)
                continue;

            data.Add(attr.PascalName ?? ToPascalCase(attr.Category), type.FullName!);
        }

        return data;
    }

    public static TheoryData<string> RegistryCategories()
    {
        var data = new TheoryData<string>();
        foreach (var nested in RegistryType.GetNestedTypes(BindingFlags.Public))
        {
            if (ResolveToActionString(nested) != null && ResolveActionEnum(nested) != null)
                data.Add(nested.Name);
        }

        return data;
    }

    private static Type ResolveInterface(string interfaceName)
    {
        var type = typeof(ServiceCategoryAttribute).Assembly.GetType(interfaceName);
        Assert.True(type != null, $"Interface {interfaceName} could not be resolved.");
        return type!;
    }

    private static Type? ResolveRegistryClass(string categoryPascal) =>
        RegistryType.GetNestedTypes(BindingFlags.Public)
            .FirstOrDefault(t => string.Equals(t.Name, categoryPascal, StringComparison.OrdinalIgnoreCase));

    private static MethodInfo? ResolveToActionString(Type registry) =>
        registry.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "ToActionString"
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType.IsEnum);

    private static Type? ResolveActionEnum(Type registry) =>
        ResolveToActionString(registry)?.GetParameters()[0].ParameterType;

    private static string? InvokeToActionString(MethodInfo toActionString, object action, string categoryPascal)
    {
        try
        {
            return (string?)toActionString.Invoke(null, [action]);
        }
        catch (TargetInvocationException ex)
        {
            Assert.Fail($"ServiceRegistry.{categoryPascal}.ToActionString({action}) threw {ex.InnerException?.GetType().Name}: "
                + $"{ex.InnerException?.Message}. Rule 15: every enum value needs a mapping.");
            throw;
        }
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
