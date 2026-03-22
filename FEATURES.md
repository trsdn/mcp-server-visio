# VisioMcp Feature Inventory

This file is the truthful feature snapshot for the current Visio repo state.

It intentionally does **not** claim that every inherited template surface is already implemented for Visio. Instead, it separates:

- validated Visio functionality
- migration backlog domains
- explicit cleanup rules for legacy carryover

## Validated Visio functionality

The following domains are implemented and covered by focused validation:

| Domain | CLI | MCP | VS Code surface | Status | Notes |
|---|---|---|---|---|---|
| File / Session | Yes | Yes | Via MCP extension | Validated | Open, create, list, close, save, visible mode |
| Page | Yes | Yes | Via MCP extension | Validated | List, read, create, rename, delete |
| Layer | Yes | Yes | Via MCP extension | Validated | List, read, create, delete, shape membership, visibility/print/lock/color flags |
| Shape | Yes | Yes | Via MCP extension | Validated MVP | List, read, add basic shapes, add text boxes, move/resize, delete |
| Text | Yes | Yes | Via MCP extension | Validated MVP | Get, set, find, replace, word count |
| Cell / ShapeSheet | Yes | Yes | Via MCP extension | Validated MVP | Read value/formula, write value, set formula, curated listing |
| Stencil / Master | Yes | Yes | Via MCP extension | Validated MVP | List masters from installed stencils, drop masters on pages |
| Export | Yes | Yes | Via MCP extension | Validated MVP | PDF/XPS document export, page export by file extension, save-copy |
| Visible live mode | Yes | Yes | Via MCP extension | Validated | Watch Visio while automation runs |

## Current recommended workflow

For new Visio automation, use the validated surfaces above.

The recommended sequence today is:

1. create or open a session
2. work with pages, layers, shapes, text, cells, and stencils
3. save and close the session

## Domains in migration backlog

These areas are part of the planned broad parity push, but should not yet be marketed as implemented unless validated individually:

| Domain | Planned disposition | Notes |
|---|---|---|
| Connectors / connection points / routing | Port / redesign | High-priority Visio-native domain |
| Layers / styles / themes | Partially validated / continue port | Layer management is now validated; styles/themes still need Visio-specific API work |
| Groups / containers / lists / selection | Port / redesign | Important for real diagram workflows |
| Broader ShapeSheet coverage | Port | Expand beyond curated MVP cells |
| Data graphics / data recordsets | Redesign | Needs deliberate Visio-first model |
| Print / metadata | Port / redesign | Keep only Visio-meaningful operations beyond the validated export MVP |
| Hyperlinks / comments / document metadata | Port / redesign | Evaluate per-domain fit |
| Legacy slide-era presentation concepts | Remove unless remapped | Do not preserve misleading PowerPoint semantics |

## Cleanup rules

When touching docs, package metadata, prompts, or generated surfaces:

- do not describe legacy PowerPoint inventory as shipped Visio capability
- do not keep PowerPoint terminology in user-facing text unless explicitly discussing migration history
- remove clearly misleading carryover instead of padding feature counts
- keep CLI, MCP, and VS Code messaging aligned

## Verification sources

The current validated snapshot is based on:

- focused CLI integration tests
- focused MCP integration tests
- skill-generation checks
- manual Visio smoke workflows, including visible mode

## Related docs

- [README.md](README.md)
- [docs/INSTALLATION.md](docs/INSTALLATION.md)
- [docs/VISIO-COM-REFERENCE.md](docs/VISIO-COM-REFERENCE.md)
- [src/VisioMcp.CLI/README.md](src/VisioMcp.CLI/README.md)
- [src/VisioMcp.McpServer/README.md](src/VisioMcp.McpServer/README.md)
- [vscode-extension/README.md](vscode-extension/README.md)
