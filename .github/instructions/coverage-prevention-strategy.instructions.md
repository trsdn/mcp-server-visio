---
applyTo: "src/VisioMcp.Core/Commands/**/*.cs,src/VisioMcp.McpServer/**/*.cs"
---

# Core Commands Coverage - Mandatory Workflow

> **⚠️ CRITICAL**: When adding Core Commands methods, you MUST expose them in MCP Server

## Quick Reference

| Task | Command | Time |
|------|---------|------|
| Check coverage before commit | `.\scripts\audit-core-coverage.ps1` | 30s |
| Add new Core method | Follow 8-step workflow below | 5-10 min |
| Fix pre-commit hook failure | Add missing enum values + mappings | 2-3 min |
| Verify build | `dotnet build -c Release` | 1-2 min |

---

## Mandatory Workflow: Adding New Core Method

**ALWAYS follow these 8 steps in order:**

```markdown
1. ✅ Add method to Core Commands interface
   File: src/VisioMcp.Core/Commands/[Feature]/I[Feature]Commands.cs
   Example: Task<OperationResult> NewMethodAsync(IPptBatch batch);

2. ✅ Implement in Core Commands class  
   File: src/VisioMcp.Core/Commands/[Feature]/[Feature]Commands.cs

3. ✅ Add enum value to ToolActions.cs
   File: src/VisioMcp.McpServer/Models/ToolActions.cs
   Example: SlideAction.NewMethod
   ⚠️ Build will show CS8524 error until steps 4-6 complete

4. ✅ Add ToActionString mapping
   File: src/VisioMcp.McpServer/Models/ActionExtensions.cs
   Example: SlideAction.NewMethod => "new-method",
   ⚠️ CS8524 error persists

5. ✅ Add switch case in MCP Tool
   File: src/VisioMcp.McpServer/Tools/Ppt[Feature]Tool.cs
   Example: SlideAction.NewMethod => await NewMethodAsync(...),
   ⚠️ CS8524 error persists

6. ✅ Implement MCP method
   File: src/VisioMcp.McpServer/Tools/Ppt[Feature]Tool.cs
   Example: private static async Task<string> NewMethodAsync(...)
   ✅ CS8524 errors resolved

7. ✅ Build and verify
   Command: dotnet build -c Release
   Expected: 0 warnings, 0 errors

8. ✅ Update documentation
   Files: skill references (`skills/shared/`), tool descriptions, README (if needed)
```

**Why This Order**: Compiler (CS8524) enforces steps 3-6, preventing you from shipping unexposed Core methods.

---

## Compiler Enforcement (CS8524)

**The compiler FORCES you to expose Core methods** through enum-based switches:

```csharp
// Step 3: Add enum value (compiler checks this)
public enum SlideAction
{
    List,
    Get,
    NewMethod  // ⚠️ Forget this → CS8524 error in ActionExtensions.cs
}

// Step 4: Add ToActionString mapping (compiler checks this)
public static string ToActionString(this SlideAction action) => action switch
{
    SlideAction.List => "list",
    SlideAction.Get => "get",
    SlideAction.NewMethod => "new-method",  // ⚠️ Forget this → CS8524 error
};

// Step 5: Add switch case in Tool (compiler checks this)
return action switch
{
    SlideAction.List => await ListAsync(...),
    SlideAction.Get => await GetAsync(...),
    SlideAction.NewMethod => await NewMethodAsync(...),  // ⚠️ Forget this → CS8524 error
};
```

**Result**: **Impossible to compile** until all 3 enum mappings are added!

---

## Pre-Commit Hook (Automatic Check)

**Before every commit**, the pre-commit hook runs `audit-core-coverage.ps1` to verify Core methods match enum values.

**Setup** (one-time):
```powershell
.\scripts\pre-commit.ps1
```

**On failure, you see**:
```
❌ Coverage gaps detected! All Core methods must be exposed via MCP Server.

The following interfaces have fewer enum values than Core methods:
  - IRangeCommands: Core has 42 methods, RangeAction has 40 values (missing 2)

Action Required:
  1. Review Core interface for new methods
  2. Add missing enum values to ToolActions.cs
  3. Add ToActionString mappings to ActionExtensions.cs
  4. Add switch cases to appropriate MCP Tools
```

**Fix**: Follow 8-step workflow above.

**Emergency bypass** (use only for non-Core changes):
```bash
git commit --no-verify -m "Message"
```

⚠️ **Never use `--no-verify`** for Core Commands changes - fix the gaps instead!

---

## Manual Coverage Check

**Requires a prior build** — the audit reads generated output (`ServiceRegistry.*.Dispatch.g.cs`,
`McpTool.*.g.cs`), so run `dotnet build VisioMcp.sln` first. If the generated files are missing,
the script fails rather than reporting coverage over an empty set.

```powershell
# Check coverage (fails on gaps by default)
.\scripts\audit-core-coverage.ps1

# Report gaps without failing
.\scripts\audit-core-coverage.ps1 -FailOnGaps:$false

# Show the per-category breakdown
.\scripts\audit-core-coverage.ps1 -ShowDetail
```

**Expected output when there are no gaps**:
```
Core Commands Coverage Audit
============================

Summary
-------
  Categories discovered : 37 (13 public, 24 suppressed)
  Interface methods     : 281
  Dispatch files        : 37
  Generated MCP tools   : 12
  Hand-written MCP tools: 1 (file)

No gaps detected across 37 categories and 281 methods.
```

**When gaps are detected**:
```
3 gap(s) detected:

  - [layer] method 'SetOpacity' has no dispatch case in ServiceRegistry.Layer.Dispatch.g.cs
  - [connector] is PublicSurface but has neither a generated McpTool.Connector.g.cs nor a
    hand-written [McpServerTool(Name = "connector")] - the tool is invisible to MCP clients
  - [vba] is PublicSurface = false but an MCP tool exists - a suppressed domain leaked onto
    the public surface
```

**When the tree has not been built**:
```
FAIL: discovery returned nothing.

  - Discovered 0 ServiceRegistry.*.Dispatch.g.cs files under ...\src\VisioMcp.Core\obj -
    run 'dotnet build VisioMcp.sln' first, this audit reads generated output.

Refusing to report coverage on an empty dataset.
```

**Fix**: Follow the 8-step workflow.

---

## Troubleshooting

### CS8524 Error: "Switch expression does not handle all possible values"

**Cause**: Added enum value but forgot to add it to switch expression.

**Fix**: Add the missing case to the switch expression in the file mentioned in error.

### Pre-Commit Hook Fails with "Coverage gaps detected"

**Cause**: Core interface has more methods than corresponding enum has values.

**Fix**: Follow 8-step workflow (steps 3-6).

### Build Succeeds but Pre-Commit Hook Still Fails

**Cause**: Added Core method but forgot to add enum value.

**Fix**: Add to ToolActions.cs, then mappings in ActionExtensions.cs, then Tool switch case.

---

## Key Takeaways

✅ **Compiler enforces coverage** - CS8524 prevents incomplete implementations  
✅ **Pre-commit hook verifies** - Catches gaps before commit  
✅ **8-step workflow is mandatory** - No shortcuts  
✅ **100% coverage is required** - No exceptions

