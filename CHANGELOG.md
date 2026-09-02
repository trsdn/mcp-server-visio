# Changelog

All notable changes to VisioMcp will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- **Every operation is now atomic and undoable in one step** (#36a). `VisioBatch.Execute` wraps each
  operation in a Visio undo scope: `EndUndoScope(id, commit: true)` on success,
  **`commit: false` on failure — which reverts the writes already made**.
  Previously an operation that wrote several cells and then failed left the document half-edited,
  with no way for the caller to know which writes had landed. A test now proves the rollback:
  two cells written, an exception thrown, both values back to their originals.
  It also groups: everything a command writes becomes a single entry in Visio's undo stack, so a
  command that touches five cells is one Ctrl+Z for a user watching in visible mode rather than
  five. Verified directly — after two cells were written inside one committed scope, a single
  `Undo()` reverted both.
  Cost is ~1 ms per scope, negligible beside the COM calls it wraps. If Visio refuses to open a
  scope the operation still runs: losing atomicity is better than refusing to work.

### Added

- **Connection point CRUD** (#32). Four new `shape` actions — `list-connection-points`,
  `add-connection-point`, `set-connection-point`, `delete-connection-point`. The tool already had
  *connectors* (the lines between shapes) and nothing for the *anchors those connectors glue to*;
  the descriptions now distinguish them, since the two are easily confused.
  Positions are ShapeSheet **formulas**, not co-ordinates: `Width*0.5` keeps a point centred when
  the shape is resized, which is the entire reason these cells accept expressions. A test proves it
  by widening a shape and asserting the point moved.
  A named point becomes the glue target `Connections.<name>`, verified by gluing a connector to it
  and reading back `PAR(PNT(Sheet.1!Connections.Top.X,…))`.
  Built as a typed wrapper over #33's section/row primitives rather than new plumbing.

### Fixed

- **A single-character parameter name takes down the entire CLI** (#32). Adding `x` and `y` to the
  shape tool generated `--x`/`--y`, and Spectre.Console throws *"Long option names must consist of
  more than one character"* while building the command tree — so **every** CLI command stopped
  working, not just `shape`. The pre-commit smoke test caught it, but only as a JSON parse error on
  `session create` that named neither the parameter nor the tool.
  `ParameterNameConstraintTests` now rejects single-character parameter names across the public
  surface and explains why.

### Added

- **Generic ShapeSheet section and row access** (#33). Six new `cell` actions — `list-sections`,
  `list-rows`, `add-row`, `delete-row`, `read-src`, `write-src`. A caller could already read
  `Prop.Cost` by name but could not **create** it, and could not reach a row with no name at all.
  `section` accepts a name (`Prop`, `User`, `Connections`, `Actions`, `Hyperlink`, `Geometry1`,
  `Char`, `Para`…) or a numeric index, and an unknown name is rejected by listing the valid ones.
  `read-src`/`write-src` address a cell by section, row and column, which is the only way to reach
  a positional row such as a connection point — so #32 becomes a typed wrapper over this rather
  than new plumbing, and #36b falls out because `PageSheet` and `DocumentSheet` expose the same API.

  The issue's premise was wrong: there is **no allow-list**. `CellsU[name]` already accepted any
  cell name; the gap was that the section and row primitives were private and duplicated —
  `VisSectionProp = 243` declared separately in `ShapeCommands` and `DocumentPropertyCommands`,
  `LayerCommands` carrying its own copy, and `ShapeCommands` writing `AddRow(1, 20, 0)` with no
  constant at all. `ShapeSheetSections` now holds one authoritative map, derived by adding a named
  row to each candidate index on a live Visio instance and reading the resulting cell name back.
  That mattered: the widely-repeated mapping of Actions to 238 and Character to 4 is wrong — they
  are **240** and **3**.

### Fixed

- **Runtime error messages named the wrong product** (#76). #23 covered tool descriptions and #37
  covered parameter descriptions; neither reaches the strings returned when something *fails*.
  Seven `ServiceResponse.ErrorMessage` sites said *"PowerPoint process for session 'X' has died"*,
  and `FileAccessValidator` told a user whose `.vsdx` was locked to close **PowerPoint** — which
  was not running, while Visio held the lock.
  This surface arguably matters more than the descriptions: a description is read once while the
  agent has full context, an error is read when the operation has already failed and the wrong
  product name sends recovery in the wrong direction.
  Also renamed `ResiliencePipelines.CreatePowerPointQuitPipeline` (a public method) and removed a
  *"Presentations with active Power Pivot models"* comment — Power Pivot is Excel.
  `RuntimeMessageTerminologyTests` scans the runtime layers' string literals, with an explicit
  allow-list for the messages that name PowerPoint on purpose to explain a missing equivalent.

### Fixed

- **Every MCP parameter is now documented** (#37b). The remaining 35 placeholders were parameters
  with no `<param>` doc anywhere: `layer`, `page`, `stencil` and `window` had **none at all**.
  Written from the implementations rather than guessed — `add-guide`'s `guideType` is validated in
  code as 1/2/3, snap strengths are validated as 1–999, and the page routing setters name the
  ShapeSheet cell they write (`RouteStyle`, `ConLineRouteExt`, `LineJumpCode`) rather than invent
  enum values that could not be verified.
  **Placeholder descriptions: 158 of 158 → 0**, and a guard test now fails the build on any new one.

- **The hand-written `file` tool had no parameter descriptions at all** (#37b). It is the entry
  point for every workflow and is not generated, so nothing supplied them: `path`, `session_id`,
  `save`, `show` and `timeout_seconds` reached the LLM as bare names. `save` now states that edits
  are lost when it is false.

- **The MCP schema documented parameters far worse than the CLI did** (#37). `ServiceRegistryGenerator`
  runs inside the Core compilation and can read XML `<param>` docs; `McpToolGenerator` runs in the
  MCP server compilation where Core is a **metadata reference**, and XML documentation is not
  carried in metadata. So `shape(find-by-type)`'s `shape_type` read *"Visio VisShapeTypes integer:
  1=Page, 2=Group…"* in the CLI skill and *"(required for: find-by-type)"* in the MCP schema — the
  same parameter, the same source doc, and the **LLM-facing** surface got the worse one.
  Core's generator now emits those descriptions as `ServiceRegistry.{Category}.ParameterDocs`
  constants, which *are* carried in metadata — the same reason `[McpTool(Description = "...")]`
  always worked — and the MCP generator reads them back.
  **Placeholder parameter descriptions fell from 115 of 151 (76%) to 35 of 167 (21%).**

- **Parameter descriptions were taken from the first declaring action, not the first documented
  one** (#37). `shapeName` carries a `<param>` doc on 26 methods, but `read` declares it first
  without one, so it rendered as a bare `(required for: …)` on **both** surfaces. Aggregation now
  takes the first non-empty description.

- **`cell` had no parameter documentation at all** (#37). Its five actions are the ShapeSheet
  surface, where the non-obvious rules matter most: distance cells need explicit units (`"3 in"`,
  `"12 pt"`) or a bare number is read as inches, text-valued cells such as `Comment` evaluate to
  `0` and must be read with `read-formula`, and a formula recalculates where a literal does not.
  All of that is now in the schema an agent reads.

### Removed

- **The Excel-era parameter description table** (#37). `StringHelper.GetParameterDescription` held
  eighteen keys inherited from the Excel ancestor — `queryName`, `mCode`, `rangeAddress`,
  `pivotTableName`, `slicerName`, and the three-product artefact `"sheetName" => "Slide name"`.
  Exactly one (`formula`) matched a parameter that exists in this product, so seventeen were dead
  and every real lookup already fell through to `ToPascalCase`. Deleting it changed no output.

### Security

- **Scriban upgraded 6.6.0 → 7.2.6** (#13): 6.6.0 carried one critical and four moderate advisories
  (GHSA-5wr9-m6jw-xx44, GHSA-6q7j-xr26-3h2c, GHSA-m2p3-hwv5-xpqw, GHSA-q6rr-fm2g-g5x8,
  GHSA-xw6w-9jjh-p9cr). With `TreatWarningsAsErrors`, `NuGetAudit` turned these into five hard build
  errors, so `main` did not build at all.
- **Microsoft.Build.Framework / Microsoft.Build.Utilities.Core upgraded 17.14.8 → 17.14.28** and the
  blanket `<NoWarn>NU1903</NoWarn>` removed from `VisioMcp.Build.Tasks` (#13). The suppression was
  masking CVE-2025-55247 (GHSA-w3q9-fxm7-j8fq); the advisory is now resolved rather than hidden, so
  the project no longer suppresses any dependency audit warnings.

### Removed

- **Three unused PowerPoint save-format constants** (#23). `ComInteropConstants` defined
  `PpSaveAsOpenXMLPresentation = 24`, `PpSaveAsOpenXMLPresentationMacroEnabled = 25` and
  `PpSaveAsDefault = 11` — PowerPoint `PpSaveAsFileType` codes, referenced from nowhere in the
  tree. `PowerPointQuitTimeout` and `VisioShutdownService.SavePresentationWithTimeout`, which are
  live, are renamed to `VisioQuitTimeout` and `SaveDocumentWithTimeout` so the corrected
  instructions do not document PowerPoint-named APIs.

- **The `VisioContext` and `IVisioBatch` PowerPoint aliases are gone** (#21). Both types exposed
  every member twice — `Presentation`/`Document`, `PresentationPath`/`DocumentPath`, `App`/
  `Application`, plus `Presentations` and `GetPresentation` on the batch. Because the properties
  are `dynamic`, `ctx.Presentation.Slides` compiled cleanly and failed only when executed, so the
  compiler could not help with the migration in progress. `IVisioBatch` is the first parameter of
  every Core command, so leaving it aliased would have defeated the change; all of its aliases had
  zero external callers. `VisioContextTests` now asserts by reflection that neither type exposes a
  PowerPoint-named member.
  Notably, the issue's step 3 — *"any code that then fails to compile is PowerPoint-era code
  needing migration"* — produced **nothing**: zero compile errors across 18 renamed files. #20 had
  already made `shape` and `text` Visio-native and #22 had deleted the 14 dead domains, so the
  inventory this step was designed to generate had already been worked through.

- **The 14 legacy domains with no Visio analogue are deleted** (#22). `animation`, `chart`,
  `customshow`, `media`, `notes`, `placeholder`, `proofing`, `section`, `slide`, `slideimport`,
  `slideshow`, `slidetable`, `smartart` and `transition` — **4,768 lines and 82 actions** — each
  probed against a live Visio instance and confirmed to have no equivalent COM surface
  (`Shape.AnimationSettings`, `Document.Charts`, `Page.NotesPage`, `Page.SmartArt`,
  `Page.Transition`, `Document.SlideShowSettings` and `Document.Sections` are all absent).
  Ten of them were still routed by `VisioMcpService`, so an internal caller could reach a
  PowerPoint code path against a `.vsdx`. The suppressed-domain count drops from 26 to 12 and the
  Core interface-method count from 281 to 199.

### Changed

- **Contributor instructions no longer describe a PowerPoint product** (#23). `.github/`
  carried **282 PowerPoint references**, opening with *"VisioMcp is a Windows-only toolset for
  programmatic PowerPoint automation via COM interop"*. As #23 put it, every agent contributing to
  this repository was told it was building a PowerPoint tool — a plausible reason the legacy
  persisted through several rounds of migration work. Reduced to **one**, which describes migration
  history and is exempted explicitly.
  Much of it was not merely terminology but **wrong**: instructions documented `IPptBatch`,
  `PptSession.BeginBatch`, `ctx.Presentation`, `PptToolsBase` and `PptShutdownService` — none of
  which exist — and gave `--filter "Feature=Slide"`, `Feature=VBA` and `Feature=VBATrust` as test
  commands, none of which match a single test. `ppt-com-interop.instructions.md` and
  `ppt-com-patterns-guide.instructions.md` are renamed to `visio-*`.
  `ContributorInstructionTerminologyTests` guards the result; verified it fails on an induced
  regression.

- **The shipped skill packages no longer install PowerPoint guidance** (#23). Both
  `skills/visio-cli/references/` and `skills/visio-mcp/references/` are installed verbatim by
  `npx skills add`, and nothing checked their contents.
  `slide-design-review.md` was **9.5 KB of PowerPoint deck advice** — the Title Story Test, deck
  length targets, *"Would an executive spend more than 3 seconds understanding any slide?"* —
  replaced by `diagram-design-review.md`, a Visio self-review covering connector glue, label
  clipping, grid drift and shape vocabulary. `ppt_agent_mode.md` and `slide-design-principles.md`
  had already been rewritten for Visio but kept their PowerPoint filenames; renamed to
  `visible-session-mode.md` and `diagram-design-principles.md`.
  `cli-commands.md` was deleted: it claimed *"Auto-generated from `visiocli --help`. Do not edit
  manually"* but had been committed by hand and listed `powerquery`, `pivottable`, `range`,
  `slicer`, `worksheetstyle` and `slide` — an **Excel-era** command list, one migration further
  back than the rest of the debt, in a shipped Visio skill.
  `SkillReferenceQualityTests` now guards the reference folders, which only `SKILL.md` was covered
  for before.

### Fixed

- **The CLI skill documented two parameters that do not exist, and taught the wrong format for the
  one that does** (#23). It instructed agents to pass `--values '{"text": "Hello"}'` and
  `--selected-items '["Slide 1","Slide 3"]'`; neither parameter exists anywhere in the source. Its
  "List Parameters Use JSON Arrays" section then declared comma-separated values *"WRONG: not
  valid"* — but `--shape-names`, the only real list parameter, **is** comma-separated, so an agent
  following the shipped skill would format the one working case incorrectly.

- **The `shape` tool advertised twelve shape types it cannot draw** (#23). Its MCP description
  listed `auto_shape_type (MsoAutoShapeType): 1=Rectangle, 5=Triangle, 9=Oval, 10=Hexagon,
  13=Pentagon, 16=Cube, 23=RoundedRectangle, 55=Chevron, 61=RightArrow, 92=Heart, 106=Plus,
  127=Callout`. The implementation is `9 => DrawOval, _ => DrawRectangle`, so eleven of the twelve
  silently produced a rectangle **and returned success naming the type the agent asked for**. The
  description now states the two primitives honestly and points at `stencil(drop-master)`, which is
  how Visio actually provides richer shapes.

- **PowerPoint terminology removed from every LLM- and user-facing string** (#23). The entry-point
  `file` tool described itself as *"File management commands for PowerPoint presentations"* and its
  `filePath` parameter as *"Path to the .pptx or .pptm file"*; `window` minimised *"the PowerPoint
  window"*; the CLI's `session create` help read *"Create a new PowerPoint file"*; the tray UI's
  `AccessibleDescription` and `AccessibleName` — read aloud by screen readers — said *"PowerPoint
  automation for coding agents"*; and `list-actions` shipped a copy-paste example combining a
  `.pptx` file with **Excel** range syntax (`range set-values --range A1`).
  `McpDescriptionTerminologyTests` now reads the 141 descriptions the MCP SDK actually registers,
  tool-level and per-parameter, and fails the build on PowerPoint terminology. `FEATURES.md` has
  carried this rule since before the migration began; prose could not enforce it.

- **The `text` tool's legacy note was out of date** (#23). It claimed `insert-datetime` and
  `insert-slide-number` were *"presentation-era carryovers and not Visio-native"*, but #61
  reimplemented both — they append literal text. The note now distinguishes the two actions that
  genuinely throw (`empty-placeholder-audit`, `insert-link`) from the two that work with a caveat.

- **The MCP server suggested a `.pptx` path to agents** (#21). When a caller passed a path that was
  not fully qualified, `VisioToolsBase.ValidateWindowsPath` built a corrected suggestion ending in
  `presentation.pptx` — so a Visio server told the LLM to retry with a PowerPoint file, which
  Visio cannot open. Now `drawing.vsdx`.

- **`shape(find-by-type)` documented the wrong constants** (#22, introduced in #20). The parameter
  description shipped into both SKILL.md and the MCP schema read *"MsoShapeType integer
  (1=AutoShape, 6=Group, 13=Picture, 14=Placeholder, 17=TextBox, etc.)"* while the implementation
  compared against Visio's `VisShapeTypes`. Every documented value overlapped a real Visio value
  with a different meaning, so an agent following the schema matched nothing — silently, because
  the action returns "no shapes found" rather than an error.

- **`ShapeHelpers.GetShapeTypeName` reported wrong shape-type names** (#22). It mapped
  `MsoShapeType`, so a Visio rectangle (`Type = 3`) was named **"Chart"**, a group (`2`)
  **"Callout"**, and an imported image (`4`) **"Comment"**. Replaced by `VisioShapeTypes`, one
  authoritative mapping confirmed against a live instance, now shared by `ShapeCommands`,
  `MasterCommands` and `AccessibilityCommands`. `ShapeHelpersTests` verified the PowerPoint table
  was reproduced faithfully — it was, and it was the wrong table; `VisioShapeTypesTests` replaces
  it and asserts the three previously misreported values by name.

- **`shape(add-shape)` promised auto-shapes it does not draw** (#22). The description advertised
  *"MsoAutoShapeType integer (1=Rectangle, 9=Oval, etc.)"*, but the implementation only
  distinguishes `9` (ellipse); every other value silently produces a rectangle **and reports
  success naming the requested type**. The description now states exactly what is supported and
  points at stencil masters for richer shapes.

### Added

- **Every suppressed legacy domain now carries a disposition, an owner and Visio evidence** (#22).
  26 command domains (~7,700 LOC, 132 actions) were compiled but hidden behind
  `[McpTool(PublicSurface = false)]` with no recorded decision — documented only as eight vague
  thematic rows in `FEATURES.md`. Each domain was **probed against a live Visio 16.0 instance** and
  classified **Port** (7), **Remap** (5) or **Delete** (14), with the specific COM member that does
  or does not exist recorded per row. Follow-ups filed for the Port and Remap sets:
  #62 `comment`, #63 `headerfooter`, #64 `image`, #65 `printoptions`, #66 `vba`,
  #67 `background`+`pagesetup`.
  Probing overturned two guesses the issue itself made: `Document.Sections` **does not exist**, so
  `section` is a delete rather than the port it was assumed to be (its name collides with the
  unrelated ShapeSheet sections of #33), and `Page.Comments.Add` **does** exist and works, making
  `comment` a complete port. `Document.Theme` also does not exist — Visio themes are `DocumentSheet`
  cells — which corrects the premise of the styles/themes work in #36.
  `LegacyDomainClassificationTests` makes the table an enforced invariant rather than prose: it
  asserts the table lists **exactly** the domains suppressed in code, that every disposition is one
  of the three valid verdicts, and that every row names an owner, evidence and a tracking reference.
  Verified against three induced drift modes — a missing row, a stale row, and an invalid
  disposition with a `TBD` owner — each of which fails the build.

- **CI now runs the test suite** (#14). No registered workflow executed `dotnet test` at all: the
  only real invocation lived in `integration-tests.yml`, which was both unregistered (#12) and
  gated off by default, so the only job that could run was a stub that **reported a green check
  having executed zero tests**. The 13 public tools had no automated regression protection.
  A `unit-tests` job in `build-cli.yml` now runs `dotnet test --filter "Category!=Integration"` —
  **646 tests** — on `windows-latest`, needing neither Visio nor a self-hosted runner, and uploads
  per-project `.trx` results. Verified that a red test makes the command exit 1.
  The false-green stub is deleted: with it gone, the integration job reports as **skipped** when the
  gate variable is unset, which cannot be mistaken for a pass.
  `integration-tests.yml` is renamed *Visio Integration Tests*, its gate variable
  `ENABLE_POWERPOINT_INTEGRATION_CI` → `ENABLE_VISIO_INTEGRATION_CI`, its job
  `powerpoint-integration` → `visio-integration`, and its runner label `powerpoint` → `visio`.
  `build-cli.yml`'s *"Test CLI (requires PowerPoint)"* step — which only printed a note telling the
  developer to run tests locally — is removed, since the real job now exists.

- **Git hooks are now installable in one command** (#17). `.git/hooks` contained only the stock
  `*.sample` files, so despite being documented, **no check ran on commit anywhere** — which,
  combined with the failing gates (#16) and the dormant workflows (#12), meant nothing ran on
  commit, on push, or in CI.
  A committed `.githooks/pre-commit` plus `scripts/Install-GitHooks.ps1` replaces the manual
  `Copy-Item scripts\pre-commit.ps1 .git\hooks\pre-commit`. Because the hook is version-controlled
  and reached via `core.hooksPath`, a clone that bootstraps once also picks up later changes to the
  hook — a copied file would not.
  Verified end to end: the hook fires on commit, runs every gate, and **blocks** a commit carrying
  an induced COM leak (naming the offending file), while a clean tree passes.

### Fixed

- **All seven committed workflows now carry `workflow_dispatch`, and `build-mcp-server.yml` runs on
  pull requests** (#12). Five workflows were committed but never registered with GitHub Actions, so
  they had never run and never would; `gh run list --workflow build-cli.yml` returned
  `HTTP 404: workflow not found on the default branch`.
  `build-cli.yml`, `build-mcp-server.yml` and `dependency-review.yml` had no manual trigger, so
  registration could not be forced or verified. All three now have one.
  Separately, `build-mcp-server.yml` had **no `pull_request` trigger at all** — only `push` to
  `main`/`develop` — so the MCP Server build was never validated on a PR, only after merge. It now
  mirrors `build-cli.yml`'s pull-request trigger.
  Four of the five have since registered (`build-cli`, `dependency-review`, `integration-tests`
  plus the already-active `codeql`/`stale`) as branches touching their path filters were pushed;
  `build-mcp-server` and `release` register once this merges to `main`.

- **`scripts/pre-commit.ps1` now exits 0 on a clean checkout** (#16). Six of the documented gates
  failed, and the gate inventory itself was wrong in both directions.
  - `check-com-leaks.ps1` excluded `PptBatch.cs|PptSession.cs` — files that do not exist — so the
    legitimate session file `VisioBatch.cs` was reported as a leak. Now excludes
    `VisioBatch.cs|VisioSession.cs`; **1 leak → 0**.
  - `check-cli-settings-usage.ps1` matched `class Settings ... (.+)$` under `Singleline`, which
    swallowed the rest of the file. In `BatchCommand.cs` that pulled in the `BatchEntry` and
    `BatchResult` DTOs and reported their properties as unused settings. Now brace-matches the real
    class body. Its exclusion lists (`PivotTable`, `Slicer`, `DaxQuery`, `MCodeFile`, `SheetScope`)
    were Excel-era and are deleted.
  - `Test-CliWorkflow.ps1` created a `.pptx` and drove the suppressed `slide` domain. Rewritten
    against `page` / `--page-index` on a `.vsdx`; **11/11 steps pass**, including save-and-reopen
    round-trip.
  - `Stop-VisioMcpProcesses.ps1` killed `POWERPNT` and never `VISIO`, so Visio processes leaked
    after every COM test run — 28 orphans were observed during this work. Now kills `VISIO`.
    `pre-commit.ps1` had the same defect in its own cleanup loop.
  - CI now runs the Visio-independent gates via a `quality-gates` job, so they cannot silently rot
    again. `build-cli.yml` and `build-mcp-server.yml` also now build the whole solution before the
    coverage audit, which reads generated output from both `Core` and `McpServer`.

- **`audit-core-coverage.ps1` no longer reports "100% coverage" on zero discovered methods** (#15).
  Pre-commit gate #3 parsed a hand-written `ToolActions.cs` / `ActionExtensions.cs` model that no
  longer exists, so it found **0 methods and 0 enum values**, printed *"No gaps detected — 100%
  coverage maintained!"* and exited **0**. A gate that reports success on an empty dataset is worse
  than no gate, because it manufactures confidence.
  Rewritten against the real source of truth — `[ServiceCategory]` / `[McpTool(PublicSurface)]`
  attributes compared against the generated `ServiceRegistry.*.Dispatch.g.cs` and
  `McpTool.*.g.cs`. It now discovers **37 categories (13 public, 24 suppressed) and 281 interface
  methods**, and detects three classes of gap: an action missing from dispatch, a public category
  with no MCP tool, and a suppressed category that leaked onto the public surface.
  **Discovery returning nothing is now a hard failure**, including when the tree has not been built.
  Verified in all three states: green on real data, red on an induced gap, red on empty discovery.
  Hand-written tools (`VisioFileTool`) are recognised so `file` is not a false positive.
  Callers updated: `pre-commit.ps1` (the `-CheckNaming` switch no longer exists — action names are
  now derived from method names by the generator, so they cannot drift) and the usage docs in
  `.github/instructions/coverage-prevention-strategy.instructions.md`, whose sample output
  described tables and PivotTables from the Excel ancestor.

- **All 14 OnDemand session/batch tests now pass** (#25). They created `.pptx` files, which
  `SessionManager` correctly rejects, so the suite that Rule 3 makes mandatory before touching
  session or batch code had **zero** working coverage of STA threading, COM lifetime, timeout
  handling, message pumping and disposal.
  `SessionManagerTimeoutTests`, `VisioBatchTimeoutTests` and `VisioBatchMessagePumpTests` are
  migrated to a new `batch-test-static.vsdx` template, `ctx.Document.Pages` instead of
  `ctx.Presentation.Slides`, and `VISIO` instead of `POWERPNT` process assertions. Every assertion
  is preserved; none was weakened to make a test pass.
  `dotnet test tests/VisioMcp.ComInterop.Tests --filter "RunType=OnDemand"`: **0 passed / 14 failed
  → 14 passed / 0 failed**.

- **`Close_NoOperationsRunning_ClosesSuccessfully` was failing on a stale `.pptx` path, not a
  response-contract mismatch** (#28). The test created `CloseTest_<guid>.pptx`; `file create`
  correctly rejects a non-Visio extension and returns an error object with no `session_id`, so the
  *next* line threw `KeyNotFoundException`. The reported symptom pointed at the `file close`
  contract, but the failure was on the `file create` response and the contract was never wrong.
  Extension corrected to `.vsdx` — the last stale PowerPoint path in this file.
  Property reads now go through a helper that reports the tool, the missing property, the properties
  that *are* present and the raw response, so genuine contract drift fails legibly instead of as a
  bare `The given key was not present in the dictionary`.
  `VisioMcp.McpServer.Tests --filter "Category=Integration"`: **41 passed / 1 failed → 42 passed /
  0 failed**. That suite started this release cycle at 17 passed / 25 failed.

- **Shape Data parameters now have real descriptions in the generated CLI skill** (#29).
  `--property-name` and `--property-value` shipped with empty description cells in
  `skills/visio-cli/SKILL.md`, failing `SkillMdQualityTests.CliSkill_HasNoEmptyParameterDescriptions`.
  Added `<param>` XML docs to `ListProperties`, `GetProperty`, `SetProperty` and `DeleteProperty` on
  `IShapeCommands`. `propertyName` was also declared `string? = null` while every implementation
  calls `ArgumentException.ThrowIfNullOrWhiteSpace` on it, so the generated surface described a
  required parameter as optional; it is now non-nullable and the generators emit
  `(required for: get-property, set-property, delete-property)`.
  Whole non-integration suite: **645 passed / 1 failed → 646 passed / 0 failed**.

- **`OleMessageFilterTests` PENDINGMSG constants were swapped** (#27). The test declared
  `PENDINGMSG_WAITDEFPROCESS = 1` and `PENDINGMSG_WAITNOPROCESS = 2`; the Win32 values
  (`objidl.h`, `tagPENDINGMSG`) are the reverse — `CANCELCALL = 0`, `WAITNOPROCESS = 1`,
  `WAITDEFPROCESS = 2`. Its two assertions therefore contradicted each other, requiring the return
  value to be `WAITDEFPROCESS` while simultaneously forbidding `2`. `OleMessageFilter` itself was
  always correct.
  Its rationale also described `FormatConditions.Add()`, `Calculate` and `SheetChange` — **Excel**
  APIs attributed to PowerPoint, carried over from the `mcp-server-excel` ancestor, describing a
  scenario that cannot occur in Visio. Replaced with the contract the filter actually enforces
  (inbound calls must be dispatched so `HandleInComingCall` can accept with `SERVERCALL_ISHANDLED`
  or reject with `SERVERCALL_RETRYLATER`). No unverified Visio deadlock is claimed in its place.
  The same wrong-product rationale in `OleMessageFilter.cs` itself is corrected too.
  `VisioMcp.ComInterop.Tests`: **23 passed / 1 failed → 24 passed / 0 failed**.

- **The MCP smoke-test gate now derives its expected tool list from the assembly** (#26).
  `ExpectedToolNames` was a hand-maintained 12-entry allow-list that omitted `layer`, so
  `SmokeTest_AllTools_E2EWorkflow` and `ListTools_CanIterateAllTools` — the tests
  `integration-tests.yml` invokes by name as the designated CI gate — had been red since `layer`
  was added, treating a fully working Visio-native tool as an intruder. The set is now reflected
  from `[McpServerToolType]`/`[McpServerTool]`, the same source `WithToolsFromAssembly()` uses, so
  adding a public tool cannot silently break the gate again. Discovery asserts a non-empty result
  and the presence of known anchor tools, so it cannot vacuously pass on zero data the way
  `audit-core-coverage.ps1` does (#15). The hardcoded `HiddenLegacyToolNames` leak check is
  deliberately left independent.
  `VisioMcp.McpServer.Tests --filter "Category=Integration"`: **3 failed / 39 passed → 1 failed /
  41 passed**. The last failure is #28.

- **`xunit.runner.json` now reaches every test project's output directory** (#24). xunit reads this
  file from `bin/`, and silently ignores the settings when it is absent — no warning, no error. Only
  `VisioMcp.ComInterop.Tests` set `CopyToOutputDirectory`, so the other projects ran test collections
  in parallel against a single Visio COM instance on one STA thread. Measured on
  `VisioMcp.McpServer.Tests --filter "Category=Integration"`: **25 failed / 17 passed → 3 failed /
  39 passed**. The 22 eliminated failures were entirely phantom. The three that remain are genuine
  and tracked as #26 and #28.
  The four duplicate per-project copies are replaced by one canonical `tests/xunit.runner.json`
  linked from `tests/Directory.Build.props`, so every current and future test project picks it up
  and none can opt out by omission.

- **`main` now builds from a clean clone with no extra flags** (#13). `dotnet build VisioMcp.sln -c Release`
  previously failed with 5 `NU1904`/`NU1902` errors and only succeeded with `-p:NuGetAudit=false`.
  This also unblocks CodeQL (#18) and `release.yml`, both of which run an unflagged `dotnet build`.

### Added

- Official source-side Copilot SDK agent client under `src\VisioMcp.Agent`, including local planner tests and documentation for the agent architecture
- Dedicated documentation for the evaluation framework and the archetype/reference pipeline
- Validated Visio-native MVP across the shared service layer, CLI, and MCP server:
  - document sessions with create/open/save/close flows
  - page list/read/create/rename/delete
  - shape list/read/add basic shapes/add text boxes/move-resize/delete
  - text get/set/find/replace/word-count
  - ShapeSheet cell value and formula read/write paths
  - stencil master enumeration and drop-to-page workflows
- Real visible-mode support so Visio can be shown live during automation
- Regenerated skill and prompt surfaces aligned to the current Visio MVP
- Updated root, CLI, MCP, extension, and installation docs to describe the current Visio-first state more truthfully

### Removed

- **Four obsolete pre-commit scripts deleted** (#16). `check-mcp-core-implementations.ps1`,
  `check-cli-coverage.ps1` and `check-cli-action-coverage.ps1` parsed a hand-written
  `ToolActions.cs` / `ActionExtensions.cs` model that no longer exists; the enum and the CLI
  commands are now generated from the same `[ServiceCategory]` interfaces as the methods, so they
  cannot drift apart by construction. Their surviving intent — that every public domain reaches
  both MCP and CLI, and no suppressed domain leaks — is now asserted by `audit-core-coverage.ps1`
  against the generated `_CliCategories.g.cs`.
  `check-dynamic-casts.ps1` enforced documenting `((dynamic))` casts as *"PIA coverage gaps"* and
  pointed at `docs/PIA-COVERAGE.md`, which does not exist. Its premise was migrating to
  `Microsoft.Office.Interop.PowerPoint` — the wrong product — and its exclusion list named the
  same nonexistent `Ppt*.cs` files.

- **The unused `Microsoft.Office.Interop.PowerPoint` PIA reference removed** (#16) from
  `VisioMcp.Core.csproj` and `Directory.Packages.props`. It was referenced with
  `EmbedInteropTypes=true` and a `NU1701` suppression, but **no code used it** — the only match in
  the tree was a comment. The solution builds with 0 warnings and 0 errors without it.

- **`VisioMcp.Diagnostics.Tests` deleted** (#30). It contained no test files — only a `.csproj` —
  yet sat in the solution, built on every `dotnet build`, and appeared in the path filters of
  `build-cli.yml` and `build-mcp-server.yml` as though it contributed coverage. There is also no
  `src/VisioMcp.Diagnostics` production project for it to test.
  `tests/README.md` documented it as PowerPoint COM research into *"Power Query, Data Model,
  PivotTables"* with a `Feature=PowerQuery` filter — all **Excel** features, inherited from the
  ancestor repo, describing tests that never existed.
  The same README's "Feature-Specific Tests" section listed six filters (`PowerQuery`, `DataModel`,
  `Tables`, `PivotTables`, `Ranges`, `Connections`), none of which match any trait in the
  repository; replaced with the values actually in use.

### Changed

- **`TextCommands` is now entirely PowerPoint-free** (#20). Its 13 broken actions were written
  against `TextFrame.TextRange.Font` and `ParagraphFormat` and threw `RuntimeBinderException` on
  every call. Ten are reimplemented against Visio's Character and Paragraph ShapeSheet sections —
  `format` (`Char.Font`/`Size`/`Color`/`Style`, `Para.HorzAlign`, `VerticalAlign`),
  `format-advanced`, `set-spacing`, `set-bullets`, `read-spacing`, `read-bullets`, `change-case`,
  `insert-symbol`, `insert-date-time` and `insert-slide-number`. `alt-text-audit` now audits the
  `Comment` cell, matching `shape(set-alt-text)`.
  `Char.Style` is a bitfield, so bold/italic/underline are applied by read-modify-write — setting
  one no longer silently clears the others, which a test pins.
  `change-case` deliberately rewrites the stored text rather than setting `Char.Case`, which is a
  display transform that would leave `text(get)` returning the original casing.
  `insert-date-time` and `insert-slide-number` insert **literal text and say so**: Visio's live
  fields cannot be created through a single cell write, so a value that silently fails to track
  page reordering would be worse than an honest message.
  Parameters with no Visio equivalent — `strikethrough`, `subscript`, `superscript`,
  `characterSpacing` — are **reported as ignored** rather than silently dropped.
  `insert-link` and `empty-placeholder-audit` throw `NotSupportedException` naming the alternative:
  Visio attaches hyperlinks to a whole shape rather than a text range (#35), and Visio pages have
  no layout inheritance and therefore no placeholders.
  20 new integration tests.

- **`ShapeCommands` is now entirely PowerPoint-free** (#20). The last ten of its 23 broken actions
  are resolved, so all 51 shape actions execute against a `.vsdx`.
  Eight more are reimplemented against modern-Visio effect cells — `set-glow` (`GlowSize`,
  `GlowColor`), `set-soft-edge` (`SoftEdgesSize`), `set-reflection` (`ReflectionSize` and
  siblings), `set-3d` (`RotationXAngle`/`YAngle`/`ZAngle`, `BevelTopType`, `BevelTopHeight`),
  `set-gradient-fill` (`FillGradientEnabled`), `set-text-frame` (the four margin cells) and
  `copy-formatting` (an explicit fill/line/shadow/margin cell copy, since Visio has no
  PickUp/Apply format painter).
  `set-text-frame` now **reports** that `word_wrap` and `auto_size` have no Visio equivalent
  instead of silently ignoring them — a silent no-op is how an agent comes to believe it changed
  something.
  The two with genuinely no Visio analogue — `set-action-settings` (PowerPoint click actions
  navigate between slides) and `add-text-effect` (WordArt) — now throw `NotSupportedException`
  naming the Visio alternative, rather than an opaque `RuntimeBinderException`.
  27 integration tests in total, covering every reimplemented action.

- **Four more shape actions reimplemented against Visio** (#20), bringing the total to 13 of 23.
  `set-shadow` and `read-shadow` now use `ShdwPattern`/`ShdwForegnd`/`ShdwOffsetX`/`ShdwOffsetY`;
  `set-alt-text` writes the `Comment` cell, quoting and escaping the text as a ShapeSheet string
  formula; `copy-to-slide` uses `Page.Drop` between pages instead of a clipboard round trip, which
  is faster and immune to anything else touching the clipboard mid-operation; `find-by-type`
  compares against Visio's `VisShapeTypes` rather than PowerPoint's `MsoShapeType`.
  **`Shape.Type` values were confirmed against a live instance rather than assumed** — a drawn
  rectangle reports **3** (`visTypeShape`) and a grouped selection **2** (`visTypeGroup`). Callers
  passing the old `MsoShapeType` numbers will now match nothing, which is correct: they previously
  matched nothing *and* threw.
  Six more integration tests, including one pinning quote-escaping in the `Comment` formula.

- **Nine shape formatting actions reimplemented against Visio's ShapeSheet** (#20). `set-fill`,
  `set-line`, `set-rotation`, `flip`, `scale`, `set-opacity`, `lock-aspect-ratio`, `read-fill` and
  `read-line` were written against PowerPoint COM (`Shape.Fill`, `Shape.Line`, `Shape.Rotation`,
  `ScaleWidth`, `LockAspectRatio`) and threw `RuntimeBinderException` on every call against a
  `.vsdx` — verified before the change. They now write ShapeSheet cells (`FillForegnd`,
  `FillPattern`, `LineColor`, `LineWeight`, `Angle`, `FlipX`/`FlipY`, `Width`/`Height`,
  `FillForegndTrans`, `LockAspect`).
  Behavioural notes carried in comments: `Angle` is negated because Visio measures anticlockwise
  while the PowerPoint `Rotation` it replaces was clockwise; `scale` grows about the shape's pin
  rather than its top-left, because Visio has no `ScaleWidth`; `set-opacity` also sets
  `LineColorTrans` so a single knob keeps the shape visually coherent; `lock-aspect-ratio` adds the
  Protection section when a shape does not already carry one.
  Covered by 11 new integration tests in `VisioMcp.Core.Tests/Integration` — the project's first,
  though `integration-tests.yml` already expected them. Each setter is verified through a reader,
  so the tests prove the value reached Visio rather than that the call did not throw.

- **`master` and `hyperlink` removed from the public surface** (#19). Both were advertised MCP
  tools and CLI commands implemented entirely against PowerPoint COM, and **failed on every
  invocation** against a Visio document:
  ```
  $ visiocli master list -s <sid>
  {"success":false,"error":"RuntimeBinderException: 'System.__ComObject' does not contain
   a definition for 'SlideMasters'"}
  ```
  Unlike the 24 already-suppressed legacy domains, these were published to LLMs and CLI users with
  confident descriptions — `master` described itself as *"Inspect and edit slide masters and
  layouts"*, inviting an agent to select it and receive an opaque binder error.
  `PublicSurface = false` on each `[McpTool]` attribute removes them from the MCP schema, the CLI
  and both shipped skill packages at once, because all three generators already filter on that
  flag. The generated MCP skill's advertised operation count self-corrected from **149 to 139**.
  They return when reimplemented against `Document.Masters` (#34) and `Shape.Hyperlinks` (#35).
  A new regression test, `AllPublicTools_DoNotThrowRuntimeBinderException_AgainstVsdx`, sweeps
  every publicly listed tool against a real `.vsdx` session and fails if any returns a
  `RuntimeBinderException`.

- **ADR-001 and Rule 30 rewritten to state the policy the repository actually follows** (#31).
  Both previously forbade unit tests outright — ADR-001 said *"We do NOT write traditional unit
  tests"* and *"❌ Write unit tests for business logic"*, Rule 30 said *"NEVER write unit tests"* —
  while `VisioMcp.Core.Tests` is entirely unit tests and the largest block of passing tests in the
  suite. As written the policy forbade the majority of existing coverage, so either every
  contributor was violating it or it was wrong. Contributors and coding agents are told these
  documents are binding.
  The rule is now: **anything touching Visio COM must be an integration test; never mock a COM
  object; logic with no COM dependency may be unit tested**. The defensible half of the original
  argument is kept and is now the actual rule.
  `docs/ADR-001-NO-UNIT-TESTS.md` is renamed to `docs/ADR-001-TESTING-STRATEGY.md`, since the old
  filename asserted the position being corrected, and its PowerPoint-era examples (`IPptBatch`,
  `CreateSlide`, `ctx.Presentation.Slides`) are rewritten for Visio. References updated in
  `docs/DEVELOPMENT.md`, `tests/README.md` and `.github/instructions/`.

- **`IVisioBatch.PowerPointProcessId` renamed to `VisioProcessId`, and `IsPowerPointProcessAlive()`
  to `IsVisioProcessAlive()`** (#25). Both implementations were already Visio-correct —
  `VisioBatch` captures the PID via `Process.GetProcessesByName("VISIO")` — only the names were
  PowerPoint-era. Callers in `SessionManager` and `VisioMcpService` updated.

- Reframed feature documentation around validated Visio functionality instead of inherited PowerPoint inventory
- Updated package and extension metadata from PowerPoint language to Visio language
- Continued repository cleanup by removing misleading public PowerPoint claims from user-facing surfaces
