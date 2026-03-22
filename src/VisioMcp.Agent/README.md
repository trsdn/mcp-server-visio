# VisioMcp.Agent

Official source-side Copilot SDK orchestrator for multi-step Visio diagram workflows through `mcp-server-visio`.

## Purpose

`src\VisioMcp.Agent` is the repository's source-side orchestration client for larger Visio tasks.

It exists for workflows that are larger than a single prompt/response exchange:

- plan a diagram from one natural-language task
- execute the plan through normal sequential MCP tool calls
- verify the produced diagram
- repair incomplete output when artifact and quality validation fail

This keeps the product boundary clean:

- `src\VisioMcp.McpServer` stays focused on primitive Visio capabilities
- `skills\shared\*.md` stays focused on LLM guidance
- `src\VisioMcp.Agent` owns orchestration, retries, artifact validation, and run summaries

## Architectural Boundary

`VisioMcp.Agent` is **not** a third server surface and it does **not** move orchestration into the MCP server.

It deliberately avoids:

- MCP batch dependencies
- MCP subagent dependencies
- server-side planner / worker / verifier state machines

Instead, one client process runs these logical phases:

1. **Plan** — generate structured page and diagram intents without touching Visio
2. **Execute** — build the diagram through standard MCP calls
3. **Verify** — reopen and inspect the generated diagram
4. **Repair** — re-enter the diagram if structural or business-quality validation finds gaps

## What the Agent Writes

For an output file like `quarterly-review.vsdx`, the agent also writes:

- `quarterly-review.plan.json` — extracted structured diagram plan
- `quarterly-review-artifacts\` — verification exports and runtime traces
- `quarterly-review-artifacts\run-summary.json` — high-level execution summary

## Run from Source

```powershell
dotnet build src\VisioMcp.McpServer\VisioMcp.McpServer.csproj -c Release

Set-Location src\VisioMcp.Agent
npm install
npm run check
npm test

node .\src\cli.mjs run `
  --task "Create a Visio process diagram with pages for Overview and Escalation and add the initial labeled shapes." `
  --output "C:\Users\you\Documents\process-review.vsdx"
```

Optional flags:

```powershell
--model gpt-5.4
--plan-file C:\path\to\precomputed.plan.json
--show
--overwrite
--skip-verify
--mcp-server "C:\path\to\VisioMcp.McpServer.exe"
--plan-timeout-ms 120000
--execute-timeout-ms 900000
--verify-timeout-ms 300000
```

## Default MCP Server Resolution

By default the client looks for:

- `src\VisioMcp.McpServer\bin\Release\net9.0-windows\VisioMcp.McpServer.exe`

You can override that with:

- `--mcp-server`
- `VISIO_MCP_AGENT_MCP_SERVER`
- `VISIO_MCP_SERVER_COMMAND`
- `visio_mcp_SERVER_COMMAND`

## Reusing a Precomputed Plan

If planning is already done or you want to debug execution in isolation, you can skip the planning phase:

```powershell
node .\src\cli.mjs run `
  --task "Execute this process-diagram plan." `
  --plan-file "C:\path\to\dashboard.plan.json" `
  --output "C:\Users\you\Documents\dashboard.vsdx"
```

Plan-file runs still validate the saved diagram against required literal labels and structural expectations from the plan. If execution times out after producing a partial or low-quality artifact, the agent reopens the diagram, repairs it, and validates again before succeeding.

## Reliability Behaviors

The official source component keeps the hardened behaviors from the verified prototype:

- JSON-first plan extraction with markdown fallback
- timeout fallback when the expected artifact already exists
- save-before-close cleanup for open agent documents
- artifact validation against expected page and label content
- literal text validation against the plan
- targeted repair pass when the generated diagram is incomplete or structurally unacceptable

## Validation Scope

Local validation for this component should always cover:

```powershell
Set-Location src\VisioMcp.Agent
npm run check
npm test
```

For end-to-end smoke validation, build the MCP server and run a small `run --task ...` scenario against a real Visio-enabled Windows desktop.

## Related Docs

- [Agent Client Architecture](../../docs/AGENT-CLIENT.md)
- [Installation Guide](../../docs/INSTALLATION.md)
- [Eval Framework](../../eval/README.md)
- [Archetype Pipeline](../../docs/ARCHETYPE-PIPELINE.md)
