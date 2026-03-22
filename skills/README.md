# VisioMcp - Agent Skills

Two skill packages for AI coding assistants working against the current Visio CLI and MCP surfaces:

| Skill | Target | Best For |
|-------|--------|----------|
| **[visio-cli](visio-cli/SKILL.md)** | CLI Tool | Coding agents - token-efficient, `--help` discoverable |
| **[visio-mcp](visio-mcp/SKILL.md)** | MCP Server | Conversational AI - rich tool schemas |

## Installation

```bash
# Via VS Code extension (auto-installs visio-mcp)
# Or via npx:
npx skills add trsdn/mcp-server-visio --skill visio-cli   # Coding agents
npx skills add trsdn/mcp-server-visio --skill visio-mcp   # Conversational AI
```

## Building

```powershell
dotnet build -c Release
```

Generates `SKILL.md` and copies `shared/` references into each skill's `references/` folder.

## Structure

```
skills/
├── shared/          # Shared behavioral guidance (source of truth)
├── visio-mcp/       # MCP Server skill (SKILL.md + references/)
├── visio-cli/       # CLI skill (SKILL.md + references/)
├── templates/       # Scriban templates for SKILL.md generation
├── CLAUDE.md        # Claude Code project instructions
└── .cursorrules     # Cursor-specific rules
```
