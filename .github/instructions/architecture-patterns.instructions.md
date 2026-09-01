---
applyTo: "src/**/*.cs"
---

# Architecture Patterns

> **Core patterns for VisioMcp development**

## .NET Class Design (MANDATORY)

**Official Docs:** [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/), [Partial Classes](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods)

### Key Rules

1. **One Public Class Per File** - Standard .NET practice (System.Text.Json, ASP.NET Core, EF Core)
2. **File Name = Class Name** - `ShapeCommands.cs` contains `ShapeCommands`
3. **Partial Classes for Large Implementations** - Split 15+ method classes by feature domain
4. **Descriptive Names** - No over-optimization (`ShapeCommands` ✅, `Commands` ❌)
5. **Folder = Organization, Not Identity** - `Commands/Shape/ShapeCommands.cs`

### Partial Class Pattern

**When:** Class has 15+ methods, multiple feature domains, team collaboration

**Structure:**
```
Commands/Shape/
    IShapeCommands.cs           # Interface
    ShapeCommands.cs            # Implementation
    ShapeHelpers.cs             # Separate helper class when needed
```

**Benefits:** Git-friendly, team-friendly, ~100-200 lines per file, mirrors .NET Framework patterns

---

## TWO EQUAL ENTRY POINTS (CRITICAL)

**VisioMcp has TWO first-class entry points: MCP Server AND CLI.** Both must have:
- **Feature parity**: Every action in MCP must exist in CLI and vice versa
- **Parameter parity**: Same parameters, same defaults, same validation
- **Behavior parity**: Same Core command, same result format

When adding or changing ANY feature, ALWAYS update BOTH entry points. See Rule 24 (Post-Change Sync).

```
MCP Server (MCP tools, JSON-RPC) ──► In-process VisioMcpService ──► Core Commands ──► Visio COM
CLI (command-line args, console)  ──► CLI Daemon (named pipe) ─────► Core Commands ──► Visio COM
```

---

## Command Pattern

### Structure
```
Commands/
├── IPageCommands.cs     # Interface
├── PageCommands.cs      # Implementation
```

### Routing (Program.cs)
```csharp
return args[0] switch
{
    "page-list" => page.List(args),
    "shape-read" => shape.Read(args),
    _ => ShowHelp()
};
```

---

## Resource Management Pattern

**See visio-com-interop.instructions.md** for complete WithVisio() pattern and COM object lifecycle management.

---

## Exception Propagation Pattern (CRITICAL)

**Core Commands: Let exceptions propagate naturally** - Do NOT suppress with catch blocks that return error results.

```csharp
// ❌ WRONG: Suppressing exception with catch block
public async Task<OperationResult> SomeAsync(IVisioBatch batch, string param)
{
    try
    {
        return await batch.Execute((ctx, ct) => {
            // ... operation ...
            return ValueTask.FromResult(new OperationResult { Success = true });
        });
    }
    catch (Exception ex)
    {
        // ❌ WRONG: Catches exception and returns error result
        return new OperationResult 
        { 
            Success = false, 
            ErrorMessage = ex.Message 
        };
    }
}

// ✅ CORRECT: Let exception propagate through batch.Execute()
public async Task<OperationResult> SomeAsync(IVisioBatch batch, string param)
{
    return await batch.Execute((ctx, ct) => {
        // ... operation ...
        return ValueTask.FromResult(new OperationResult { Success = true });
    });
    // Exception automatically caught by batch.Execute() via TaskCompletionSource
    // Returns OperationResult { Success = false, ErrorMessage } from batch layer
}

// ✅ CORRECT: Finally blocks still allowed for COM resource cleanup
public async Task<OperationResult> ComplexAsync(IVisioBatch batch, string param)
{
    dynamic? shapeRef = null;
    try
    {
        return await batch.Execute((ctx, ct) => {
            var page = ((dynamic)ctx.Document).Pages.Item(1);
            shapeRef = page.DrawRectangle(1.0, 1.0, 3.0, 2.0);
            // ... operation ...
            return ValueTask.FromResult(new OperationResult { Success = true });
        });
    }
    finally
    {
        if (shapeRef != null)
        {
            ComUtilities.Release(ref shapeRef!);  // ✅ Cleanup in finally
        }
    }
}
```

**Why This Pattern:**
- `batch.Execute()` already captures exceptions via `TaskCompletionSource`
- Exceptions in lambda automatically become `OperationResult { Success = false }`
- Double-wrapping (try-catch returning error result) loses stack context and originates from wrong layer
- Finally blocks are the correct place for resource cleanup, NOT catch blocks for error suppression

**See:** CRITICAL-RULES.md Rule 1 for Success flag requirements

---

## MCP Server Resource-Based Tools

**In-Process Architecture**: MCP Server hosts VisioMcpService fully in-process with direct method calls (no pipe).
ServiceBridge holds the service reference and calls ProcessAsync() directly.

**11 Focused Tools (101 generated actions):**
1. `file` - Document lifecycle (create, open, save, close, list)
2. `cell` - ShapeSheet cell read/write and formulas
3. `docproperty` - Document custom properties
4. `export` - Export documents and pages
5. `layer` - Layer lifecycle and shape assignment
6. `page` - Page lifecycle, guides, and layout/routing
7. `shape` - Shape drawing, stencil drops, modification, deletion, and connectivity
8. `shapealign` - Shape alignment and distribution
9. `stencil` - Stencil and master discovery
10. `text` - Shape text read/write
11. `window` - Visio window operations

### Action-Based Routing with ForwardToService
```csharp
[McpServerTool]
public static string VisioPage(string action, string sessionId, ...)
{
    return action.ToLowerInvariant() switch
    {
        "list" => ForwardList(sessionId),
        "read" => ForwardRead(sessionId, pageIndex),
        _ => throw new McpException($"Unknown action: {action}")
    };
}

private static string ForwardList(string sessionId)
{
    return VisioToolsBase.ForwardToService("page.list", sessionId);
}
```

---

## DRY Shared Utilities

**Shared helper methods:** `GetPage()`, `ReadShapeInfo()`, ShapeSheet cell readers, and path/session validators

**Why:** Prevents 60+ lines of duplicate code per feature

---

## Security-First Patterns

```csharp
// Defaults
SavePassword = false  // Never export credentials by default
```

---

## Performance Patterns

**Minimize document opens** - Use single session for multiple operations
**Bulk operations** - Minimize COM round-trips by batching page, shape, and cell operations

---

## Key Principles

1. **VisioSession / IVisioBatch for everything** - See visio-com-interop.instructions.md
2. **Release intermediate objects** - Prevents Visio hanging
3. **Batch/Session for MCP** - Multiple operations in single session
4. **Resource-based tools** - 11 tools, 101 generated actions
5. **DRY utilities** - Share common patterns
6. **Security defaults** - Never expose credentials
7. **Bulk operations** - Minimize COM round-trips
