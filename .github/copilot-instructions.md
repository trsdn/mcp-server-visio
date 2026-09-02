# GitHub Copilot Instructions - VisioMcp

> **🎯 Optimized for AI Coding Agents** - Modular, path-specific instructions

## 📋 Critical Files (Read These First)

**ALWAYS read when working on code:**
- [CRITICAL-RULES.md](instructions/critical-rules.instructions.md) - 27 mandatory rules (Success flag, COM cleanup, tests, etc.)
- [Architecture Patterns](instructions/architecture-patterns.instructions.md) - Batch API, command pattern, resource management

**Read based on task type:**
- Adding/fixing commands → [Visio COM Interop](instructions/visio-com-interop.instructions.md)
- Writing tests → [Testing Strategy](instructions/testing-strategy.instructions.md)
- MCP Server work → [MCP Server Guide](instructions/mcp-server-guide.instructions.md)
- Creating PR → [Development Workflow](instructions/development-workflow.instructions.md)
- Fixing bugs → [Bug Fixing Checklist](instructions/bug-fixing-checklist.instructions.md)

**Less frequently needed:**
- [Visio ShapeSheet Patterns](instructions/visio-com-patterns-guide.instructions.md) - Only for connection-specific work
- [README Management](instructions/readme-management.instructions.md) - Only when updating READMEs
- [Documentation Structure](instructions/documentation-structure.instructions.md) - Only when creating docs

---

## What is VisioMcp?

**VisioMcp** is a Windows-only toolset for programmatic Visio automation via COM interop, designed for coding agents and automation scripts.

> **⚠️ CRITICAL: VisioMcp has TWO equal entry points — MCP Server AND CLI.**
> Both are first-class citizens. Every feature, action, and parameter must work identically through both.
> When adding/changing features, ALWAYS verify BOTH MCP Server tools AND CLI commands are updated.
> See Rule 24 (Post-Change Sync) for the full checklist.

**Core Layers:**
1. **ComInterop** (`src/VisioMcp.ComInterop`) - Reusable COM automation patterns (STA threading, session management, batch operations, OLE message filter)
2. **Core** (`src/VisioMcp.Core`) - Visio-specific business logic (slides, shapes, VBA, parameters)
3. **Service** (`src/VisioMcp.Service`) - Visio session management and command routing (in-process for MCP Server, named pipe for CLI daemon)
4. **CLI** (`src/VisioMcp.CLI`) - Command-line interface for scripting (EQUAL entry point)
5. **MCP Server** (`src/VisioMcp.McpServer`) - Model Context Protocol for AI assistants (EQUAL entry point)

**Source Generators** (`src/VisioMcp.Generators*`) - Generate CLI commands and MCP tools from Core interfaces

---

## 🎯 Quick Reference

### Test Commands
```powershell
# ⚠️ CRITICAL: Integration tests take 45+ MINUTES for full suite
# ALWAYS use surgical testing - test only what you changed!

# Fast feedback (excludes VBA) - Still takes 10-15 minutes
dotnet test --filter "Category=Integration&RunType!=OnDemand&Feature!=Export"

# Surgical testing - Feature-specific (2-5 minutes per feature)
dotnet test --filter "Feature=Page&RunType!=OnDemand"
dotnet test --filter "Feature=Shape&RunType!=OnDemand"
dotnet test --filter "Feature=Text&RunType!=OnDemand"

# Session/batch changes (MANDATORY)
dotnet test --filter "RunType=OnDemand"
```

### Code Patterns
```csharp
// Core: NEVER wrap batch.Execute() in try-catch that returns error result
// Let exceptions propagate naturally - batch.Execute() handles them via TaskCompletionSource
public DataType Method(IVisioBatch batch, string arg1)
{
    return batch.Execute((ctx, ct) => {
        dynamic? item = null;
        try {
            // Operation code here
            item = ctx.Document.SomeObject;
            // For CRUD: return void (throws on error)
            // For queries: return actual data
            return someData;
        }
        finally {
            // ✅ ONLY finally blocks for COM cleanup
            ComUtilities.Release(ref item!);
        }
        // ❌ NO catch blocks that return error results
    });
}


// CLI: Wrap Core calls
public int Method(string[] args)
{
    try {
        using var batch = VisioSession.BeginBatch(filePath);
        _coreCommands.Method(batch, arg1);
        return 0;
    } catch (Exception ex) {
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
        return 1;
    }
}

// Tests: Use batch API
[Fact]
public void TestMethod()
{
    using var batch = VisioSession.BeginBatch(_testFile);
    var result = _commands.Method(batch, args);
    Assert.NotNull(result); // Or other appropriate assertion
}
```

