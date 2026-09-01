// Suppress IDE0005 (unnecessary using) – explicit usings kept for clarity in test reflection code
#pragma warning disable IDE0005
using System.Reflection;
using VisioMcp.Core.Commands.Accessibility;
using VisioMcp.Core.Commands.Background;
using VisioMcp.Core.Commands.Comment;
using VisioMcp.Core.Commands.Design;
using VisioMcp.Core.Commands.DocumentProperty;
using VisioMcp.Core.Commands.Export;
using VisioMcp.Core.Commands.File;
using VisioMcp.Core.Commands.HeaderFooter;
using VisioMcp.Core.Commands.Hyperlink;
using VisioMcp.Core.Commands.Image;
using VisioMcp.Core.Commands.Master;
using VisioMcp.Core.Commands.PageSetup;
using VisioMcp.Core.Commands.PrintOptions;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Commands.ShapeAlign;
using VisioMcp.Core.Commands.Tag;
using VisioMcp.Core.Commands.Text;
using VisioMcp.Core.Commands.Vba;
using VisioMcp.Core.Commands.Window;
#pragma warning restore IDE0005
using VisioMcp.Generated;
using Xunit;

namespace VisioMcp.McpServer.Tests.Integration;

/// <summary>
/// CRITICAL: Automated verification that all Core Commands methods are exposed via generated actions.
/// These tests PREVENT regression by ensuring compile-time and runtime coverage.
/// </summary>
public class CoreCommandsCoverageTests
{
    // ── Existing coverage tests ──────────────────────────────

