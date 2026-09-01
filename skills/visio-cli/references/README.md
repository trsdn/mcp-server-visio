# Visio Skill References

This folder contains supporting guidance for the generated Visio CLI skill.

**Note for developers:** Run `dotnet build -c Release` to regenerate `SKILL.md` and refresh copied reference content from `skills/shared/`.

**Note for users:** Published skill packages already include the reference files needed by the skill.

## Contents

- `behavioral-rules.md` — execution and verification rules
- `generation-pipeline.md` — page-and-shape generation workflow
- `visible-session-mode.md` — visible-session guidance for users who want to watch the work
- `diagram-design-principles.md` — page layout and diagram readability guidance
- `diagram-design-review.md` — self-review checklist and quality scorecard for finished diagrams

The command reference lives in `SKILL.md`, which is generated from the CLI itself. There is no
hand-maintained command list here: the previous one claimed to be auto-generated but had been
committed by hand, and still listed commands from two migrations ago, none of which exist.