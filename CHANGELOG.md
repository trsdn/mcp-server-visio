# Changelog

All notable changes to VisioMcp will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

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
