# VisioMcp - Model Context Protocol Server for Visio

<!-- mcp-name: io.github.trsdn/mcp-server-visio -->
mcp-name: io.github.trsdn/mcp-server-visio

[![NuGet](https://img.shields.io/nuget/v/VisioMcp.McpServer.svg)](https://www.nuget.org/packages/VisioMcp.McpServer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VisioMcp.McpServer.svg)](https://www.nuget.org/packages/VisioMcp.McpServer)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-blue.svg)](https://github.com/trsdn/mcp-server-visio)

`VisioMcp.McpServer` exposes Microsoft Visio automation to MCP clients such as GitHub Copilot, Claude Desktop, Cursor, Cline, and Windsurf.

## Best for

- conversational AI clients that benefit from rich tool schemas
- iterative diagram-building workflows
- shared-session automation together with the CLI

## Current validated Visio MVP

The recommended MCP workflow today is centered on:

- `file` for session lifecycle
- `page` for page management
- `shape` for basic shape creation and movement
- `text` for labels and search/replace
- `cell` for ShapeSheet-backed value and formula editing
- `stencil` for installed stencil master discovery and dropping

Direct MCP-oriented integration tests currently cover `page`, `text`, `cell`, and `stencil`.

## Installation

```powershell
dotnet tool install --global VisioMcp.McpServer
```

Optional, if you also want the scripting surface:

```powershell
dotnet tool install --global VisioMcp.CLI
```

## Example MCP-style workflows

- open or create a `.vsdx` file
- list pages and inspect one page
- add a shape or text box to a page
- set text on the created shape
- read or write ShapeSheet cells like `Width`, `Height`, `PinX`, and `PinY`
- list masters from a stencil and drop one onto a page

## Requirements

- Windows
- Microsoft Visio desktop installed

## Architecture note

The MCP server and CLI are equal entry points. Both forward into the shared `VisioMcp.Service`, which manages the live Visio session and keeps behavior aligned across both surfaces.

## Migration note

This repository started from a Visio-oriented template. Legacy categories still exist during migration, but the Visio-first path for new clients is the MVP surface described above.

## Related

- [Repository](https://github.com/trsdn/mcp-server-visio)
- [Installation Guide](https://github.com/trsdn/mcp-server-visio/blob/main/docs/INSTALLATION.md)
- [CLI README](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.CLI/README.md)
- [Feature Reference](https://github.com/trsdn/mcp-server-visio/blob/main/FEATURES.md)
