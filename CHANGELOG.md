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
