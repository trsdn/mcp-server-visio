# Attribution and Provenance

This document records where `mcp-server-visio` comes from, what it inherited, and what
it no longer shares with its upstream projects. It exists because the repository carries a
dual copyright notice in [LICENSE](LICENSE), and that notice deserves an explanation.

## Lineage

```
sbroenne/mcp-server-excel        (Stefan Broenner)
        |
        v
sbroenne/mcp-server-powerpoint   (Stefan Broenner)
        |
        v
trsdn/mcp-server-visio           (this repository)
```

| Project | Author | Role in this repo |
|---|---|---|
| [sbroenne/mcp-server-excel](https://github.com/sbroenne/mcp-server-excel) | Stefan Broenner | Original source of the layered COM-automation architecture |
| [sbroenne/mcp-server-powerpoint](https://github.com/sbroenne/mcp-server-powerpoint) | Stefan Broenner | Direct upstream; this repo was bootstrapped from it |
| [trsdn/mcp-server-visio](https://github.com/trsdn/mcp-server-visio) | Torsten Mahr | Visio-native rewrite of the domain layer |

Both upstream projects are MIT licensed. This repository remains MIT licensed and preserves
the original copyright alongside its own.

## What was inherited

The architectural pattern, not the domain logic:

- the layered split of `ComInterop` / `Core` / `Service` / `CLI` / `McpServer`
- STA threading, batch/session management, and the OLE message filter approach
- the incremental source generators that project Core interfaces into CLI commands and MCP tools
- the two-equal-entry-points principle (MCP server and CLI kept at parity)
- the integration-test-first philosophy for COM automation
- repository conventions: pre-commit checks, skills packaging, instruction files

## What is no longer shared

- **Excel domain code is gone.** No Power Query, DAX, PivotTable, or worksheet logic remains.
  Only a handful of incidental code comments still reference Excel, and those describe
  Office-wide COM behavior rather than inherited implementation.
- **PowerPoint domain code has been replaced** by Visio-native commands for documents, pages,
  layers, shapes, text, ShapeSheet cells, stencils/masters, connectors, and export.
- **The COM surface is different.** Visio's object model (pages, masters, ShapeSheet cells,
  connects) has no meaningful overlap with slides and slide shapes.

## Known carryover

Migration is not finished. Some PowerPoint-era terminology still appears in documentation,
agent instructions, evaluation prompts, and test fixtures. This is tracked as cleanup work
rather than as shipped Visio capability.

[FEATURES.md](FEATURES.md) is the authoritative statement of what is actually validated
against Visio today, and it deliberately separates validated functionality from the
migration backlog. Treat any PowerPoint reference outside of migration-history context as
a leftover to be removed, not as a documented feature.

## Reporting issues

Report issues with this repository to
[trsdn/mcp-server-visio](https://github.com/trsdn/mcp-server-visio/issues) — not to the
upstream projects. See [SECURITY.md](SECURITY.md) for security reports.
