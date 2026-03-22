# VisioMcp VS Code Extension

[![GitHub](https://img.shields.io/badge/GitHub-trsdn%2Fmcp--server--visio-blue)](https://github.com/trsdn/mcp-server-visio)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Use Microsoft Visio from GitHub Copilot inside VS Code through a bundled MCP server, bundled CLI, and bundled agent skills.

## What this extension gives you

- One-click setup for the Visio MCP server in VS Code
- Bundled `visiocli` CLI for scriptable workflows
- Bundled `visio-mcp` and `visio-cli` agent skills
- Windows desktop automation against the live Visio application

## Current validated Visio MVP

The current Visio-first workflow is centered on:

- sessions and document lifecycle
- pages
- shapes
- text
- ShapeSheet cells
- stencil masters

These flows are the recommended path today in the extension, CLI, and MCP server.

## Example prompts

- "Create a new Visio file called process.vsdx and add a page named Overview"
- "List the pages in the current diagram"
- "Add a rectangle on page 1 and label it Start"
- "Read the Width cell for Rectangle.1"
- "List masters in a stencil and drop one onto page 1"
- "Show me Visio while you work"

## Quick start

1. Install the extension.
2. Open Copilot Chat in VS Code.
3. Ask for a Visio task in natural language.

The extension bundles the required MCP server and CLI, so no separate .NET setup is required for the VS Code path.

## Requirements

- Windows
- Microsoft Visio desktop installed

## Troubleshooting

If Copilot does not see the Visio tools:

- restart VS Code
- check the Output panel for the VisioMcp extension channel
- ensure Visio desktop is installed and launches normally

If a file is locked:

- close the target Visio document in other Visio windows
- retry the command from Copilot

## Related

- [Repository](https://github.com/trsdn/mcp-server-visio)
- [Installation Guide](https://github.com/trsdn/mcp-server-visio/blob/main/docs/INSTALLATION.md)
- [CLI README](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.CLI/README.md)
- [MCP Server README](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.McpServer/README.md)
