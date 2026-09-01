---
applyTo: "src/VisioMcp.Core/Commands/**/*.cs,src/VisioMcp.McpServer/**/*.cs"
---

# Core Commands Coverage - Mandatory Workflow

> **⚠️ CRITICAL**: When adding Core Commands methods, you MUST expose them in MCP Server

## Quick Reference

| Task | Command | Time |
|------|---------|------|
| Check coverage before commit | `dotnet test tests\VisioMcp.McpServer.Tests --filter "FullyQualifiedName~CoreCommandsCoverageTests"` | 30s |
| Add new Core method | Follow 8-step workflow below | 5-10 min |
| Fix pre-commit hook failure | Add missing enum values + mappings | 2-3 min |
| Verify build | `dotnet build -c Release` | 1-2 min |

---

## Mandatory Workflow: Adding New Core Method

**ALWAYS follow these 8 steps in order:**

```markdown
1. ✅ Add method to Core Commands interface
   File: src/VisioMcp.Core/Commands/[Feature]/I[Feature]Commands.cs
   Example: OperationResult NewMethod(IVisioBatch batch);

2. ✅ Implement in Core Commands class  
   File: src/VisioMcp.Core/Commands/[Feature]/[Feature]Commands.cs

3. ✅ Add enum value to ToolActions.cs
   File: src/VisioMcp.McpServer/Models/ToolActions.cs
   Example: PageAction.NewMethod
   ⚠️ Build will show CS8524 error until steps 4-6 complete

4. ✅ Add ToActionString mapping
   File: src/VisioMcp.McpServer/Models/ActionExtensions.cs
   Example: PageAction.NewMethod => "new-method",
   ⚠️ CS8524 error persists

5. ✅ Add switch case in MCP Tool
   File: src/VisioMcp.McpServer/Tools/Visio[Feature]Tool.cs
   Example: PageAction.NewMethod => await NewMethodAsync(...),
   ⚠️ CS8524 error persists

6. ✅ Implement MCP method
   File: src/VisioMcp.McpServer/Tools/Visio[Feature]Tool.cs
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
public enum PageAction
{
    List,
    Read,
    NewMethod  // ⚠️ Forget this → CS8524 error in ActionExtensions.cs
}

// Step 4: Add ToActionString mapping (compiler checks this)
public static string ToActionString(this PageAction action) => action switch
{
    PageAction.List => "list",
    PageAction.Read => "read",
    PageAction.NewMethod => "new-method",  // ⚠️ Forget this → CS8524 error
};

// Step 5: Add switch case in Tool (compiler checks this)
return action switch
{
    PageAction.List => await ListAsync(...),
    PageAction.Read => await ReadAsync(...),
    PageAction.NewMethod => await NewMethodAsync(...),  // ⚠️ Forget this → CS8524 error
};
```

**Result**: **Impossible to compile** until all 3 enum mappings are added!

---

## Pre-Commit Hook (Automatic Check)

**Before every commit**, the pre-commit hook runs the `CoreCommandsCoverageTests` suite to verify Core methods match enum values.

**Setup** (one-time):
```powershell
.\scripts\pre-commit.ps1
```

**On failure, you see**:
```
❌ Coverage gaps detected! All Core methods must be exposed via MCP Server.

The following interfaces have fewer enum values than Core methods:
  - IPageCommands: Core has 14 methods, PageAction has 12 values (missing 2)

Action Required:
  1. Review Core interface for new methods
  2. Add missing enum values to ToolActions.cs
  3. Add ToActionString mappings to ActionExtensions.cs
  4. Add switch cases to appropriate MCP Tools
```

**Fix**: Follow 8-step workflow above.

**Emergency bypass** (use only for non-Core changes):
```powershell
git commit --no-verify -m "Message"
```

⚠️ **Never use `--no-verify`** for Core Commands changes - fix the gaps instead!

---

## Manual Coverage Check

**Run anytime** to verify coverage:

```powershell
# Check coverage (shows gaps if any)
dotnet test tests\VisioMcp.McpServer.Tests --filter "FullyQualifiedName~CoreCommandsCoverageTests"

# Check coverage and fail if gaps found (useful in CI/CD)
dotnet test tests\VisioMcp.McpServer.Tests --filter "FullyQualifiedName~CoreCommandsCoverageTests"

# Verbose output with detailed counts
dotnet test tests\VisioMcp.McpServer.Tests --filter "FullyQualifiedName~CoreCommandsCoverageTests" -v n
```

**Expected output when 100% coverage**:
```
Interface           CoreMethods EnumValues Gap Status
---------           ----------- ---------- --- ------
IPageCommands                14         14   0 ✅
IShapeCommands               20         20   0 ✅
ICellCommands                 4          4   0 ✅
ITextCommands                 4          4   0 ✅

Summary: 100% coverage ✅ (101 Core methods, 101 enum values)
```

**When gaps detected**:
```
Interface           CoreMethods EnumValues Gap Status
---------           ----------- ---------- --- ------
IPageCommands                14         12   2 ❌

Summary: 98.7% coverage (65 Core methods, 63 enum values, 2 gaps)
```

**Fix**: Follow 8-step workflow.

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

