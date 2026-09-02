using System.Reflection;
using VisioMcp.Core.Attributes;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Rejects parameter names the CLI entry point cannot express.
///
/// The CLI generates a Spectre.Console long option per parameter, and Spectre refuses a
/// single-character long option: *"Long option names must consist of more than one character."*
/// It throws while building the command tree, so a single-letter parameter anywhere in Core stops
/// <b>every</b> CLI command from running — not just its own.
///
/// #32 hit this by adding <c>x</c> and <c>y</c> to the shape tool. The whole CLI died, and nothing
/// caught it until the pre-commit smoke test failed on `session create` with a parse error that
/// named neither the parameter nor the tool. This test names both.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "ActionValidation")]
public class ParameterNameConstraintTests
{
    public static TheoryData<string, string, string> PublicParameters()
    {
        var data = new TheoryData<string, string, string>();

        var assembly = typeof(McpToolAttribute).Assembly;

        var interfaces = assembly
            .GetTypes()
            .Where(t => t.IsInterface)
            .Select(t => new
            {
                Category = t.GetCustomAttribute<ServiceCategoryAttribute>(),
                Tool = t.GetCustomAttribute<McpToolAttribute>(),
                Type = t
            })
            .Where(x => x.Category is not null && x.Tool is not null && x.Tool!.PublicSurface);

        foreach (var iface in interfaces)
        {
            foreach (var method in iface.Type.GetMethods())
            {
                if (method.GetCustomAttribute<ServiceActionAttribute>() is null)
                {
                    continue;
                }

                foreach (var parameter in method.GetParameters())
                {
                    if (parameter.Name is null || parameter.Name == "batch")
                    {
                        continue;
                    }

                    data.Add(iface.Category!.Category, method.Name, parameter.Name);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublicParameters))]
    public void ParameterName_IsLongEnoughForACliOption(string category, string methodName, string parameterName)
    {
        Assert.True(
            parameterName.Length > 1,
            $"'{category}.{methodName}' declares parameter '{parameterName}'. The CLI turns each "
            + "parameter into a Spectre.Console long option, and a single-character long option "
            + "throws while the command tree is built — which takes down every CLI command, not "
            + "just this one. Use a descriptive name such as 'connectionPointX'.");
    }

    [Fact]
    public void Discovery_FindsPublicParametersToCheck()
    {
        // A gate that checks nothing passes vacuously — the failure mode #15 shipped with.
        Assert.True(
            PublicParameters().Count >= 100,
            $"Expected the full public parameter surface, found {PublicParameters().Count}.");
    }
}
