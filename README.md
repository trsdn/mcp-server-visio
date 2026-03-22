# VisioMcp

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/trsdn/mcp-server-visio)

Windows-first Visio automation through real desktop COM, exposed consistently through an MCP server, a CLI, and a VS Code extension.

## What this repo is today

`mcp-server-visio` is no longer just a renamed template. It already ships a validated Visio-native MVP with:

- document sessions
- page operations
- layer management
- shape create/read/move/delete flows
- text read/write/find/replace
- ShapeSheet cell read/write and formula operations
- stencil master listing and master drop
- visible live mode so you can watch Visio while automation runs

These flows are exercised through focused integration tests and manual Visio smoke checks.

## Official surfaces

This repo treats all three entry points as first-class:

- **MCP Server** - best for conversational AI clients and rich tool discovery
- **CLI** (`visiocli`) - best for scripting, coding agents, and shell workflows
- **VS Code extension** - easiest on-ramp for GitHub Copilot inside VS Code

All new Visio work is expected to stay aligned across these surfaces.

## Current validated Visio MVP

| Domain | Status | Notes |
|---|---|---|
| File / Session | Validated | Create, open, list, save, close, visible mode |
| Page | Validated | List, read, create, rename, delete |
| Layer | Validated | List, read, create, delete, shape membership, visibility/print/lock/color |
| Shape | Validated MVP | List, read, add basic shapes/text boxes, move/resize, delete |
| Text | Validated MVP | Get, set, find, replace, word count |
| Cell / ShapeSheet | Validated MVP | Read value, read formula, write value, set formula, curated cell listing |
| Stencil / Master | Validated MVP | List masters from installed stencils, drop master to page |

See `FEATURES.md` for the current feature inventory and migration backlog.

## Live mode

Visio automation can run hidden or visibly:

- MCP supports visible mode for "show me while you work" workflows
- CLI supports visible mode through `session create --show` and `session open --show`
- this is useful for debugging, demos, and side-by-side agent workflows

Example:

```powershell
visiocli -q session open C:\Work\diagram.vsdx --show
visiocli -q page list --session <session-id>
visiocli -q session close --session <session-id> --save
```

## Quick start

### Install the tools

```powershell
dotnet tool install --global VisioMcp.McpServer
dotnet tool install --global VisioMcp.CLI
```

More setup details: [docs/INSTALLATION.md](docs/INSTALLATION.md)

### CLI example

```powershell
visiocli -q session create C:\Work\process.vsdx
visiocli -q shape add-shape --session <session-id> --page-index 1 --left 1 --top 1 --width 2 --height 1 --auto-shape-type 1
visiocli -q text set --session <session-id> --page-index 1 --shape-name "Rectangle.1" --text "Start"
visiocli -q cell read --session <session-id> --page-index 1 --shape-name "Rectangle.1" --cell-name Width
visiocli -q session close --session <session-id> --save
```

### MCP example prompts

- "Create a new Visio diagram and add a page named Overview"
- "Drop a built-in master onto page 1 and label it Start"
- "Read the Width and PinX ShapeSheet cells for Rectangle.1"
- "Show me Visio while you work"

## Architecture

VisioMcp uses real Windows COM automation against the Visio desktop application.

```
MCP Server  ----\
                 >---- VisioMcp Service ----> Visio COM automation
CLI         ----/

VS Code extension
  -> installs/registers the MCP server for GitHub Copilot
```

Key architectural properties:

- shared session management between MCP and CLI
- Visio-native COM control instead of file-only manipulation
- incremental generator-driven surfaces
- integration-first validation against the real desktop app

## Current migration posture

The repo still contains inherited legacy areas from the original PowerPoint-oriented bootstrap. The cleanup strategy is now:

- keep only truthful Visio-facing docs and package metadata
- port inherited surfaces to real Visio behavior where they make sense
- redesign domains that need a Visio-native API shape
- remove clearly misleading PowerPoint-only leftovers instead of documenting them as if they already worked

## Repository roadmap

The next major wave focuses on:

1. repo and GitHub surface cleanup
2. full inventory of inherited legacy surfaces
3. architecture decisions for broad Visio COM parity domains
4. incremental parity slices across Core, Service, CLI, MCP, and VS Code
5. release-readiness hardening

## Related docs

- [Feature inventory and migration status](FEATURES.md)
- [Installation guide](docs/INSTALLATION.md)
- [Visio COM reference](docs/VISIO-COM-REFERENCE.md)
- [CLI guide](src/VisioMcp.CLI/README.md)
- [MCP server guide](src/VisioMcp.McpServer/README.md)
- [VS Code extension guide](vscode-extension/README.md)
- [Skills overview](skills/README.md)

## Contributing

See [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

## License

MIT. See [LICENSE](LICENSE).
