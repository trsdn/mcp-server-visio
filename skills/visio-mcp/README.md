# Visio MCP Server Skill

Agent skill for AI assistants using the Visio MCP Server via the Model Context Protocol.

## Best For

- **Conversational AI** (Claude Desktop, VS Code Chat)
- Exploratory automation with iterative reasoning
- Self-healing workflows needing rich introspection
- Long-running autonomous tasks with continuous context

## Installation

### GitHub Copilot

The [VisioMcp VS Code extension](https://marketplace.visualstudio.com/items?itemName=trsdn.visio-mcp) installs this skill automatically to `~/.copilot/skills/visio-mcp/`.

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
| **Claude Code** | `.claude/skills/visio-mcp/` |
| **Cursor** | `.cursor/skills/visio-mcp/` |
| **Windsurf** | `.windsurf/skills/visio-mcp/` |
| **Gemini CLI** | `.gemini/skills/visio-mcp/` |
| **Codex** | `.codex/skills/visio-mcp/` |
| **And 36+ more** | Via `npx skills` |
| **Goose** | `.goose/skills/visio-mcp/` |

Or use npx:
```bash
# Interactive - prompts to select visio-cli, visio-mcp, or both
npx skills add trsdn/mcp-server-visio

# Or specify directly
npx skills add trsdn/mcp-server-visio --skill visio-mcp
```

## Contents

```
visio-mcp/
├── SKILL.md           # Main skill definition with MCP tool guidance
├── VERSION            # Version tracking
├── README.md          # This file
└── references/        # Detailed domain-specific guidance
    ├── behavioral-rules.md
    ├── generation-pipeline.md
    ├── visible-session-mode.md
    ├── diagram-design-principles.md
    └── diagram-design-review.md
```

## MCP Server Setup

The skill works with the Visio MCP Server. See [Installation Guide](https://VisioMcpserver.dev/installation/) for setup instructions.

## Related

- [Visio CLI Skill](https://github.com/trsdn/mcp-server-visio/releases) - For coding agents preferring CLI tools
- [Documentation](https://VisioMcpserver.dev/)
- [GitHub Repository](https://github.com/trsdn/mcp-server-visio)
