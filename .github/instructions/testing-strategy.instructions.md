---
applyTo: "tests/**/*.cs"
---

# Testing Strategy - Quick Reference

## Test Execution

**⚠️ CRITICAL: Always specify the test project explicitly to avoid running all test projects!**

### Core.Tests (Business Logic)
```powershell
# Development (fast - excludes VBA and Screenshot)
dotnet test tests/VisioMcp.Core.Tests/VisioMcp.Core.Tests.csproj --filter "Category=Integration&RunType!=OnDemand&Feature!=VBA&Feature!=VBATrust&Feature!=Screenshot"

# Diagnostic tests (validate patterns, slow ~20s each)
dotnet test tests/VisioMcp.Diagnostics.Tests/VisioMcp.Diagnostics.Tests.csproj --filter "RunType=OnDemand&Layer=Diagnostics"

# VBA tests (manual only - requires VBA trust)
dotnet test tests/VisioMcp.Core.Tests/VisioMcp.Core.Tests.csproj --filter "(Feature=VBA|Feature=VBATrust)&RunType!=OnDemand"

# Screenshot tests (isolated run only - clipboard contention when parallel)
dotnet test tests/VisioMcp.Core.Tests/VisioMcp.Core.Tests.csproj --filter "Feature=Screenshot"

# Specific feature
dotnet test tests/VisioMcp.Core.Tests/VisioMcp.Core.Tests.csproj --filter "Feature=Page"
```

### ComInterop.Tests (Session/Batch Infrastructure)
```powershell
# Session/batch changes (MANDATORY - see CRITICAL-RULES.md Rule 3)
dotnet test tests/VisioMcp.ComInterop.Tests/VisioMcp.ComInterop.Tests.csproj --filter "RunType=OnDemand"
```

### McpServer.Tests (End-to-End Tool Tests)
```powershell
# All MCP tool tests
dotnet test tests/VisioMcp.McpServer.Tests/VisioMcp.McpServer.Tests.csproj

# Specific tool
dotnet test tests/VisioMcp.McpServer.Tests/VisioMcp.McpServer.Tests.csproj --filter "FullyQualifiedName~PageTool"
```

### CLI.Tests (Command-Line Interface)
```powershell
# All CLI tests
dotnet test tests/VisioMcp.CLI.Tests/VisioMcp.CLI.Tests.csproj

# Specific command
dotnet test tests/VisioMcp.CLI.Tests/VisioMcp.CLI.Tests.csproj --filter "FullyQualifiedName~Page"
```

### Run Specific Test by Name
```powershell
# Use full project path + filter
dotnet test tests/VisioMcp.Core.Tests/VisioMcp.Core.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

## Round-Trip Validation Pattern

**Always verify actual Visio state after operations:**

```csharp
// ✅ CREATE → Verify exists
var createResult = await _commands.CreateAsync(batch, "TestTable");
Assert.True(createResult.Success);

var listResult = await _commands.ListAsync(batch);
Assert.Contains(listResult.Items, i => i.Name == "TestTable");  // ✅ Proves it exists!

// ✅ UPDATE → Verify changes applied
var updateResult = await _commands.RenameAsync(batch, "TestTable", "NewName");
Assert.True(updateResult.Success);

var viewResult = await _commands.GetAsync(batch, "NewName");
Assert.Equal("NewName", viewResult.Name);  // ✅ Proves rename worked!

// ✅ DELETE → Verify removed
var deleteResult = await _commands.DeleteAsync(batch, "NewName");
Assert.True(deleteResult.Success);

var finalList = await _commands.ListAsync(batch);
Assert.DoesNotContain(finalList.Items, i => i.Name == "NewName");  // ✅ Proves deletion!
```

### Content Replacement Validation (CRITICAL)

**For operations that replace content (Update, Set, etc.), ALWAYS verify content was replaced, not merged/appended:**

```csharp
// ❌ WRONG: Only checks operation completed
var updateResult = await _commands.UpdateAsync(batch, queryName, newFile);
Assert.True(updateResult.Success);  // Doesn't prove content was replaced!

