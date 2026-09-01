# Changelog

All notable changes to VisioMcp will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Security

- **Scriban upgraded 6.6.0 → 7.2.6** (#13): 6.6.0 carried one critical and four moderate advisories
  (GHSA-5wr9-m6jw-xx44, GHSA-6q7j-xr26-3h2c, GHSA-m2p3-hwv5-xpqw, GHSA-q6rr-fm2g-g5x8,
  GHSA-xw6w-9jjh-p9cr). With `TreatWarningsAsErrors`, `NuGetAudit` turned these into five hard build
  errors, so `main` did not build at all.
- **Microsoft.Build.Framework / Microsoft.Build.Utilities.Core upgraded 17.14.8 → 17.14.28** and the
  blanket `<NoWarn>NU1903</NoWarn>` removed from `VisioMcp.Build.Tasks` (#13). The suppression was
  masking CVE-2025-55247 (GHSA-w3q9-fxm7-j8fq); the advisory is now resolved rather than hidden, so
  the project no longer suppresses any dependency audit warnings.

### Added

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