### Tool Selection
- Code changes → `replace_string_in_file` (3-5 lines context)
- Find code → `grep_search` or `semantic_search`
- Check errors → `get_errors`
- Build/test/git → `run_in_terminal`

---

## 🔄 Key Lessons (Update After Major Work)

**Success Flag:** NEVER `Success = true` with `ErrorMessage`. Set Success in try block, always false in catch.

**Batch API:** Create NEW simple tests. CLI needs try-catch wrapping.

**Visio Quirks:** Shape Z-order requires explicit reordering. Page indices are 1-based. Use `Pages.Item(index)` not zero-based access.

**MCP Design:** Prompts are shortcuts, not tutorials. LLMs know Visio and programming.

**Tool Priority:** `replace_string_in_file` > `grep_search` > `run_in_terminal`. Avoid PowerShell for code.

**Pre-Commit:** Search TODO/FIXME/HACK, delete commented code, verify tests, check docs.

**PR Review:** Check automated comments immediately (Copilot, GitHub Security). Fix before human review.

**Surgical Testing:** Integration tests take 45+ minutes. ALWAYS test only the feature you changed using `--filter "Feature=<name>"`.

**MCP Parameter Naming:** NEVER use underscores in C# Core interface parameter names. The `McpToolGenerator` calls `StringHelper.ToSnakeCase()` on the C# parameter name to produce the MCP snake_case parameter automatically. Use camelCase in C# that produces the desired snake_case output: `rangeAddress` → `range_address`, `sourceRangeAddress` → `source_range_address`. If the C# name can't produce the desired MCP name via ToSnakeCase, use `[FromString("desiredName")]` attribute instead of underscores in C# names.

---

## 📚 How Path-Specific Instructions Work

GitHub Copilot auto-loads instructions based on files you're editing:

- `tests/**/*.cs` → [Testing Strategy](instructions/testing-strategy.instructions.md)
- `src/VisioMcp.Core/**/*.cs` → [Visio COM Interop](instructions/visio-com-interop.instructions.md)
- `src/VisioMcp.McpServer/**/*.cs` → [MCP Server Guide](instructions/mcp-server-guide.instructions.md)
- `.github/workflows/**/*.yml` → [Development Workflow](instructions/development-workflow.instructions.md)
- `**` (all files) → [CRITICAL-RULES.md](instructions/critical-rules.instructions.md)

Modular approach = relevant context without overload.

---

## 🔒 Pre-Commit Hooks (7 Automated Checks)

Pre-commit runs `scripts/pre-commit.ps1` which blocks commits if any check fails:

| # | Check | Script | What It Validates |
|---|-------|--------|-------------------|
| 1 | Branch | (inline) | Never commit to `main` directly (Rule 6) |
| 2 | Process cleanup | (inline) | Kills stale `VISIO`, `visiocli` and server processes so the build can replace locked binaries |
| 3 | COM Leaks | `check-com-leaks.ps1` | All `dynamic` COM objects have `ComUtilities.Release()` in finally |
| 4 | Coverage Audit | `audit-core-coverage.ps1` | Every Core action reaches dispatch; every public domain reaches both MCP and CLI; no suppressed domain leaks |
| 5 | Success Flag | `check-success-flag.ps1` | Rule 0: Never `Success=true` with `ErrorMessage` |
| 6 | CLI Settings Usage | `check-cli-settings-usage.ps1` | Every Settings property on a hand-written CLI command is actually read |
| 7 | CLI Workflow Test | `Test-CliWorkflow.ps1` | E2E CLI round-trip against a real `.vsdx` |
| 8 | MCP Smoke Test | `dotnet test --filter "...SmokeTest..."` | All MCP tools functional |

> The table previously claimed **10** checks. Three of them (`check-cli-coverage.ps1`,
> `check-cli-action-coverage.ps1`, `check-cli-settings-usage.ps1`) were **never invoked** by the
> hook, and one that *was* invoked (`check-dynamic-casts.ps1`) was not listed. Corrected in #16;
> the first two were deleted, the third is now wired in, and the fourth was deleted because its
> premise — migrating to the PowerPoint PIA — does not apply to a Visio project.