// ✅ CORRECT: Verify content was replaced, not merged
var updateResult = await _commands.UpdateAsync(batch, queryName, newFile);
Assert.True(updateResult.Success);

var viewResult = await _commands.ViewAsync(batch, queryName);
Assert.Equal(expectedContent, viewResult.Content);  // ✅ Content matches expected
Assert.DoesNotContain("OldContent", viewResult.Content);  // ✅ Old content gone!

// ✅ EVEN BETTER: Test multiple sequential updates (exposes merging bugs)
await _commands.UpdateAsync(batch, queryName, file1);
await _commands.UpdateAsync(batch, queryName, file2);
var viewResult = await _commands.ViewAsync(batch, queryName);
Assert.Equal(file2Content, viewResult.Content);  // ✅ Only file2 content present
Assert.DoesNotContain(file1Content, viewResult.Content);  // ✅ file1 content gone!
```

**Why Critical:** Bug report showed that UpdateAsync was **merging** M code instead of replacing it. Tests passed because they only checked `Success = true`, not actual content. The bug compounded with each update, corrupting queries progressively worse.

**Lesson:** "Operation completed" ≠ "Operation did the right thing". Always verify the actual result.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Shared test file | Each test creates unique file |
| Only test success flag | Verify actual Visio state |
| Save before assertions | Remove Save entirely |
| Save in middle of test | Only at end or in persistence test |
| Manual IDisposable | Use `IClassFixture<TempDirectoryFixture>` |
| .vsdx for VBA tests | Use `.vsdm` |
| "Accept both" assertions | Binary assertions only |
| Missing Feature trait | Add from valid feature list above |

## When Tests Fail

1. Run individually: `--filter "FullyQualifiedName=Namespace.Class.Method"`
2. Check file isolation (unique files?)
3. Check assertions (binary, not conditional?)
4. Check Save (removed unless persistence test?)
5. Verify Visio state (not just success flag?)

**Full checklist**: See CRITICAL-RULES.md Rule 12

---

## LLM Integration Tests

**Location**: `llm-tests/`

**Purpose**: Validate that LLMs correctly use Visio MCP Server and CLI tools using [pytest-aitest](https://github.com/sbroenne/pytest-aitest).

### When to Run

- **Manual/on-demand only** - Not part of CI/CD
- After changing tool descriptions or adding new tools
- To validate LLM behavior patterns (e.g., incremental updates vs rebuild)

### Running LLM Tests

```powershell
# From repo root, run the canonical manual gate
.\scripts\Test-LlmRegressionGate.ps1

# Or run the full suite from llm-tests/
cd llm-tests
uv sync

# Run MCP tests only
uv run pytest -m mcp -v

# Run CLI tests only
uv run pytest -m cli -v

# Run all LLM tests
uv run pytest -m aitest -v
```

### Prerequisites

- `AZURE_OPENAI_ENDPOINT` environment variable
- Windows desktop with Visio installed
- MCP Server built (Release) and CLI available on PATH

### Canonical Gate

The standard manual gate is `.\scripts\Test-LlmRegressionGate.ps1`.

It runs these six scenarios:

- `cli/test_cli_table.py::test_cli_table_create_query`
- `cli/test_cli_chart.py::test_cli_chart_workflows`
- `cli/test_cli_styling.py::test_cli_styling_header_fill`
- `mcp_tests/test_mcp_table.py::test_mcp_table_create_query`
- `mcp_tests/test_mcp_chart.py::test_mcp_chart_workflows`
- `mcp_tests/test_mcp_styling.py::test_mcp_styling_header_fill`

### Configuration Overrides

- `visio_mcp_SERVER_COMMAND` to override MCP server command
- `VISIO_CLI_COMMAND` to override CLI command

### Test Results

Reports are generated in `tests/VisioMcp.LLM.Tests/TestResults/`:
- `report.html` - Visual HTML report
- `report.json` - Machine-readable JSON

See `llm-tests/README.md` for complete documentation.