    [Fact]
    public void IShapeCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IShapeCommands));
        var enumValueCount = Enum.GetValues<ShapeAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IShapeCommands has {coreMethodCount} [ServiceAction] methods but ShapeAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void ITextCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(ITextCommands));
        var enumValueCount = Enum.GetValues<TextAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"ITextCommands has {coreMethodCount} [ServiceAction] methods but TextAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IMasterCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IMasterCommands));
        var enumValueCount = Enum.GetValues<MasterAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IMasterCommands has {coreMethodCount} [ServiceAction] methods but MasterAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IExportCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IExportCommands));
        var enumValueCount = Enum.GetValues<ExportAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IExportCommands has {coreMethodCount} [ServiceAction] methods but ExportAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IImageCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IImageCommands));
        var enumValueCount = Enum.GetValues<ImageAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IImageCommands has {coreMethodCount} [ServiceAction] methods but ImageAction has only {enumValueCount} enum values.");
    }

    // ── NEW: Coverage tests for previously untested command areas ──

    [Fact]
    public void IDesignCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IDesignCommands));
        var enumValueCount = Enum.GetValues<DesignAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IDesignCommands has {coreMethodCount} [ServiceAction] methods but DesignAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IDocumentPropertyCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IDocumentPropertyCommands));
        var enumValueCount = Enum.GetValues<DocpropertyAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IDocumentPropertyCommands has {coreMethodCount} [ServiceAction] methods but DocpropertyAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IHyperlinkCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IHyperlinkCommands));
        var enumValueCount = Enum.GetValues<HyperlinkAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IHyperlinkCommands has {coreMethodCount} [ServiceAction] methods but HyperlinkAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IVbaCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IVbaCommands));
        var enumValueCount = Enum.GetValues<VbaAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IVbaCommands has {coreMethodCount} [ServiceAction] methods but VbaAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IWindowCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IWindowCommands));
        var enumValueCount = Enum.GetValues<WindowAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IWindowCommands has {coreMethodCount} [ServiceAction] methods but WindowAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IFileCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IFileCommands));
        var enumValueCount = Enum.GetValues<FileAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IFileCommands has {coreMethodCount} [ServiceAction] methods but FileAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void ICommentCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(ICommentCommands));
        var enumValueCount = Enum.GetValues<CommentAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"ICommentCommands has {coreMethodCount} [ServiceAction] methods but CommentAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IBackgroundCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IBackgroundCommands));
        var enumValueCount = Enum.GetValues<BackgroundAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IBackgroundCommands has {coreMethodCount} [ServiceAction] methods but BackgroundAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IHeaderFooterCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IHeaderFooterCommands));
        var enumValueCount = Enum.GetValues<HeaderfooterAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IHeaderFooterCommands has {coreMethodCount} [ServiceAction] methods but HeaderfooterAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IShapeAlignCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IShapeAlignCommands));
        var enumValueCount = Enum.GetValues<ShapealignAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IShapeAlignCommands has {coreMethodCount} [ServiceAction] methods but ShapealignAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IPageSetupCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IPageSetupCommands));
        var enumValueCount = Enum.GetValues<PagesetupAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IPageSetupCommands has {coreMethodCount} [ServiceAction] methods but PagesetupAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void ITagCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(ITagCommands));
        var enumValueCount = Enum.GetValues<TagAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"ITagCommands has {coreMethodCount} [ServiceAction] methods but TagAction has only {enumValueCount} enum values.");
    }

    // ── Existing mapping tests ───────────────────────────────

    /// <summary>
    /// Verifies all generated action enums have ToActionString mappings via ServiceRegistry.
    /// </summary>
    [Fact]
    public void ShapeAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<ShapeAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Shape.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Shape.ToActionString(action));
        }
    }

    [Fact]
    public void TextAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<TextAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Text.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Text.ToActionString(action));
        }
    }

    // ── NEW: Mapping tests for previously untested action enums ──

    [Fact]
    public void DesignAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<DesignAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Design.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Design.ToActionString(action));
        }
    }

    [Fact]
    public void DocpropertyAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<DocpropertyAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Docproperty.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Docproperty.ToActionString(action));
        }
    }

    [Fact]
    public void HyperlinkAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<HyperlinkAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Hyperlink.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Hyperlink.ToActionString(action));
        }
    }

    [Fact]
    public void VbaAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<VbaAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Vba.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Vba.ToActionString(action));
        }
    }

    [Fact]
    public void WindowAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<WindowAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Window.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Window.ToActionString(action));
        }
    }

    [Fact]
    public void MasterAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<MasterAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Master.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Master.ToActionString(action));
        }
    }

    [Fact]
    public void ExportAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<ExportAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Export.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Export.ToActionString(action));
        }
    }

    [Fact]
    public void ImageAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<ImageAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Image.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Image.ToActionString(action));
        }
    }

    [Fact]
    public void FileAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<FileAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.File.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.File.ToActionString(action));
        }
    }

    [Fact]
    public void CommentAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<CommentAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Comment.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Comment.ToActionString(action));
        }
    }

    [Fact]
    public void BackgroundAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<BackgroundAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Background.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Background.ToActionString(action));
        }
    }

    [Fact]
    public void HeaderfooterAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<HeaderfooterAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Headerfooter.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Headerfooter.ToActionString(action));
        }
    }

    [Fact]
    public void ShapealignAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<ShapealignAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Shapealign.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Shapealign.ToActionString(action));
        }
    }

    [Fact]
    public void PagesetupAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<PagesetupAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Pagesetup.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Pagesetup.ToActionString(action));
        }
    }

    [Fact]
    public void TagAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<TagAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Tag.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Tag.ToActionString(action));
        }
    }

    [Fact]
    public void IPrintOptionsCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IPrintOptionsCommands));
        var enumValueCount = Enum.GetValues<PrintoptionsAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IPrintOptionsCommands has {coreMethodCount} [ServiceAction] methods but PrintoptionsAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void IAccessibilityCommands_AllMethodsHaveEnumValues()
    {
        var coreMethodCount = GetServiceActionMethodCount(typeof(IAccessibilityCommands));
        var enumValueCount = Enum.GetValues<AccessibilityAction>().Length;
        Assert.True(enumValueCount >= coreMethodCount,
            $"IAccessibilityCommands has {coreMethodCount} [ServiceAction] methods but AccessibilityAction has only {enumValueCount} enum values.");
    }

    [Fact]
    public void PrintoptionsAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<PrintoptionsAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Printoptions.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Printoptions.ToActionString(action));
        }
    }

    [Fact]
    public void AccessibilityAction_AllEnumValuesHaveMappings()
    {
        foreach (var action in Enum.GetValues<AccessibilityAction>())
        {
            var exception = Record.Exception(() => ServiceRegistry.Accessibility.ToActionString(action));
            Assert.Null(exception);
            Assert.NotEmpty(ServiceRegistry.Accessibility.ToActionString(action));
        }
    }

    /// <summary>
    /// Helper: Counts methods with [ServiceAction] attribute in an interface.
    /// </summary>
    private static int GetServiceActionMethodCount(Type interfaceType)
    {
        return interfaceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes()
                .Any(a => a.GetType().Name == "ServiceActionAttribute"))
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal)
            .Count();
    }
}

