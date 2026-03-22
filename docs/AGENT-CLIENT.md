# Agent Client Architecture

`src\VisioMcp.Agent` is the official source-side Copilot SDK controller for multi-phase Visio diagram generation in this repository.

It exists to handle orchestration that should **not** live inside the MCP server itself.

## Why This Component Exists

Some diagram-generation tasks need more than one prompt:

- create a structured page-and-shape plan first
- build the diagram page-by-page
- re-open and verify the result
- repair incomplete or structurally weak output if the produced artifact is wrong

That logic is intentionally client-side.

The MCP server remains responsible for primitive Visio capabilities, while the agent owns sequencing, retries, artifact checks, and run summaries.

## Responsibility Split

| Layer | Responsibility |
|---|---|
| `src\VisioMcp.Core` + `src\VisioMcp.Service` + `src\VisioMcp.McpServer` | Primitive Visio operations, sessions, and MCP transport |
| `skills\shared\*.md` | Planning, layout, and review guidance shared across hosts |
| `src\VisioMcp.Agent` | Plan → execute → verify → repair orchestration plus deterministic artifact/quality validation |
| `eval\` | Experimental measurement, scoring, sweeps, and skill-tuning loops |

## Design Constraints

The component deliberately avoids pushing orchestration into the server:

- no MCP batch dependency
- no MCP subagent dependency
- no server-side planner / worker state machine
- no hidden session coordinator inside the MCP service

Instead, one local process controls the entire workflow and talks to the MCP server with normal sequential tool calls.

## Runtime Flow

The orchestrator currently works in four logical phases.

### 1. Plan

- Reads archetype guidance from `src\VisioMcp.Core\Data\archetypes\registry.md`
- Reads generation guidance from `skills\shared\generation-pipeline.md`
- Asks the model for a JSON diagram plan
- Falls back to fenced JSON, outermost JSON, or markdown page blocks if the reply is not perfectly structured

Output:

- `*.plan.json`

### 2. Execute

- Creates a new diagram at the requested output path
- Builds pages in plan order
- Uses normal MCP tool calls only
- Requires `file(action='close', save=true)` before finishing

### 3. Verify

- Re-opens the generated diagram
- Inspects structure with standard MCP read/list operations
- Validates expected pages, shapes, labels, and other artifact details
- Applies targeted fixes for structural and business-quality problems

### 4. Repair

- Runs if artifact validation detects an incomplete or low-quality result
- Re-opens or recreates the diagram
- Repairs the structure against the fixed plan
- Verifies the final page count before saving and closing

## Reliability Behaviors

The official client keeps the hardened behaviors from the verified prototype:

- **Plan parsing fallback** — handles JSON objects, nested `plan.pages`, arrays, fenced JSON, and markdown page blocks
- **Timeout fallback** — if the SDK times out waiting for `session.idle` but the expected artifact already exists, the phase can still succeed
- **Save-before-close cleanup** — targeted cleanup saves open diagrams before closing them
- **Artifact validation** — output is reopened and checked for required page and label content
- **Quality validation** — diagrams can be rejected when required content is missing or structure is clearly incomplete
- **Repair loop** — incomplete or weak output triggers a repair phase instead of silently accepting a broken artifact

## Files in This Component

| Path | Purpose |
|---|---|
| `src\VisioMcp.Agent\src\cli.mjs` | CLI entry point |
| `src\VisioMcp.Agent\src\orchestrator.mjs` | Phase sequencing and artifact validation |
| `src\VisioMcp.Agent\src\runtime.mjs` | Copilot SDK session/runtime wrapper |
| `src\VisioMcp.Agent\src\planner.mjs` | Plan extraction and normalization |
| `src\VisioMcp.Agent\patch-deps.cjs` | Node 24 compatibility patch for `@github/copilot-sdk` dependencies |
| `src\VisioMcp.Agent\tests\planner.test.mjs` | Fast local regression coverage for plan parsing |

## Build and Test

```powershell
dotnet build src\VisioMcp.McpServer\VisioMcp.McpServer.csproj -c Release

Set-Location src\VisioMcp.Agent
npm install
npm run check
npm test
```

For an end-to-end smoke run on a Visio-enabled Windows desktop:

```powershell
node .\src\cli.mjs run `
  --task "Build a one-page process diagram with a Start box, one decision, and one End box." `
  --output "C:\Users\you\AppData\Local\Temp\visio-agent-smoke.vsdx" `
  --overwrite
```

## Output Artifacts

For `diagram.vsdx`, the client also writes:

- `diagram.plan.json`
- `diagram-artifacts\run-summary.json`
- optional review/export artifacts inside `diagram-artifacts\`

These artifacts make the run inspectable and reproducible without pushing orchestration into the server.

## Relationship to the Eval Framework

The agent and eval harnesses use related building blocks, but they are not the same thing:

- `src\VisioMcp.Agent` is for one production-style build workflow
- `eval\` is for repeated experiments, judgments, and score histories

See also:

- [Eval Framework](../eval/README.md)
- [Archetype Pipeline](ARCHETYPE-PIPELINE.md)
