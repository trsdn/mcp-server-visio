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
| Page | Yes | Yes | Via MCP extension | Validated | List, read, create, rename, delete; background pages (mark, attach, detach) |
| Layer | Yes | Yes | Via MCP extension | Validated | List, read, create, delete, shape membership, visibility/print/lock/color flags |
| Shape | Yes | Yes | Via MCP extension | Validated MVP | List, read, add basic shapes, add text boxes, move/resize, delete |
| Text | Yes | Yes | Via MCP extension | Validated MVP | Get, set, find, replace, word count |
| Cell / ShapeSheet | Yes | Yes | Via MCP extension | Validated MVP | Read value/formula, write value, set formula, curated listing |
| Stencil / Master | Yes | Yes | Via MCP extension | Validated MVP | List masters from installed stencils, drop masters on pages; manage the document's own masters (list, read, create from a shape, rename, delete, find instances) |
| Export | Yes | Yes | Via MCP extension | Validated MVP | PDF/XPS document export, page export by file extension, save-copy |
| Hyperlink | Yes | Yes | Via MCP extension | Validated MVP | List, read, add, update, delete. A shape may carry several, unlike PowerPoint |
| Style | Yes | Yes | Via MCP extension | Validated MVP | List, read, create, rename, delete, apply; set a style's own ShapeSheet cells |
| Design guidance | Yes | Yes | Via MCP extension | Validated MVP | Nine diagram archetypes with their stencils and masters, the stencil catalog, cross-archetype patterns, colour palettes |
| Visible live mode | Yes | Yes | Via MCP extension | Validated | Watch Visio while automation runs |

## Current recommended workflow

For new Visio automation, use the validated surfaces above.

The recommended sequence today is:

1. create or open a session
2. work with pages, layers, shapes, text, cells, and stencils
3. save and close the session

## Domains in migration backlog

Twelve command domains inherited from the PowerPoint ancestor remain compiled but suppressed from
the public surface via `[McpTool(..., PublicSurface = false)]`. A further fourteen were probed,
found to have no Visio analogue at all, and **deleted** — 4,768 lines and 82 actions removed in #22.

Every row below carries a disposition backed by **an actual probe of a live Visio 16.0 instance**,
not by inference from the name. That distinction matters: `Document.Theme` and `Document.Sections`
both sound like safe ports and neither exists, while `GlowSize` and `Page.Comments` both sound like
PowerPoint-only concepts and both do.

- **Port** — a real Visio equivalent exists; reimplement in place and republish
- **Remap** — the concept exists under a different name and belongs in an already-public tool

The **Delete** verdict is still valid for this table, and a domain may be added back under it; it
simply has no members left. The fourteen removed were `animation`, `chart`, `customshow`, `media`,
`notes`, `placeholder`, `proofing`, `section`, `slide`, `slideimport`, `slideshow`, `slidetable`,
`smartart` and `transition`.

Note that `section` was deleted rather than ported: `Document.Sections` does not exist, and the
word collides with the *unrelated* ShapeSheet sections tracked in
[#33](https://github.com/trsdn/mcp-server-visio/issues/33).

<!-- BEGIN:LEGACY-DOMAIN-CLASSIFICATION -->

| Domain | Disposition | Owner | Visio evidence | Tracking |
|---|---|---|---|---|
| `comment` | Port | @trsdn | `Page/Shape/Document.Comments` with `Add`, `Item`, `DeleteAll`; verified by adding, editing and deleting a comment | [#62](https://github.com/trsdn/mcp-server-visio/issues/62) |
| `headerfooter` | Port | @trsdn | `Document.HeaderLeft/Center/Right`, `FooterLeft/Center/Right`, `HeaderFooterFont`, `HeaderMargin` all present | [#63](https://github.com/trsdn/mcp-server-visio/issues/63) |
| `image` | Port | @trsdn | `Page.Import` returned a shape with `Type=4` (`visTypeForeignObject`); `Shape.Export` wrote it back to disk | [#64](https://github.com/trsdn/mcp-server-visio/issues/64) |
| `printoptions` | Port | @trsdn | `Document.Print`, `PrintOut`, `ExportAsFixedFormat`, `PrintLandscape`, `PrintCenteredH`, `PaperSize` present | [#65](https://github.com/trsdn/mcp-server-visio/issues/65) |
| `vba` | Port | @trsdn | `Document.VBProject` and `Application.VBE` present; Visio supports VBA and `.vsdm` | [#66](https://github.com/trsdn/mcp-server-visio/issues/66) |
| `tag` | Remap | @trsdn | `Shape.Data1/2/3` present; Shape Data (`Prop.*`) and user cells (`User.*`) both accept named rows | [#33](https://github.com/trsdn/mcp-server-visio/issues/33) |

<!-- END:LEGACY-DOMAIN-CLASSIFICATION -->

`LegacyDomainClassificationTests` asserts that this table lists **exactly** the domains carrying
`[McpTool(PublicSurface = false)]`. Suppressing a domain without recording a disposition fails the
build, and so does leaving a row behind after a domain is deleted or published. A disposition table
that drifts is worse than none, because it is trusted.

### Themes and connectors

Two cross-cutting Visio-native areas are tracked outside the table because they are additive rather
than inherited:

| Area | Disposition | Notes |
|---|---|---|
| Connectors / connection points / routing | Port / redesign | High-priority Visio-native domain ([#32](https://github.com/trsdn/mcp-server-visio/issues/32)) |
| Groups / containers / lists / selection | Port / redesign | Important for real diagram workflows ([#36](https://github.com/trsdn/mcp-server-visio/issues/36)) |
| Broader ShapeSheet coverage | Port | Generic section and row access beyond the current cell surface ([#33](https://github.com/trsdn/mcp-server-visio/issues/33)) |
| Data graphics / data recordsets | Redesign | Visio Professional only; needs a deliberate Visio-first model |

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
