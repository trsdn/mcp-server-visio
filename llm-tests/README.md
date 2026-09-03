# VisioMcp LLM Integration Tests

LLM-powered integration tests for the VisioMcp MCP Server and the Visio CLI, using `pytest-aitest`.

These differ from the .NET tests: rather than calling the API directly, they give a model a task in
plain language and check that it reaches the right tools and produces the right drawing. They test
the **guidance** — skills, tool descriptions, CLI help — as much as the code.

## Prerequisites

- Windows desktop with Microsoft Visio installed
- .NET 9 SDK
- Azure OpenAI endpoint configured
- MCP Server and CLI built in Release

### Azure OpenAI

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-resource>.openai.azure.com/"
```

## Setup

From this directory:

```powershell
uv sync
```

## Build first

Both the MCP server and the CLI must be built in Release — the fixtures look for the built
executables and otherwise fall back to a slow `dotnet run`:

```powershell
dotnet build ..\src\VisioMcp.McpServer\VisioMcp.McpServer.csproj -c Release
dotnet build ..\src\VisioMcp.CLI\VisioMcp.CLI.csproj -c Release
```

## Run

```powershell
uv run pytest -m mcp -v      # MCP Server
uv run pytest -m cli -v      # CLI
uv run pytest -m aitest -v   # both
```

Every test creates its own `.vsdx` under `%TEMP%`, so runs do not collide.

## What is covered

| File | Covers |
|---|---|
| `mcp_tests/test_mcp_page_shape.py` | pages, shapes, text, page size through PageSheet cells |
| `mcp_tests/test_mcp_diagram.py` | stencil masters, connectors, design catalog, background pages |
| `mcp_tests/test_mcp_styling.py` | fills, named styles, layers, shape data |
| `cli/test_cli_page_shape.py` | the same foundation through `visiocli`, including session close |
| `cli/test_cli_diagram.py` | flowchart from stencil masters, design catalog, `--help` discovery |

## What these assert beyond "it worked"

A prompt can be satisfied the wrong way and still produce a plausible drawing, so several tests
assert the **method** rather than only the outcome:

- `assert_used_tool(result, "stencil")` — the agent dropped a master rather than drawing a
  rectangle. A drawn diamond is not a Decision, and nothing downstream treats it as one.
- `assert_cli_args_contain(result, "connect-shapes")` — the shapes were joined. Shapes placed but
  never connected is the most common way generated output looks right and is useless.
- `assert_used_tool(result, "cell")` — page size was set through the PageSheet, which is where it
  lives; there is no page-size property.
- Reading a created value back into the answer, so a test cannot pass on a drawing that was never
  actually written.

## Canonical regression gate

After changing skill content, MCP tool descriptions or CLI help, run the standard gate from the
repository root. It builds both binaries in Release, points the tests at them, and runs three CLI
and three MCP scenarios:

```powershell
.\scripts\Test-LlmRegressionGate.ps1
```

## Configuration overrides

- `MCP_SERVER_COMMAND` — full command line for the MCP server
- `CLI_COMMAND` — path to `visiocli.exe`
- `VISIO_MCP_AGENT_CLI` / `VISIOCLI_PATH` — used by the agent client, not by these tests

```powershell
$env:CLI_COMMAND = "$PWD\..\src\VisioMcp.CLI\bin\Release\net9.0-windows\visiocli.exe"
```

## Notes

- `TestResults/` holds the HTML report and artifacts.
- The CLI tests start and stop the CLI daemon automatically (`cli/conftest.py`).
- These are manual: they cost model calls and need a desktop Visio, so CI does not run them.
