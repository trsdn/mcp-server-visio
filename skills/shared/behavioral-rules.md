# Behavioral Rules for Visio Automation

These rules keep Visio automation reliable, inspectable, and easy for agents to recover from.

## Core Behavior

- Execute standard tasks without asking for confirmation.
- Prefer discovery over clarifying questions.
- Keep changes incremental and easy to inspect.
- End every run with a short text summary.
- Re-read the Visio state after writes when accuracy matters.

## Discover Instead of Asking

If you need context, use the surface to discover it:

| Need | Prefer |
|---|---|
| Active file or session | `session list` or `file(list)` |
| Which page to edit | `page list` or `page(read)` |
| Which shape to target | `shape list` |
| Which master to use | `stencil list-masters` |
| Current label text | `text get` |
| Current geometry | `cell read` or `cell list` |

## Page-First Workflow

For new work, follow this order:

1. Open or create the file.
2. Inspect or create the target page.
3. Add shapes or drop stencil masters.
4. Set text labels.
5. Use ShapeSheet edits only when needed.
6. Save and close cleanly.

## Prefer Targeted Edits

- Rename, move, resize, relabel, or retarget existing elements when possible.
- Avoid deleting and recreating whole pages unless the structure is genuinely wrong.
- Preserve discovered names and references whenever possible.

## Verification Rules

After creating or changing content, verify the actual state:

- `page list` or `page read` after page creation or rename
- `shape list` or `shape read` after shape changes
- `text get` after label updates
- `cell read` after ShapeSheet edits

Skip explicit verification only for simple read-only tasks.

## Visibility Preference

If the user asks to watch the work, prefer the visible-session option exposed by the current surface. If they want speed or have not asked to watch, background execution is fine.

## End-State Rule

Do not leave sessions hanging. Always save or discard explicitly and close the session when the requested work is done.
