using System.Globalization;
using System.Runtime.InteropServices;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Validation;

public class ValidationCommands : IValidationCommands
{
    public ValidationRuleSetListResult ListRuleSets(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? validation = null;
            dynamic? ruleSets = null;
            try
            {
                validation = GetValidation(ctx.Document);
                ruleSets = validation.RuleSets;

                var result = new ValidationRuleSetListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath
                };

                for (var i = 1; i <= Convert.ToInt32(ruleSets.Count, CultureInfo.InvariantCulture); i++)
                {
                    dynamic? ruleSet = null;
                    try
                    {
                        ruleSet = ruleSets.Item(i);
                        result.RuleSets.Add(ReadRuleSet(ruleSet));
                    }
                    finally
                    {
                        if (ruleSet != null) ComUtilities.Release(ref ruleSet!);
                    }
                }

                return result;
            }
            finally
            {
                if (ruleSets != null) ComUtilities.Release(ref ruleSets!);
                if (validation != null) ComUtilities.Release(ref validation!);
            }
        });
    }

    public ValidationResult Validate(IVisioBatch batch, string? ruleSetNameU = null, int flags = 1)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? validation = null;
            dynamic? ruleSets = null;
            dynamic? ruleSet = null;
            dynamic? issues = null;
            try
            {
                validation = GetValidation(ctx.Document);
                ruleSets = validation.RuleSets;

                try
                {
                    if (string.IsNullOrWhiteSpace(ruleSetNameU))
                    {
                        for (var i = 1; i <= Convert.ToInt32(ruleSets.Count, CultureInfo.InvariantCulture); i++)
                        {
                            dynamic? currentRuleSet = null;
                            try
                            {
                                currentRuleSet = ruleSets.Item(i);
                                if (Convert.ToBoolean(currentRuleSet.Enabled, CultureInfo.InvariantCulture))
                                {
                                    validation.Validate(currentRuleSet, flags);
                                }
                            }
                            finally
                            {
                                if (currentRuleSet != null) ComUtilities.Release(ref currentRuleSet!);
                            }
                        }
                    }
                    else
                    {
                        ruleSet = ruleSets.Item(ruleSetNameU);
                        validation.Validate(ruleSet, flags);
                    }
                }
                catch (COMException ex)
                {
                    throw CreateValidationUnavailableException("run validation", ex);
                }

                issues = validation.Issues;

                return new ValidationResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    RuleSetCount = Convert.ToInt32(ruleSets.Count, CultureInfo.InvariantCulture),
                    IssueCount = Convert.ToInt32(issues.Count, CultureInfo.InvariantCulture),
                    Issues = ReadIssues(issues)
                };
            }
            finally
            {
                if (issues != null) ComUtilities.Release(ref issues!);
                if (ruleSet != null) ComUtilities.Release(ref ruleSet!);
                if (ruleSets != null) ComUtilities.Release(ref ruleSets!);
                if (validation != null) ComUtilities.Release(ref validation!);
            }
        });
    }

    private static dynamic GetValidation(object document)
    {
        try
        {
            return ((dynamic)document).Validation;
        }
        catch (COMException ex)
        {
            throw CreateValidationUnavailableException("access document validation", ex);
        }
    }

    private static ValidationRuleSetInfo ReadRuleSet(dynamic ruleSet)
    {
        dynamic? rules = null;
        try
        {
            rules = ruleSet.Rules;
            return new ValidationRuleSetInfo
            {
                Id = Convert.ToInt32(ruleSet.ID, CultureInfo.InvariantCulture),
                Name = ruleSet.Name?.ToString() ?? string.Empty,
                NameU = ruleSet.NameU?.ToString() ?? string.Empty,
                Description = ruleSet.Description?.ToString() ?? string.Empty,
                Enabled = Convert.ToBoolean(ruleSet.Enabled, CultureInfo.InvariantCulture),
                RuleCount = Convert.ToInt32(rules.Count, CultureInfo.InvariantCulture)
            };
        }
        finally
        {
            if (rules != null) ComUtilities.Release(ref rules!);
        }
    }

    private static List<ValidationIssueInfo> ReadIssues(dynamic issues)
    {
        var result = new List<ValidationIssueInfo>();
        for (var i = 1; i <= Convert.ToInt32(issues.Count, CultureInfo.InvariantCulture); i++)
        {
            dynamic? issue = null;
            try
            {
                issue = issues.Item(i);
                result.Add(ReadIssue(issue));
            }
            finally
            {
                if (issue != null) ComUtilities.Release(ref issue!);
            }
        }

        return result;
    }

    private static ValidationIssueInfo ReadIssue(dynamic issue)
    {
        dynamic? targetPage = null;
        dynamic? targetShape = null;
        dynamic? rule = null;
        try
        {
            targetPage = TryGetTargetPage(issue);
            targetShape = TryGetTargetShape(issue);
            rule = TryGetRule(issue);

            return new ValidationIssueInfo
            {
                Id = Convert.ToInt32(issue.ID, CultureInfo.InvariantCulture),
                Ignored = Convert.ToBoolean(issue.Ignored, CultureInfo.InvariantCulture),
                TargetPageId = Convert.ToInt32(issue.TargetPageID, CultureInfo.InvariantCulture),
                TargetPageName = targetPage?.Name?.ToString(),
                TargetShapeId = targetShape is null ? null : Convert.ToInt32(targetShape.ID, CultureInfo.InvariantCulture),
                TargetShapeName = targetShape?.Name?.ToString(),
                RuleId = rule is null ? null : Convert.ToInt32(rule.ID, CultureInfo.InvariantCulture),
                RuleNameU = rule?.NameU?.ToString(),
                RuleDescription = rule?.Description?.ToString()
            };
        }
        finally
        {
            if (rule != null) ComUtilities.Release(ref rule!);
            if (targetShape != null) ComUtilities.Release(ref targetShape!);
            if (targetPage != null) ComUtilities.Release(ref targetPage!);
        }
    }

    private static dynamic? TryGetTargetPage(dynamic issue)
    {
        try
        {
            return issue.TargetPage;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static dynamic? TryGetTargetShape(dynamic issue)
    {
        try
        {
            return issue.TargetShape;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static dynamic? TryGetRule(dynamic issue)
    {
        try
        {
            return issue.Rule;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static InvalidOperationException CreateValidationUnavailableException(string operation, COMException ex)
    {
        return new InvalidOperationException(
            "Visio validation is unavailable or blocked by this Visio edition/licence while trying to "
            + $"{operation}. Diagram validation requires a Visio edition that includes validation features. "
            + $"Original COM error 0x{ex.HResult:X8}: {ex.Message}",
            ex);
    }
}
