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

Twenty-six command domains inherited from the PowerPoint ancestor are compiled but suppressed from
the public surface via `[McpTool(..., PublicSurface = false)]` — roughly 7,700 lines and 132 actions.

Every row below carries a disposition backed by **an actual probe of a live Visio 16.0 instance**,
not by inference from the name. That distinction matters: `Document.Theme` and `Document.Sections`
both sound like safe ports and neither exists, while `GlowSize` and `Page.Comments` both sound like
PowerPoint-only concepts and both do.

- **Port** — a real Visio equivalent exists; reimplement in place and republish
- **Remap** — the concept exists under a different name and belongs in an already-public tool
- **Delete** — probed and absent; no Visio analogue

<!-- BEGIN:LEGACY-DOMAIN-CLASSIFICATION -->

| Domain | Disposition | Owner | Visio evidence | Tracking |
|---|---|---|---|---|
| `comment` | Port | @trsdn | `Page/Shape/Document.Comments` with `Add`, `Item`, `DeleteAll`; verified by adding, editing and deleting a comment | [#62](https://github.com/trsdn/mcp-server-visio/issues/62) |
| `headerfooter` | Port | @trsdn | `Document.HeaderLeft/Center/Right`, `FooterLeft/Center/Right`, `HeaderFooterFont`, `HeaderMargin` all present | [#63](https://github.com/trsdn/mcp-server-visio/issues/63) |
| `image` | Port | @trsdn | `Page.Import` returned a shape with `Type=4` (`visTypeForeignObject`); `Shape.Export` wrote it back to disk | [#64](https://github.com/trsdn/mcp-server-visio/issues/64) |
| `printoptions` | Port | @trsdn | `Document.Print`, `PrintOut`, `ExportAsFixedFormat`, `PrintLandscape`, `PrintCenteredH`, `PaperSize` present | [#65](https://github.com/trsdn/mcp-server-visio/issues/65) |
| `vba` | Port | @trsdn | `Document.VBProject` and `Application.VBE` present; Visio supports VBA and `.vsdm` | [#66](https://github.com/trsdn/mcp-server-visio/issues/66) |
| `hyperlink` | Port | @trsdn | `Shape.Hyperlinks` present; suppressed in #19 only because the implementation was PowerPoint's | [#35](https://github.com/trsdn/mcp-server-visio/issues/35) |
| `master` | Port | @trsdn | `Document.Masters` present; suppressed in #19 for the same reason | [#34](https://github.com/trsdn/mcp-server-visio/issues/34) |
| `accessibility` | Remap | @trsdn | No `Shape.AlternateText`; Visio alt text is the `Comment` ShapeSheet cell, already shipped as `shape(set-alt-text)` / `shape(read-alt-text)` | [#20](https://github.com/trsdn/mcp-server-visio/issues/20) |
| `background` | Remap | @trsdn | `Page.Background` and `Page.BackPage` present; belongs on the public `page` tool | [#67](https://github.com/trsdn/mcp-server-visio/issues/67) |
| `pagesetup` | Remap | @trsdn | All settings are `PageSheet` cells: `PageWidth`, `PageHeight`, `PrintPageOrientation`, `PageScale`, `DrawingScale`, `PageLeftMargin`, `PaperKind`, `CenterX` | [#67](https://github.com/trsdn/mcp-server-visio/issues/67) |
| `design` | Remap | @trsdn | **`Document.Theme` does not exist.** `Document.Styles` does (6 built-ins); themes are `DocumentSheet` cells `ThemeIndex`, `VariationColorIndex`, `VariationStyleIndex` | [#36](https://github.com/trsdn/mcp-server-visio/issues/36) |
| `tag` | Remap | @trsdn | `Shape.Data1/2/3` present; Shape Data (`Prop.*`) and user cells (`User.*`) both accept named rows | [#33](https://github.com/trsdn/mcp-server-visio/issues/33) |
| `animation` | Delete | @trsdn | `Shape.AnimationSettings` missing | #22 |
| `chart` | Delete | @trsdn | `Document.Charts` missing; Visio charts are embedded OLE with no automatable object model | #22 |
| `customshow` | Delete | @trsdn | No analogue; the concept presupposes an ordered slide show | #22 |
| `media` | Delete | @trsdn | No audio or video object model; media can only arrive as an OLE foreign object | #22 |
| `notes` | Delete | @trsdn | `Page.NotesPage` missing | #22 |
| `placeholder` | Delete | @trsdn | Visio pages have no layout inheritance, so there is nothing to place or audit | #22 |
| `proofing` | Delete | @trsdn | `Document.SpellCheck`, `Document.LanguageSettings` and `Application.CustomDictionaries` all missing; only `Document.Language` and the `Char.LangID` cell survive, and those belong to `cell` | #22 |
| `section` | Delete | @trsdn | `Document.Sections` missing. **Not** the ShapeSheet section of #33 — same word, unrelated concept | #22 |
| `slide` | Delete | @trsdn | Superseded by the public `page` tool | #22 |
| `slideimport` | Delete | @trsdn | Slide-import semantics have no page analogue; `Page.Paste` already covers what is meaningful | #22 |
| `slideshow` | Delete | @trsdn | `Document.SlideShowSettings` missing | #22 |
| `slidetable` | Delete | @trsdn | Visio has no table object; tables are drawn as grouped shapes | #22 |
| `smartart` | Delete | @trsdn | `Page.SmartArt` missing | #22 |
| `transition` | Delete | @trsdn | `Page.Transition` missing | #22 |

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
