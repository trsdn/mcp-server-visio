## Summary
Brief description of what this PR does.

## Type of Change
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📚 Documentation update
- [ ] 🔧 Maintenance (dependency updates, code cleanup, etc.)

## Related Issues
Closes #[issue number]
Relates to #[issue number]

## Changes Made
- Change 1
- Change 2
- Change 3

## Testing Performed
- [ ] Tested manually with various Visio files
- [ ] Verified Visio process cleanup (no visio.exe remains after 5 seconds)
- [ ] Tested error conditions (missing files, invalid arguments, etc.)
- [ ] All existing commands still work
- [ ] If Visio CI runner was inactive, ran the required Visio integration tests locally
- [ ] If LLM-facing help/docs changed, ran `scripts\Test-LlmRegressionGate.ps1` or explained why not
- [ ] VBA script execution tested (if applicable)
- [ ] PPTM file format validation tested (if applicable)
- [ ] VBA trust setup tested (if applicable)
- [ ] Build produces zero warnings

## Test Commands
```powershell
# Commands used for testing
VisioMcp command1 "test.vsdx"
VisioMcp command2 "test.vsdx" "param"
```

## Screenshots (if applicable)
[Add screenshots showing the new functionality]

## Core Commands Coverage Checklist ⚠️

**Does this PR add or modify Core Commands methods?** [ ] Yes [ ] No

If YES, verify all steps completed:

- [ ] Added method to Core Commands interface (e.g., `IPowerQueryCommands.NewMethodAsync()`)
- [ ] Implemented method in Core Commands class (e.g., `PowerQueryCommands.NewMethodAsync()`)
- [ ] Added enum value to `ToolActions.cs` (e.g., `PowerQueryAction.NewMethod`)
- [ ] Added `ToActionString` mapping to `ActionExtensions.cs` (e.g., `PowerQueryAction.NewMethod => "new-method"`)
- [ ] Added switch case to appropriate MCP Tool (e.g., `PptPowerQueryTool.cs`)
- [ ] Implemented MCP method that calls Core method
- [ ] Build succeeds with 0 warnings (CS8524 compiler enforcement verified)
- [ ] Updated `CORE-COMMANDS-AUDIT.md` (if significant addition)
- [ ] Added integration tests for new action
- [ ] Updated MCP Server prompts documentation
- [ ] Updated CLI commands documentation (if applicable)

**Coverage Impact**: +___ methods, ___% → ___% coverage

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review of code completed
- [ ] Code builds with zero warnings
- [ ] Appropriate error handling added
- [ ] Updated help text (if adding new commands)
- [ ] Updated README.md (if needed)
- [ ] Follows Visio COM best practices from copilot-instructions.md
- [ ] Uses batch API with proper disposal (`using var batch` or `await using var batch`)
- [ ] Properly handles 1-based Visio indexing
- [ ] Escapes user input with `.EscapeMarkup()`
- [ ] Returns consistent exit codes (0 = success, 1+ = error)

## Additional Notes
Any additional information that reviewers should know.
