using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Validation;

/// <summary>
/// Read Visio validation rule sets and run diagram validation to report issues.
/// </summary>
[ServiceCategory("validation")]
[McpTool("validation", Title = "Validation Operations", Destructive = false, Category = "quality",
    Description = "Inspect Visio diagram validation. Validation is an edition-dependent Visio feature; if the installed edition blocks it the result explains the edition/licence limitation. "
    + "Use list-rule-sets to see available rule sets and validate to run validation and return issues. "
    + "flags for validate: 0=Visio default and may open the Issues window, 1=validate without opening the Issues window.")]
public interface IValidationCommands
{
    /// <summary>List validation rule sets in the current document.</summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list-rule-sets")]
    ValidationRuleSetListResult ListRuleSets(IVisioBatch batch);

    /// <summary>Run diagram validation and return the validation issues.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="ruleSetNameU">Optional universal name of a single rule set to run; omit for all active rule sets</param>
    /// <param name="flags">0=Visio default and may open the Issues window, 1=validate without opening the Issues window</param>
    [ServiceAction("validate")]
    ValidationResult Validate(IVisioBatch batch, string? ruleSetNameU = null, int flags = 1);
}
