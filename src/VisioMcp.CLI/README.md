# VisioMcp.CLI - Command-Line Interface for Visio Automation

[![NuGet](https://img.shields.io/nuget/v/VisioMcp.CLI.svg)](https://www.nuget.org/packages/VisioMcp.CLI)
[![Downloads](https://img.shields.io/nuget/dt/VisioMcp.CLI.svg)](https://www.nuget.org/packages/VisioMcp.CLI)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

`VisioMcp.CLI` provides the `visiocli` command for scripting, coding-agent workflows, CI jobs, and local automation against Microsoft Visio on Windows.

> Install `VisioMcp.CLI` for the CLI surface. Install `VisioMcp.McpServer` separately when you also want the MCP server surface.

## Best for

- coding agents that prefer compact CLI help over large MCP schemas
- repeatable scripts and PowerShell automation
- local validation of Visio workflows
- CI-style orchestration on Windows desktops with Visio installed

## Current validated Visio MVP

The recommended Visio-first path today is:

- `session` - create, open, close, list
- `page` - list, read, create, set-name, delete
- `shape` - list, read, add-shape, add-textbox, move-resize, delete
- `text` - get, set, find, replace, word-count
- `cell` - read, read-formula, write, set-formula, list
- `stencil` - list-masters, drop-master

These are the CLI flows currently covered by focused integration tests.

## Installation

```powershell
dotnet tool install --global VisioMcp.CLI
visiocli --version
visiocli --help
```

## Quick start

```powershell
# Create or open a diagram and capture the returned session id
visiocli -q session create C:\Work\process.vsdx
visiocli -q session open C:\Work\process.vsdx --show    # Open Visio visibly while you work

# Inspect pages
visiocli -q page list --session <session-id>

# Add a basic shape and label it
visiocli -q shape add-shape --session <session-id> --page-index 1 --left 1 --top 1 --width 2 --height 1 --auto-shape-type 1
visiocli -q text set --session <session-id> --page-index 1 --shape-name "Rectangle.1" --text "Start"

# Inspect or modify ShapeSheet cells
visiocli -q cell read --session <session-id> --page-index 1 --shape-name "Rectangle.1" --cell-name Width
visiocli -q cell write --session <session-id> --page-index 1 --shape-name "Rectangle.1" --cell-name Width --value 3

# Save and close
visiocli -q session close --session <session-id> --save
```

## Quiet mode

Use `-q` or `--quiet` when you want JSON-only output for scripts and coding agents.

## Notes

- page indices are 1-based
- shape names should be discovered with `shape list` before later commands target them
- stencil workflows are best when you already know the stencil path or first inspect it with `stencil list-masters`

## Migration note

This repository was bootstrapped from a PowerPoint-oriented foundation. Some legacy command groups still exist in the codebase, but the Visio-first recommended workflow for new automation is the MVP surface listed above.

## Requirements

- Windows
- Microsoft Visio desktop installed

## Related

- [Repository](https://github.com/trsdn/mcp-server-visio)
- [Installation Guide](https://github.com/trsdn/mcp-server-visio/blob/main/docs/INSTALLATION.md)
- [MCP Server README](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.McpServer/README.md)
- [Feature Reference](https://github.com/trsdn/mcp-server-visio/blob/main/FEATURES.md)
