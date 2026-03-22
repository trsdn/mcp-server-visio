# Visio MCP Server - Quick Reference

> When the user wants to automate Microsoft Visio diagrams, pages, shapes, ShapeSheet cells, or stencil masters in `.vsdx` files, use the Visio MCP tools provided by this extension.

## When to use Visio MCP

Use these tools when the user wants to:

- create, open, save, or close Visio diagrams
- list or modify pages
- add, move, resize, or delete shapes
- read or write shape text
- inspect or edit ShapeSheet cells
- list stencil masters or drop a master onto a page
- watch Visio while automation runs

Do not use this extension for unrelated file editing, non-Visio formats, or generic CSV/data-processing workflows.

## Prerequisites

- Windows
- Microsoft Visio desktop installed
- the target file should not already be open for manual editing when automation needs exclusive access

## Recommended workflow

```
file/session open or create
-> page / shape / text / cell / stencil operations
-> file/session close
```

## Tool-selection hints

| Goal | Use |
|---|---|
| Work with the diagram lifecycle | `file` / session operations |
| Work with pages | `page` |
| Work with shapes | `shape` |
| Work with labels and body text | `text` |
| Work with ShapeSheet values and formulas | `cell` |
| Work with installed stencils and masters | `stencil` |

## Common mistakes

| Mistake | Fix |
|---|---|
| Closing the session between every step | Keep the session open until the workflow is done |
| Using text operations when the real target is a ShapeSheet formula | Use `cell` |
| Guessing master names | List stencil masters first |
| Forgetting visible mode when the user wants to watch | Open/create the session with visible mode |

## Visible mode reminder

- default to hidden automation for speed unless the user asks to watch
- use visible mode when the user says things like "show me while you work"
- if the session was opened visibly, let the user inspect before closing when appropriate
