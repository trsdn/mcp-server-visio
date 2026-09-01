# Installation Guide - VisioMcp

Complete installation instructions for the VisioMcp MCP Server and CLI tool.

## System Requirements

### Required
- **Windows OS** (Windows 10 or later)
- **Microsoft Visio desktop** (Microsoft 365, Professional, or Standard desktop install)
- **.NET 10 Runtime or SDK** (not required for VS Code Extension or MCPB - they bundle it)

### Optional (for specific features)
- **Node.js** - Required for `npx` commands (`add-mcp` auto-configuration, agent skills). Install with `winget install OpenJS.NodeJS.LTS` or from [nodejs.org](https://nodejs.org/)

### Recommended
- Windows 11 for best performance
- Office 365 with latest updates
- 8GB RAM minimum

---

## Quick Start (Recommended)

Use this order to avoid setup confusion:

1. **Choose one primary setup path**:
   - VS Code Extension (Copilot users)
   - Claude Desktop MCPB
   - Manual MCP setup (all other MCP clients)
2. **Validate MCP setup** (run the quick test prompt in Step 4 of manual setup, or test in your client after extension/MCPB install)
3. **Optional:** install CLI (`visiocli`) for scripting/RPA
4. **Optional:** install agent skills for non-extension environments

### VS Code Extension (Easiest - One-Click Setup)

**Best for:** GitHub Copilot users, beginners, anyone wanting automatic configuration

1. **Install the Extension**
   - Open VS Code
   - Press `Ctrl+Shift+X` (Extensions)
   - Search for **"VisioMcp"**
   - Click **Install**

2. **That's It!**
   - Bundles self-contained MCP server and CLI (no .NET runtime or SDK needed)
   - Auto-configures GitHub Copilot
   - Registers agent skills (visio-mcp + visio-cli) via `chatSkills`
   - Shows quick start guide on first launch

**Marketplace Link:** [VisioMcp VS Code Extension](https://marketplace.visualstudio.com/items?itemName=trsdn.visio-mcp)

---

### Claude Desktop (One-Click Install)

**Best for:** Claude Desktop users who want the simplest installation

1. Download `visio-mcp-{version}.mcpb` from the [latest release](https://github.com/trsdn/mcp-server-visio/releases/latest)
2. Double-click the `.mcpb` file (or drag-and-drop onto Claude Desktop)
3. Restart Claude Desktop

That's it! The MCPB bundle includes everything needed - no .NET installation required.

---

## Manual MCP Setup (All MCP Clients)

**Best for:** Other MCP clients (Cursor, Windsurf, Cline, Claude Code, Codex), advanced users

### Step 1: Install .NET 10

**Check if already installed:**
```powershell
dotnet --version
# Should show 10.0.x or higher
```

**If not installed:**

```powershell
winget install Microsoft.DotNet.Runtime.10
```

**Manual Download:** [.NET 10 Downloads](https://dotnet.microsoft.com/download/dotnet/10.0)

### Step 2: Install VisioMcp MCP Server

```powershell
# Install MCP Server tool (command: mcp-visio)
dotnet tool install --global VisioMcp.McpServer

# Verify installation
dotnet tool list --global | Select-String "VisioMcp"
```

> **Optional:** If you also want the standalone CLI command (`visiocli`) for scripting/RPA, install it separately:
> ```powershell
> dotnet tool install --global VisioMcp.CLI
> ```

### Step 3: Configure Your MCP Client

#### Option A: Auto-Configure All Agents (Recommended)

Use [`add-mcp`](https://github.com/neondatabase/add-mcp) to configure all detected coding agents with a single command:

```powershell
npx add-mcp "mcp-visio" --name visio-mcp
```

This auto-detects and configures **Cursor, VS Code, Claude Code, Claude Desktop, Codex, Zed, Gemini CLI**, and more. Use flags to customize:

```powershell
# Configure specific agents only
npx add-mcp "mcp-visio" --name visio-mcp -a cursor -a claude-code

# Configure globally (user-wide, all projects)
npx add-mcp "mcp-visio" --name visio-mcp -g

# Non-interactive (skip prompts)
npx add-mcp "mcp-visio" --name visio-mcp --all -y
```

> **Requires:** [Node.js](https://nodejs.org/) for `npx`. Install with `winget install OpenJS.NodeJS.LTS` if not already available. No permanent `add-mcp` installation needed — `npx` downloads, runs, and cleans up automatically.

#### Option B: Manual Configuration

**Quick Start:** Ready-to-use config files for all clients are available in [`examples/mcp-configs/`](https://github.com/trsdn/mcp-server-visio/tree/main/examples/mcp-configs/)

**For GitHub Copilot (VS Code):**

Create `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "visio-mcp": {
      "command": "mcp-visio"
    }
  }
}
```

**For GitHub Copilot (Visual Studio):**

Create `.mcp.json` in your solution directory or `%USERPROFILE%\.mcp.json`:

```json
{
  "servers": {
    "visio-mcp": {
      "command": "mcp-visio"
    }
  }
}
```

**For Claude Desktop:**

1. Locate config file: `%APPDATA%\Claude\claude_desktop_config.json`
2. If file doesn't exist, create it with the content below
3. If file exists, merge the `visio-mcp` entry into your existing `mcpServers` section

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "mcp-visio",
      "args": [],
      "env": {}
    }
  }
}
```

4. Save and restart Claude Desktop

**For Cursor:**

1. Open Cursor Settings (Ctrl+,)
2. Search for "MCP" in settings
3. Click "Edit in settings.json" or create config at: `%APPDATA%\Cursor\User\globalStorage\mcp\mcp.json`
4. Add this configuration:

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "mcp-visio",
      "args": [],
      "env": {}
    }
  }
}
```

5. Save and restart Cursor

**For Cline (VS Code Extension):**

1. Install Cline extension in VS Code
2. Open Cline panel and click the MCP settings gear icon
3. Add this configuration:

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "mcp-visio",
      "args": [],
      "env": {}
    }
  }
}
```

4. Save and restart VS Code

**For Windsurf:**

1. Open Windsurf Settings
2. Navigate to MCP Servers configuration  
3. Add this configuration:

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "mcp-visio",
      "args": [],
      "env": {}
    }
  }
}
```

4. Save and restart Windsurf

### Step 4: Validate MCP Setup

Restart your MCP client, then ask:
```
Create an empty Visio file called "test.vsdx"
```

If it works, you're all set! 🎉

**💡 Tip:** Want to watch the AI work? Ask:
```
Show me Visio while you work on test.vsdx
```
This opens Visio visibly so you can see every change in real-time - great for debugging and demos!

---

## Optional: CLI Installation (No AI Required)

**Best for:** Scripting, RPA, CI/CD pipelines, automation without AI

### Install CLI

```powershell
# Install CLI as a separate .NET tool
dotnet tool install --global VisioMcp.CLI

# Verify CLI is available
visiocli --version
```

> **⚠️ Version Sync:** If you install both MCP Server and CLI, keep both packages on the same version.

### Quick Test

```powershell
# Session-based workflow (keeps Visio open between commands)
visiocli -q session open test.vsdx --show            # Returns session ID and keeps Visio visible
visiocli -q page list --session <session-id>         # List pages
visiocli -q session close --session <session-id> --save
```

> **💡 Tip:** Use `-q` (quiet mode) to suppress banner and get JSON output only - perfect for scripting and automation.

**CLI Documentation:** [CLI Guide](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.CLI/README.md)

---

## Agent Skills Installation (Cross-Platform)

**Best for:** Adding AI guidance to coding agents (Copilot, Cursor, Windsurf, Claude Code, Gemini, Codex, etc.)

Skills are auto-installed by the VS Code extension. For other platforms:

```powershell
# CLI skill (for coding agents - token-efficient workflows)
npx skills add trsdn/mcp-server-visio --skill visio-cli

# MCP skill (for conversational AI - rich tool schemas)
npx skills add trsdn/mcp-server-visio --skill visio-mcp

# Install for specific agents
npx skills add trsdn/mcp-server-visio --skill visio-cli -a cursor
npx skills add trsdn/mcp-server-visio --skill visio-mcp -a claude-code

# Install globally (user-wide)
npx skills add trsdn/mcp-server-visio --skill visio-cli --global
```

**Supports 43+ agents** including claude-code, github-copilot, cursor, windsurf, gemini-cli, codex, goose, cline, continue, replit, and more.

**📚 [Agent Skills Guide →](../skills/README.md)**

---

## Updating VisioMcp

### Check Installed Version

**MCP Server and CLI:**
```powershell
dotnet tool list --global | Select-String "VisioMcp"

# Or check CLI version
visiocli --version
```

### Update Installed Tools

> **⚠️ If both are installed:** update MCP Server and CLI together so versions stay in sync.

**Step 1: Update both tools**
```powershell
dotnet tool update --global VisioMcp.McpServer
dotnet tool update --global VisioMcp.CLI
```

**Step 2: Verify update**
```powershell
# Check installed version
dotnet tool list --global | Select-String "VisioMcp"

# Verify both tools work
visiocli --version
mcp-visio --version
```

**Step 3: Restart your MCP client**
- Restart VS Code, Claude Desktop, Cursor, or whichever client you're using
- The new version will be used automatically

### Troubleshooting Updates

#### Update Command Fails

**Error: "Tool not found"**
```powershell
# The tool may need to be reinstalled
dotnet tool uninstall --global VisioMcp.McpServer
dotnet tool install --global VisioMcp.McpServer
```

**Error: "Access denied"**
- Run PowerShell as Administrator
- Or install in user directory (not global):
```powershell
dotnet tool update --global VisioMcp.McpServer --install-dir ~/.dotnet/tools
```

#### MCP Server Still Running Old Version

**Solution:** Fully restart your MCP client
- Close VS Code completely (including terminal windows)
- Close Claude Desktop completely
- Reopen the application

**Still not working?**
```powershell
# Reinstall the tool
dotnet tool uninstall --global VisioMcp.McpServer
dotnet tool install --global VisioMcp.McpServer
```

### Rollback to Previous Version

If an update causes issues, you can downgrade:

```powershell
# Uninstall current version
dotnet tool uninstall --global VisioMcp.McpServer

# Install specific version
dotnet tool install --global VisioMcp.McpServer --version 1.2.3
# Replace 1.2.3 with the version you want
```

### Check What's New

Before updating, check the release notes:
- **GitHub Releases:** https://github.com/trsdn/mcp-server-visio/releases
- **Changelog:** https://github.com/trsdn/mcp-server-visio/blob/main/CHANGELOG.md

---

## Troubleshooting

### Common Issues

#### 1. "dotnet command not found"

**Solution:** Install .NET 10 SDK or Runtime (see Step 1 above)

#### 2. MCP Server Not Responding

**Check if tool is installed:**
```powershell
dotnet tool list --global | Select-String "VisioMcp"
```

**Reinstall if missing:**
```powershell
dotnet tool uninstall --global VisioMcp.McpServer
dotnet tool install --global VisioMcp.McpServer
```

#### 3. "Presentation is locked" or "Cannot open file"

**Solution:** Close all Visio windows before running VisioMcp

VisioMcp requires exclusive access to the target document during automation.

## Uninstallation

### Uninstall MCP Server
```powershell
dotnet tool uninstall --global VisioMcp.McpServer
```

### Uninstall CLI
```powershell
dotnet tool uninstall --global VisioMcp.CLI
```

---

## Getting Help

- **Documentation:** [GitHub Repository](https://github.com/trsdn/mcp-server-visio)
- **Issues:** [GitHub Issues](https://github.com/trsdn/mcp-server-visio/issues)
- **Contributing:** [Contributing Guide](https://github.com/trsdn/mcp-server-visio/blob/main/docs/CONTRIBUTING.md)

---

## Next Steps

After installation:

1. **Learn the basics:** Try simple commands like creating pages, adding shapes, and setting text
2. **Explore features:** See [README](https://github.com/trsdn/mcp-server-visio#readme) for complete feature list
3. **Read the guides:**
   - [MCP Server Guide](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.McpServer/README.md)
   - [CLI Guide](https://github.com/trsdn/mcp-server-visio/blob/main/src/VisioMcp.CLI/README.md)
   - [Agent Skills](https://github.com/trsdn/mcp-server-visio/blob/main/skills/visio-mcp/SKILL.md) - Cross-platform AI guidance
4. **Join the community:** Star the repo, report issues, contribute improvements

---

## Agent Skills (Optional)

Agent Skills provide domain-specific guidance to AI coding assistants, helping them use VisioMcp more effectively.

> **Note:** Agent Skills are for **coding agents** (GitHub Copilot, Claude Code, Cursor). **Claude Desktop** uses MCP Prompts instead (included automatically via the MCP Server).

### Two Skills for Different Use Cases

| Skill | Target | Best For |
|-------|--------|----------|
| **visio-cli** | CLI Tool | **Coding agents** (Copilot, Cursor, Windsurf) - token-efficient, `visiocli --help` discoverable |
| **visio-mcp** | MCP Server | **Conversational AI** (Claude Desktop, VS Code Chat) - rich tool schemas, exploratory workflows |

**VS Code Extension:** Skills are installed automatically to `~/.copilot/skills/`.

**Other Platforms (Claude Code, Cursor, Windsurf, Gemini, Codex, etc.):**

```powershell
# Install CLI skill (recommended for coding agents - Copilot, Cursor, Windsurf, Codex, etc.)
npx skills add trsdn/mcp-server-visio --skill visio-cli

# Install MCP skill (for conversational AI - Claude Desktop, VS Code Chat)
npx skills add trsdn/mcp-server-visio --skill visio-mcp

# Interactive install - prompts to select visio-cli, visio-mcp, or both
npx skills add trsdn/mcp-server-visio

# Install specific skill directly
npx skills add trsdn/mcp-server-visio --skill visio-cli   # Coding agents
npx skills add trsdn/mcp-server-visio --skill visio-mcp   # Conversational AI

# Install both skills
npx skills add trsdn/mcp-server-visio --skill '*'

# Target specific agent (optional - auto-detects if omitted)
npx skills add trsdn/mcp-server-visio --skill visio-cli -a cursor
npx skills add trsdn/mcp-server-visio --skill visio-mcp -a claude-code
```

**Manual Installation:**
1. Download the latest Visio skills bundle from [GitHub Releases](https://github.com/trsdn/mcp-server-visio/releases/latest)
2. The package contains both skills:
   - `skills/visio-cli/` - for coding agents (Copilot, Cursor, Windsurf)
   - `skills/visio-mcp/` - for conversational AI (Claude Desktop, VS Code Chat)
3. Extract the skill(s) you need to your AI assistant's skills directory:
   - Copilot: `~/.copilot/skills/visio-cli/` or `~/.copilot/skills/visio-mcp/`
   - Claude Code: `.claude/skills/visio-cli/` or `.claude/skills/visio-mcp/`
   - Cursor: `.cursor/skills/visio-cli/` or `.cursor/skills/visio-mcp/`

**See:** [Agent Skills Documentation](https://github.com/trsdn/mcp-server-visio/blob/main/skills/README.md)

---

**Happy automating! 🚀**