**Install hook (one command, once per clone):**
```powershell
# From repo root
.\scripts\Install-GitHooks.ps1
```

This sets `core.hooksPath` to the committed `.githooks` directory, so the hook is
version-controlled and every clone that bootstraps once picks up later changes automatically.

CI runs the same gates on every PR via the `quality-gates` job in `build-cli.yml`, so the hook is
not the only line of defence.

---

## 🧪 LLM Integration Tests (`llm-tests/`)

Separate pytest-based project validating LLM behavior using `pytest-aitest`:

```powershell
# Setup
cd llm-tests
uv sync

# Run tests
uv run pytest -m mcp -v      # MCP Server tests
uv run pytest -m cli -v      # CLI tests
uv run pytest -m aitest -v   # All LLM tests
```

**Prerequisites:**
- Azure OpenAI endpoint: `$env:AZURE_OPENAI_ENDPOINT = "https://<resource>.openai.azure.com/"`
- Build MCP Server: `dotnet build src\VisioMcp.McpServer -c Release`

**Structure:**
- `test_mcp_*.py` - MCP Server workflows
- `test_cli_*.py` - CLI workflows
- `Fixtures/` - Shared test inputs (CSV/JSON/M files)

---

## 📦 Agent Skills (`skills/`)

Two cross-platform AI assistant skill packages:

| Skill | File | Target | Best For |
|-------|------|--------|----------|
| **visio-cli** | `skills/visio-cli/SKILL.md` | CLI Tool | Coding agents (token-efficient, `--help` discoverable) |
| **visio-mcp** | `skills/visio-mcp/SKILL.md` | MCP Server | Conversational AI (rich tool schemas) |

**Build skills from source:**
```powershell
dotnet build -c Release  # Generates SKILL.md, copies references, and generates MCP prompts
```

**Guidance architecture (single source of truth):**
- `skills/shared/*.md` → auto-copied to skill references AND auto-generated as MCP prompts
- Skill-based clients (VS Code, Cursor) read `skills/visio-*/references/`
- MCP-only clients (Claude Desktop) read auto-generated `[McpServerPrompt]` methods
- NEVER create separate prompt files for content that belongs in `skills/shared/`

**Install via npx:**
```bash
npx skills add trsdn/mcp-server-visio --skill visio-cli   # Coding agents
npx skills add trsdn/mcp-server-visio --skill visio-mcp   # Conversational AI
```

---

## 🏗️ Architecture Patterns

### Command File Structure
```
Commands/Slide/
├── ISlideCommands.cs           # Interface (defines contract)
├── SlideCommands.cs            # Partial class (constructor, DI)
├── SlideCommands.Lifecycle.cs  # Partial (Create, Delete, Rename...)
└── SlideCommands.Style.cs      # Partial (formatting operations)
```

**Rules:**
- One public class per file
- File name = class name
- Partial classes for 15+ methods (split by feature domain)

### Exception Propagation (CRITICAL)
```csharp
// ✅ CORRECT: Let batch.Execute() handle exceptions
return await batch.Execute((ctx, ct) => {
    var result = DoSomething();
    return ValueTask.FromResult(result);
});
// Exception auto-caught by TaskCompletionSource → OperationResult { Success = false }

// ❌ WRONG: Never suppress with catch returning error result
catch (Exception ex) { 
    return new OperationResult { Success = false, ErrorMessage = ex.Message }; 
}
```

### Service Architecture (TWO EQUAL ENTRY POINTS)

```
MCP Server ──► In-process VisioMcpService ──► Core Commands ──► Visio COM
CLI ─────────► CLI Daemon (named pipe) ─────► Core Commands ──► Visio COM
```

**⚠️ MCP Server and CLI are BOTH first-class entry points.** Each hosts its own VisioMcpService instance:
- **MCP Server**: Fully in-process, direct method calls (no pipe)
- **CLI**: Daemon process with named pipe (`VisioMcp-cli-{SID}`), sessions persist across CLI invocations
- **Feature parity**: Every action available in MCP must be available in CLI and vice versa
- **Parameter parity**: Same parameters, same defaults, same validation

