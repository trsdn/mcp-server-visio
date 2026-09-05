using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Validation;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for Visio diagram validation (#127).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Validation")]
public sealed class ValidationTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly ValidationCommands _validation = new();

    [Fact]
    public void Validate_WithNoRuleSets_ReturnsZeroIssues()
    {
        using var batch = CreateDocument();

        var result = _validation.Validate(batch, flags: 1);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(0, result.RuleSetCount);
        Assert.Equal(0, result.IssueCount);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ReturnsIssuesFromARealValidationRuleSet()
    {
        using var batch = CreateDocument();
        CreateFailingShapeRule(batch, "ProbeRuleSet", "AlwaysFailShapeProbe", "ProbeShape");

        var ruleSets = _validation.ListRuleSets(batch);
        var result = _validation.Validate(batch, "ProbeRuleSet", flags: 1);

        Assert.True(ruleSets.Success, ruleSets.ErrorMessage);
        Assert.Contains(ruleSets.RuleSets, ruleSet => ruleSet.NameU == "ProbeRuleSet" && ruleSet.RuleCount == 1);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, result.IssueCount);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("AlwaysFailShapeProbe", issue.RuleNameU);
        Assert.Equal("ProbeShape", issue.TargetShapeName);
        Assert.False(issue.Ignored);
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private static void CreateFailingShapeRule(IVisioBatch batch, string ruleSetNameU, string ruleNameU, string shapeName)
    {
        batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? validation = null;
            dynamic? ruleSets = null;
            dynamic? ruleSet = null;
            dynamic? rules = null;
            dynamic? rule = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shape = page.DrawRectangle(1, 1, 3, 2);
                shape.Name = shapeName;
                validation = ((dynamic)ctx.Document).Validation;
                ruleSets = validation.RuleSets;
                ruleSet = ruleSets.Add(ruleSetNameU);
                ruleSet.Description = "Integration validation probe";
                rules = ruleSet.Rules;
                rule = rules.Add(ruleNameU);
                rule.Category = "Probe";
                rule.Description = "Probe rule that intentionally fails for shapes";
                rule.TargetType = 0;
                rule.FilterExpression = "TRUE";
                rule.TestExpression = "FALSE";
                return 0;
            }
            finally
            {
                if (rule != null) ComUtilities.Release(ref rule!);
                if (rules != null) ComUtilities.Release(ref rules!);
                if (ruleSet != null) ComUtilities.Release(ref ruleSet!);
                if (ruleSets != null) ComUtilities.Release(ref ruleSets!);
                if (validation != null) ComUtilities.Release(ref validation!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }
}
