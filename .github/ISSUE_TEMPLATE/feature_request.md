---
name: Feature Request
about: Suggest an idea for VisioMcp
title: '[FEATURE] '
labels: 'enhancement'
assignees: ''

---

## Is your feature request related to a problem?
A clear and concise description of what the problem is. Ex. I'm always frustrated when [...]

## Component
Which component should this feature be added to?
- [ ] **MCP Server** (AI assistant integration)
- [ ] **CLI** (Command-line interface)
- [ ] **Both** (MCP Server and CLI)
- [ ] **Core Library** (Shared functionality)
- [ ] **Not sure**

## Describe the solution you'd like
A clear and concise description of what you want to happen.

## Proposed Syntax

**For CLI:**
```powershell
visiocli <domain> <action> --file <file.vsdx> [options]
```

**For MCP Server:**
- Tool: [e.g., page, shape, text, cell, layer, stencil, export, window, docproperty, shapealign, file]
- Action: [e.g., new-action]
- Parameters: [describe expected parameters]

## Describe alternatives you've considered
A clear and concise description of any alternative solutions or features you've considered.

## Use Case
Describe the specific use case this feature would address:
- [ ] Diagram authoring (pages, shapes, connectors)
- [ ] Diagram inspection / reporting
- [ ] ShapeSheet automation
- [ ] Coding agent automation
- [ ] Macro-enabled document (.vsdm) operations
- [ ] Other: [please specify]

## Target Users
Who would benefit from this feature?
- [ ] **AI Assistants** (GitHub Copilot, Claude, ChatGPT via MCP Server)
- [ ] **Direct CLI Users** (Command-line automation)
- [ ] **CI/CD Pipelines** (Automated Visio development workflows)
- [ ] **Visio Developers** (diagram tooling / templates)
- [ ] **Data Engineers** (ETL workflows)
- [ ] Other: [please specify]

## Visio Operations Involved
What Visio APIs or operations would this feature likely use?
- [ ] Pages (Document.Pages)
- [ ] Shapes / connectors (Page.Shapes, Shape.Connects)
- [ ] ShapeSheet cells (Shape.CellsU / FormulaU)
- [ ] Stencils and masters (Document.Masters, Page.Drop)
- [ ] Layers (Page.Layers)
- [ ] Export / rendering
- [ ] Macro-enabled documents (.vsdm)
- [ ] Other: [please specify]

## Additional context
Add any other context, screenshots, or examples about the feature request here.

## Implementation Notes
If you have ideas about how this could be implemented, please share them here.