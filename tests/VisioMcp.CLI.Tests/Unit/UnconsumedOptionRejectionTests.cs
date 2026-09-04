using System.Reflection;
using VisioMcp.Generated;
using Xunit;

namespace VisioMcp.CLI.Tests.Unit;

/// <summary>
/// Regression tests for #103: the CLI accepted options the selected action does not consume,
/// reported success, and silently discarded them.
///
/// These are COM-free (Rule 30): they exercise the generated routing layer only, and would still
/// be meaningful with Visio uninstalled.
/// </summary>
[Trait("Layer", "CLI")]
[Trait("Category", "Unit")]
[Trait("Feature", "ActionValidation")]
[Trait("Speed", "Fast")]
public sealed class UnconsumedOptionRejectionTests
{
    /// <summary>
    /// The exact reproduction from #103: <c>shape add-shape --text "Start"</c> produced
    /// <c>success: true</c> and an unlabelled shape, because AddShape has no text parameter.
    /// </summary>
    [Fact]
    public void AddShape_WithTextOption_IsRejected()
    {
        var settings = new ServiceRegistry.Shape.CliSettings
        {
            Action = "add-shape",
            SessionId = "s1",
            PageIndex = 1,
            AutoShapeType = 1,
            Left = 100,
            Top = 100,
            Width = 120,
            Height = 60,
            Text = "Start"
        };

        var ex = Assert.Throws<ArgumentException>(
            () => ServiceRegistry.Shape.RouteFromSettings("add-shape", settings));

        Assert.Contains("--text", ex.Message, StringComparison.Ordinal);
        Assert.Contains("add-shape", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// An agent that gets rejected needs to know where the option *is* valid, otherwise the
    /// error is just a different kind of dead end.
    /// </summary>
    [Fact]
    public void RejectionMessage_NamesAnActionThatDoesAcceptTheOption()
    {
        var settings = new ServiceRegistry.Shape.CliSettings
        {
            Action = "add-shape",
            SessionId = "s1",
            PageIndex = 1,
            Text = "Start"
        };

        var ex = Assert.Throws<ArgumentException>(
            () => ServiceRegistry.Shape.RouteFromSettings("add-shape", settings));

        Assert.Contains("add-textbox", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same options on the action that does consume them must still route cleanly.
    /// </summary>
    [Fact]
    public void AddTextbox_WithTextOption_IsAccepted()
    {
        var settings = new ServiceRegistry.Shape.CliSettings
        {
            Action = "add-textbox",
            SessionId = "s1",
            PageIndex = 1,
            Left = 100,
            Top = 100,
            Width = 120,
            Height = 60,
            Text = "Start"
        };

        var (command, args) = ServiceRegistry.Shape.RouteFromSettings("add-textbox", settings);

        Assert.Equal("shape.add-textbox", command);
        Assert.NotNull(args);
    }

    /// <summary>
    /// add-shape's own options must not be rejected by the validator.
    /// </summary>
    [Fact]
    public void AddShape_WithOnlyItsOwnOptions_IsAccepted()
    {
        var settings = new ServiceRegistry.Shape.CliSettings
        {
            Action = "add-shape",
            SessionId = "s1",
            PageIndex = 1,
            AutoShapeType = 1,
            Left = 100,
            Top = 100,
            Width = 120,
            Height = 60
        };

        var (command, args) = ServiceRegistry.Shape.RouteFromSettings("add-shape", settings);

        Assert.Equal("shape.add-shape", command);
        Assert.NotNull(args);
    }

    /// <summary>
    /// --output and --session are universal CLI plumbing, not action parameters, so they must
    /// never be rejected regardless of the action.
    /// </summary>
    [Fact]
    public void UniversalOptions_AreNeverRejected()
    {
        var settings = new ServiceRegistry.Shape.CliSettings
        {
            Action = "list",
            SessionId = "s1",
            PageIndex = 1,
            OutputPath = "out.json"
        };

        var (command, _) = ServiceRegistry.Shape.RouteFromSettings("list", settings);

        Assert.Equal("shape.list", command);
    }

    /// <summary>
    /// Guards against an allow-list that is accidentally generated empty: routing every action of
    /// every public category with no options supplied must never trip the unconsumed-option check.
    /// A failure here means the validator rejects options nobody supplied.
    /// </summary>
    [Fact]
    public void NoOptionsSupplied_NeverTripsTheValidator()
    {
        var failures = new List<string>();

        foreach (var (categoryType, routeFromSettings, settingsType, validActions) in EnumerateCategories())
        {
            foreach (var action in validActions)
            {
                var settings = Activator.CreateInstance(settingsType)!;

                try
                {
                    routeFromSettings.Invoke(null, [action, settings]);
                }
                catch (TargetInvocationException tie) when (tie.InnerException is ArgumentException inner)
                {
                    // Missing-required-parameter errors are expected here and are a different
                    // check. Only unconsumed-option rejections are a bug.
                    if (inner.Message.Contains("does not accept", StringComparison.Ordinal))
                    {
                        failures.Add($"{categoryType.Name}.{action}: {inner.Message}");
                    }
                }
            }
        }

        Assert.Empty(failures);
    }

    /// <summary>
    /// Every public category must have the rejection wired in, not just Shape — otherwise the
    /// silent-discard bug simply moves to whichever domain the generator missed.
    /// </summary>
    [Fact]
    public void EveryCategory_RejectsAnOptionItsActionDoesNotConsume()
    {
        var categoriesWithMultipleActions = 0;
        var categoriesThatRejected = 0;

        foreach (var (_, routeFromSettings, settingsType, validActions) in EnumerateCategories())
        {
            var optionProperties = settingsType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name is not ("Action" or "SessionId" or "OutputPath"))
                .ToList();

            if (validActions.Count < 2 || optionProperties.Count == 0)
            {
                continue;
            }

            categoriesWithMultipleActions++;

            var rejectedSomething = validActions.Any(action =>
                optionProperties.Any(property => RejectsOption(routeFromSettings, settingsType, action, property)));

            if (rejectedSomething)
            {
                categoriesThatRejected++;
            }
        }

        Assert.True(categoriesWithMultipleActions > 0, "No multi-action categories were discovered.");
        Assert.Equal(categoriesWithMultipleActions, categoriesThatRejected);
    }

    private static bool RejectsOption(
        MethodInfo routeFromSettings,
        Type settingsType,
        string action,
        PropertyInfo property)
    {
        var value = SampleValueFor(property.PropertyType);
        if (value is null)
        {
            return false;
        }

        var settings = Activator.CreateInstance(settingsType)!;
        var backing = settingsType.GetProperty(property.Name);
        if (backing is null || !backing.CanWrite)
        {
            return false;
        }

        backing.SetValue(settings, value);

        try
        {
            routeFromSettings.Invoke(null, [action, settings]);
            return false;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ArgumentException inner)
        {
            return inner.Message.Contains("does not accept", StringComparison.Ordinal);
        }
    }

    private static object? SampleValueFor(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return "x";
        if (underlying == typeof(bool)) return true;
        if (underlying.IsEnum) return Enum.GetValues(underlying).GetValue(0);
        if (underlying == typeof(int)) return 1;
        if (underlying == typeof(long)) return 1L;
        if (underlying == typeof(float)) return 1f;
        if (underlying == typeof(double)) return 1d;

        return null;
    }

    private static IEnumerable<(Type CategoryType, MethodInfo RouteFromSettings, Type SettingsType, IReadOnlyList<string> ValidActions)> EnumerateCategories()
    {
        foreach (var categoryType in typeof(ServiceRegistry).GetNestedTypes(BindingFlags.Public))
        {
            var settingsType = categoryType.GetNestedType("CliSettings", BindingFlags.Public);
            var routeFromSettings = categoryType.GetMethod(
                "RouteFromSettings",
                BindingFlags.Public | BindingFlags.Static);
            var validActionsField = categoryType.GetField(
                "ValidActions",
                BindingFlags.Public | BindingFlags.Static);

            if (settingsType is null || routeFromSettings is null || validActionsField is null)
            {
                continue;
            }

            var validActions = (string[])validActionsField.GetValue(null)!;
            yield return (categoryType, routeFromSettings, settingsType, validActions);
        }
    }
}
