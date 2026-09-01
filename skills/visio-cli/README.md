# Visio CLI Skill

Agent skill for AI coding assistants using the Visio CLI tool (`visiocli`).

## Best For

- **Coding agents** (GitHub Copilot, Cursor, Windsurf, Codex, Gemini CLI, and 38+ more)
- Token-efficient workflows (no large tool schemas)
- Discoverable via `visiocli --help`
- Scriptable in PowerShell pipelines, CI/CD, batch processing
- Quiet mode (`-q`) outputs clean JSON only

## Why CLI Over MCP?

Modern coding agents increasingly favor CLI-based workflows:

```powershell
# Token-efficient: No schema overhead
visiocli -q session open C:\Data\Diagram.vsdx
visiocli -q page list --session 1
visiocli -q session close --session 1 --save
```

## Installation

### GitHub Copilot

The [VisioMcp VS Code extension](https://marketplace.visualstudio.com/items?itemName=trsdn.visio-mcp) installs this skill automatically to `~/.copilot/skills/visio-cli/`.

Enable skills in VS Code settings:
```json
{
  "chat.useAgentSkills": true
}
```

### Other Platforms

Extract to your AI assistant's skills directory:

| Platform | Location |
|----------|----------|
| **Claude Code** | `.claude/skills/visio-cli/` |
| **Cursor** | `.cursor/skills/visio-cli/` |
| **Windsurf** | `.windsurf/skills/visio-cli/` |
| **Gemini CLI** | `.gemini/skills/visio-cli/` |
| **Codex** | `.codex/skills/visio-cli/` |
| **And 36+ more** | Via `npx skills` |
| **Goose** | `.goose/skills/visio-cli/` |

Or use npx:
```bash
# Interactive - prompts to select visio-cli, visio-mcp, or both
npx skills add trsdn/mcp-server-visio

# Or specify directly
npx skills add trsdn/mcp-server-visio --skill visio-cli
```

## Contents

```
visio-cli/
├── SKILL.md           # Main skill definition with CLI command guidance
├── README.md          # This file
└── references/        # Detailed domain-specific guidance
    ├── behavioral-rules.md
    ├── generation-pipeline.md
    ├── visio_agent_mode.md
    ├── diagram-design-principles.md
    └── diagram-design-review.md
```

## CLI Installation

Install the CLI tool via NuGet:
```powershell
dotnet tool install --global VisioMcp.CLI
```

Verify installation:
```powershell
visiocli --version
visiocli --help
```

## Related

- [Visio MCP Skill](https://github.com/trsdn/mcp-server-visio/releases) - For conversational AI (Claude Desktop, VS Code Chat)
- [Documentation](https://github.com/trsdn/mcp-server-visio)
- [GitHub Repository](https://github.com/trsdn/mcp-server-visio)
