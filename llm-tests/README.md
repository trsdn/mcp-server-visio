# VisioMcp LLM Integration Tests

LLM-powered integration tests for both VisioMcp MCP Server and Visio CLI using pytest-aitest.

## Prerequisites

- Windows desktop with Microsoft Visio installed
- .NET 10 SDK
- Azure OpenAI endpoint configured
- VisioMcp MCP Server and CLI built/installed

### Azure OpenAI

Set the endpoint for Entra ID auth:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-resource>.openai.azure.com/"
```

## Setup (uv + local pytest-aitest)

From this directory:

```powershell
uv sync
```

This uses a local editable dependency via:

```toml
[tool.uv.sources]
pytest-aitest = { path = "../../../pytest-aitest", editable = true }
```

## Build MCP Server (Required)

```powershell
dotnet build ..\..\src\VisioMcp.McpServer\VisioMcp.McpServer.csproj -c Release
```

## Run Tests (Manual Only)

### MCP Server tests

```powershell
uv run pytest -m mcp -v
```

### CLI tests

```powershell
uv run pytest -m cli -v
```

### All LLM tests

```powershell
uv run pytest -m aitest -v
```

### Canonical regression gate

Run the standard manual gate with the helper script from the repository root:

```powershell
.\scripts\Test-LlmRegressionGate.ps1
```

This runs the canonical six scenarios:

- `cli/test_cli_diagram.py::test_create_document_with_named_page`
- `cli/test_cli_diagram.py::test_draw_two_shapes_and_connect_them`
- `cli/test_cli_diagram.py::test_shapesheet_cell_roundtrip`
- `mcp_tests/test_mcp_diagram.py::test_create_document_with_named_page`
- `mcp_tests/test_mcp_diagram.py::test_draw_two_shapes_and_connect_them`
- `mcp_tests/test_mcp_diagram.py::test_shapesheet_cell_roundtrip`

Use this gate after changing skill content, MCP tool descriptions, CLI help text, or other LLM-facing workflow guidance.

## Configuration Overrides

- `MCP_SERVER_COMMAND` — override MCP server command (full command line)
- `CLI_COMMAND` — override CLI command (default: `visiocli`)

Example:

```powershell
$env:MCP_SERVER_COMMAND = "d:\\source\\mcp-server-visio\\src\\VisioMcp.McpServer\\bin\\Release\\net9.0-windows\\VisioMcp.McpServer.exe"
$env:CLI_COMMAND = "d:\\source\\mcp-server-visio\\src\\VisioMcp.CLI\\bin\\Release\\net9.0-windows\\visiocli.exe"
```

## Test Structure

- `test_mcp_*.py` — MCP Server workflows
- `test_cli_*.py` — CLI workflows
- `TestResults/` — HTML reports and artifacts
