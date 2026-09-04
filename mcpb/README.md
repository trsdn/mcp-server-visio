# Visio (Windows)

**Automate Microsoft Visio with Claude** — build and edit diagrams through conversation. Requires
Windows and a local Visio install.

## What It Does

Visio MCP Server drives Visio through COM, so Claude works with real diagrams rather than a file
format:

- **Build diagrams** — drop stencil masters, connect them, lay out pages
- **Connect properly** — real Visio connectors that stay attached when shapes move
- **Read and write the ShapeSheet** — cells and formulas on shapes, pages and the document
- **Structure drawings** — multiple pages, layers, background pages, shape data
- **Format consistently** — named styles, colour palettes, fills and lines
- **Guided design** — a catalogue of diagram archetypes, each naming stencils and masters that are
  installed on your machine
- **Watch it work** — ask to see Visio and follow along side by side with Claude

**16 tools with 180 actions**.

## Requirements

- **Windows** (required — uses Visio COM automation)
- **Microsoft Visio 2016 or later**
- **Claude Desktop** (Windows version)

## Installation

1. Download the `.mcpb` file from the [latest release](https://github.com/trsdn/mcp-server-visio/releases/latest)
2. Double-click to install in Claude Desktop
3. Restart Claude Desktop if prompted

Start a new conversation and ask Claude to work with Visio.

## Usage Examples

These work with any `.vsdx`, including a new empty drawing.

### Example 1: A process flowchart

**You say:** *"Create OrderFlow.vsdx with a flowchart for order fulfilment: receipt, picking,
packing, dispatch, and a decision on whether the item is in stock."*

**What happens:**
- Creates the drawing and names the page
- Drops `Start/End`, `Process` and `Decision` masters from the basic flowchart stencil
- Connects them in sequence, with both branches off the decision
- Labels every shape
- Confirms with the file location

The shapes are real flowchart masters, not drawn rectangles — so a diamond *is* a decision, and
Visio's own tooling treats it as one.

### Example 2: A network diagram with shape data

**You say:** *"Add a page showing our branch office network — firewall, switch, two servers and
four workstations — and record the owner and asset tag on each device."*

**What happens:**
- Adds and names a page
- Drops network masters and connects them
- Writes owner and asset tag into Shape Data on each device, where Visio can report on it

### Example 3: A layered architecture diagram

**You say:** *"Create a block diagram of a layered application — presentation, application, domain
and data — apply one named style to every block, and put the annotations on their own layer so I
can hide them."*

**What happens:**
- Builds the four layers and connects them
- Creates a named style and applies it to each block, so later changes are one edit
- Puts callouts on a separate layer you can toggle

---

**More things you can ask:**

- *"Show me Visio while you work"* — watch each step happen live
- *"Which diagram archetypes do you know, and which stencils are installed?"*
- *"Give this page a background page with the title and revision date"*
- *"Continue this flowchart on a second page with an off-page reference"*
- *"Set the page to A3 landscape and fit the drawing to it"*
- *"List every connector on page 1 and tell me which shapes are unconnected"*

That last one is worth knowing: Claude can read the drawing's structure, so it can tell you a shape
is *unconnected* even when the picture looks fine.

## Tips for Best Results

- **Be specific** — include file paths, page numbers and shape names when you know them
- **Start simple** — build a complex diagram in steps
- **Ask for masters** — say "use flowchart shapes" rather than "draw boxes"; masters carry meaning,
  drawn rectangles do not
- **Ask to see Visio** — *"show me Visio while you work"* to watch changes in real time
- **Close the file first** — the server needs exclusive access to the drawing it is editing

## Privacy & Security

Visio MCP Server runs **entirely on your computer**. Your Visio data:

- never leaves your machine
- is not sent to any external service
- is not used for training AI models

**Zero logging:** no telemetry, usage statistics or analytics are collected or transmitted.

Details: [Security policy](https://github.com/trsdn/mcp-server-visio/blob/main/SECURITY.md)

## Troubleshooting

**Claude says the tool isn't available:**
- Restart Claude Desktop after installation
- Check Settings → Integrations to confirm Visio MCP Server is enabled

**Visio operations fail:**
- Close the drawing in Visio before asking Claude to modify it
- Confirm Visio is installed and starts normally

**A shape or stencil is not found:**
- Ask Claude which stencils are installed — the catalogue only lists masters present on your
  machine, and stencil availability varies by Visio edition

**Need help?**
- [Report an issue](https://github.com/trsdn/mcp-server-visio/issues)

## Links

- [GitHub repository](https://github.com/trsdn/mcp-server-visio)
- [Feature reference](https://github.com/trsdn/mcp-server-visio/blob/main/FEATURES.md)
- [Agent skills](https://github.com/trsdn/mcp-server-visio/blob/main/skills/README.md) — cross-platform AI guidance
- [Security policy](https://github.com/trsdn/mcp-server-visio/blob/main/SECURITY.md)
- [License (MIT)](https://github.com/trsdn/mcp-server-visio/blob/main/LICENSE)
