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
| Comment | Yes | Yes | Via MCP extension | Validated MVP | List, add, delete, clear reviewer comments on pages and shapes; separate from the ShapeSheet alt-text cell |
| Container | Yes | Yes | Via MCP extension | Validated MVP | Drop Visio containers and list containers; manage members; list memberships; drop and inspect callouts |
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

One command domain inherited from the PowerPoint ancestor remains compiled but suppressed from
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

Later deletions followed the same test and are not in that list of fourteen: `accessibility` (#77),
`background` and `pagesetup` (#91), and `tag` (#116). `tag` is the clearest case of a name that
survives translation while the concept does not — Visio has no `Tags` collection on `Document`,
`Page` or `Shape`, and agent-owned key/value metadata already has two Visio-native homes: user cells
(`User.*`, via `cell`) and Shape Data (`Prop.*`, via four `shape` actions).

<!-- BEGIN:LEGACY-DOMAIN-CLASSIFICATION -->

| Domain | Disposition | Owner | Visio evidence | Tracking |
|---|---|---|---|---|
| `vba` | Port | @trsdn | `Document.VBProject` and `Application.VBE` present; Visio supports VBA and `.vsdm` | [#66](https://github.com/trsdn/mcp-server-visio/issues/66) |

<!-- END:LEGACY-DOMAIN-CLASSIFICATION -->

`LegacyDomainClassificationTests` asserts that this table lists **exactly** the domains carrying
`[McpTool(PublicSurface = false)]`. Suppressing a domain without recording a disposition fails the
build, and so does leaving a row behind after a domain is deleted or published. A disposition table
that drifts is worse than none, because it is trusted.

### Themes and connectors

Two cross-cutting Visio-native areas are tracked outside the table because they are additive rather
than inherited. This table understated the position badly enough to invite duplicate work (#38), so
each row now names the actions that exist:

| Area | Disposition | Notes |
|---|---|---|
| Connectors and routing | **Shipped** | `shape`: `add-connector`, `connect-shapes`, `list-connectors`, `read-connector`, `list-connections`, `disconnect-connector`, `reconnect-connector`. `page`: `set-route-style`, `set-connector-routing-extension`, `set-line-jump-code`, `set-line-jump-style`, `set-walk-preference`, `set-place-style` ([#32](https://github.com/trsdn/mcp-server-visio/issues/32)) |
| Connection points | **Shipped** | `shape`: `list-connection-points`, `add-connection-point`, `set-connection-point`, `delete-connection-point` |
| Groups and selection | **Shipped** | `shape`: `group`, `ungroup`, `list-groups`, `read-group`, `select-shapes`, `add-to-selection`, `remove-from-selection`, `clear-selection`, `list-selection` |
| Shape Data | **Shipped** | `shape`: `list-properties`, `get-property`, `set-property`, `delete-property`, over `Prop.*` rows |
| ShapeSheet section and row access | **Shipped** | `cell`: `list-sections`, `list-rows`, `add-row`, `delete-row`, `read-src`, `write-src`, addressing shape, page or document via `sheet_target` ([#33](https://github.com/trsdn/mcp-server-visio/issues/33)) |
| Styles | **Shipped** | `style`: `list`, `read`, `create`, `rename`, `delete`, `read-formula`, `set-formula`, `apply` ([#36](https://github.com/trsdn/mcp-server-visio/issues/36)) |
| Themes | Not implemented | `Document.Theme` does not exist; themes are `DocumentSheet` cells (`ThemeIndex`, `VariationColorIndex`) and are reachable today through `cell` with `sheet_target='document'`, but there is no dedicated action |
| Containers, lists and callouts | **Shipped** | `container`: `list`, `read`, `drop`, `drop-list`, `add-member`, `remove-member`, `list-members`, `containers-of`, `fit-to-contents`, `insert-list-member`, `drop-callout`, `list-callouts`, `read-callout`, `callouts-of` ([#123](https://github.com/trsdn/mcp-server-visio/issues/123)) |
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
