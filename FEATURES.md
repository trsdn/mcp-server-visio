# VisioMcp Feature Inventory

This file is the truthful feature snapshot for the current Visio repo state.

It intentionally does **not** claim that every inherited template surface is already implemented for Visio. Instead, it separates:

- validated Visio functionality
- migration backlog domains
- explicit cleanup rules for legacy carryover

## Validated Visio functionality

The following domains are implemented and covered by focused validation.
Action counts are taken from the generated action enums, so they cannot drift out of date:

| Domain | Actions | CLI | MCP | VS Code surface | Status | Notes |
|---|---|---|---|---|---|---|
| File / Session | 1 + session tool | Yes | Yes | Via MCP extension | Validated | Open, create, list, close, save, visible mode |
| Page | 16 | Yes | Yes | Via MCP extension | Validated | List, read, create, rename, delete |
| Layer | 10 | Yes | Yes | Via MCP extension | Validated | List, read, create, delete, shape membership, visibility/print/lock/color flags |
| Shape | 28 | Yes | Yes | Via MCP extension | Validated MVP | List, read, add basic shapes, add text boxes, connectors, group/ungroup, z-order, move/resize, delete |
| Shape alignment | 2 | Yes | Yes | Via MCP extension | Validated MVP | Align and distribute selections |
| Text | 5 | Yes | Yes | Via MCP extension | Validated MVP | Get, set, find, replace, word count |
| Cell / ShapeSheet | 5 | Yes | Yes | Via MCP extension | Validated MVP | Read value/formula, write value, set formula, curated listing |
| Stencil / Master | 2 | Yes | Yes | Via MCP extension | Validated MVP | List masters from installed stencils, drop masters on pages |
| Export | 5 | Yes | Yes | Via MCP extension | Validated MVP | PDF/XPS document export, page export by file extension, save-copy |
| Document properties | 4 | Yes | Yes | Via MCP extension | Validated MVP | Read and write document metadata |
| Window | 23 | Yes | Yes | Via MCP extension | Validated MVP | Zoom, scroll, window geometry, visible live mode |

**Total: 11 command domains, 101 generated actions, 11 MCP tools.**

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
| Groups / containers / lists / selection | Port / redesign | Grouping and z-order exist; containers and list shapes still missing |
| Broader ShapeSheet coverage | Port | Expand beyond curated MVP cells |
| Data graphics / data recordsets | Redesign | Needs deliberate Visio-first model |
| Print / metadata | Port / redesign | Keep only Visio-meaningful operations beyond the validated export MVP |
| Hyperlinks / comments | Redesign | Removed with the presentation-era code; must be rebuilt on `Shape.Hyperlinks` |
| Shape formatting (fill, line, rotation, scale) | Redesign | Reachable today through the `cell` tool (`FillForegnd`, `LineColor`, `Angle`); ergonomic wrappers still to be written against the ShapeSheet |
| Character / paragraph text formatting | Redesign | Must be rebuilt on `Char.*` and `Para.*` ShapeSheet rows |
| AutoConnect / DropConnected | New | Largest missing Visio-native capability |

## Removed presentation-era carryover

The repository was bootstrapped from a PowerPoint MCP server (see [ATTRIBUTION.md](ATTRIBUTION.md)).
Everything below called the PowerPoint object model and therefore could never have worked against
Visio. It has been removed rather than quarantined, so the public surface no longer advertises
operations that fail at runtime:

- 26 command domains (accessibility, animation, background, chart, comment, custom show, design,
  header/footer, hyperlink, image, master, media, notes, page setup, placeholder, print options,
  proofing, section, slide, slide import, slideshow, slide table, SmartArt, tag, transition, VBA)
- 36 individual methods inside the surviving `shape` and `text` domains
- the `Microsoft.Office.Interop.PowerPoint` package reference

## Cleanup rules

When touching docs, package metadata, prompts, or generated surfaces:

- do not describe inherited PowerPoint inventory as shipped Visio capability
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
- [ATTRIBUTION.md](ATTRIBUTION.md)
- [docs/INSTALLATION.md](docs/INSTALLATION.md)
- [docs/VISIO-COM-REFERENCE.md](docs/VISIO-COM-REFERENCE.md)
- [src/VisioMcp.CLI/README.md](src/VisioMcp.CLI/README.md)
- [src/VisioMcp.McpServer/README.md](src/VisioMcp.McpServer/README.md)
- [vscode-extension/README.md](vscode-extension/README.md)
