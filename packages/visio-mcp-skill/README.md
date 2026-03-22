# visio-mcp-skill

An [Agent Skill](https://agentskills.io) for automating Microsoft Visio through the `VisioMcp.McpServer` MCP surface.

## What this skill does

When loaded by an AI agent (Claude, Codex, Cursor, Gemini CLI, etc.), this skill teaches the agent how to work against the current Visio MVP surface over MCP:

- **Session workflows** — create, open, inspect, and close Visio files
- **Page operations** — manage diagram pages
- **Shape operations** — add and inspect shapes and text boxes
- **Text workflows** — read and update labels
- **ShapeSheet cells** — inspect and modify geometry/formula-backed cells
- **Stencil workflows** — list masters and drop standard Visio masters onto a page

## Requirements

- Windows with Microsoft Visio desktop installed
- [`VisioMcp.McpServer`](https://github.com/trsdn/mcp-server-visio) installed and available to your MCP client

## Install

```bash
npx skillpm install visio-mcp-skill
```

Or with npm directly:

```bash
npm install visio-mcp-skill
```

## License

MIT
