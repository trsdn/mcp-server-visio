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

### Fixed

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

### Changed

- Reframed feature documentation around validated Visio functionality instead of inherited PowerPoint inventory
- Updated package and extension metadata from PowerPoint language to Visio language
- Continued repository cleanup by removing misleading public PowerPoint claims from user-facing surfaces
