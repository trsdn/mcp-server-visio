# visio-cli-skill

An [Agent Skill](https://agentskills.io) for automating Microsoft Visio through the `visiocli` command-line tool.

## What this skill does

When loaded by an AI agent (Claude, Codex, Cursor, Gemini CLI, etc.), this skill teaches the agent how to automate the current Visio MVP surface from scripts and CI/CD pipelines:

- **Session workflows** — create, open, list, and close Visio files safely
- **Page operations** — inspect, create, rename, and delete diagram pages
- **Shape operations** — add basic shapes and text boxes, move/resize them, and remove them
- **Text workflows** — read, set, find, and replace labels
- **ShapeSheet cells** — read and write core geometry/formula cells
- **Stencil workflows** — discover masters and drop built-in Visio shapes

## Requirements

- Windows with Microsoft Visio desktop installed
- Install the CLI: `dotnet tool install --global VisioMcp.CLI`

## Install

```bash
npx skillpm install visio-cli-skill
```

Or with npm directly:

```bash
npm install visio-cli-skill
```

## License

MIT
