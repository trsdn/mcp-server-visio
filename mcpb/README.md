# Visio (Windows)

**Automate Microsoft Visio with Claude** - Control Visio through natural language conversations. Requires Windows and a local Visio install.

## What It Does

Visio MCP Server lets you automate Visio through conversation with Claude:

- **Create & Edit** - Documents, pages, shapes, and connectors
- **Inspect** - List pages, shapes, connections, layers, and stencil masters
- **ShapeSheet Access** - Read and write any cell formula for precise geometry and formatting
- **Stencils & Masters** - Drop standard shapes from Visio stencils
- **Export** - Save pages as images or PDF
- **Agent Mode** - Say "show me Visio" and watch the automation run live, side by side with Claude

**11 tools with 101 operations** across 11 Visio-native command domains.

## Requirements

- **Windows** (required - uses Visio COM automation)
- **Microsoft Visio 2016 or later**
- **Claude Desktop** (Windows version)

## Installation

1. Download the `.mcpb` file from the [latest release](https://github.com/trsdn/mcp-server-visio/releases/latest)
2. Double-click to install in Claude Desktop
3. Restart Claude Desktop if prompted

That's it! Start a new conversation and ask Claude to work with Visio.

## Usage Examples

These examples work with any Visio file, including a new empty document.

### Example 1: Build a process flow

**You say:** *"Create a new Visio file called OrderProcess.vsdx with a page named Overview. Draw four boxes labeled Intake, Review, Approval, and Fulfilment left to right, and connect them in sequence."*

**What happens:**
- Creates a new document and renames the first page to `Overview`
- Draws four rectangles and sets the text on each
- Adds connectors gluing each box to the next
- Lists the connections back so you can confirm the flow is correct

### Example 2: Tidy up an existing diagram

**You say:** *"Open Architecture.vsdx, align all shapes on page 2 along their top edges and distribute them evenly across the page."*

**What happens:**
- Opens the document and reads the shapes on page 2
- Aligns the selection along the top edge
- Distributes the shapes with even horizontal spacing
- Reports the shape names it moved

### Example 3: Precise geometry through the ShapeSheet

**You say:** *"In Network.vsdx, set the Router shape to exactly 2 inches wide and 1 inch tall, position it at 4, 5 on the page, and fill it light blue."*

**What happens:**
- Writes `Width`, `Height`, `PinX`, and `PinY` in the shape's ShapeSheet
- Writes the `FillForegnd` cell for the fill colour
- Reads the cells back so the applied values are verifiable

---

**More things you can ask:**

- *"Show me Visio side-by-side while you build this diagram"* - Agent Mode: watch every step happen live
- *"Drop a Server master from the Basic Network stencil onto page 1"*
- *"List every shape on page 3 with its position and size"*
- *"Which shapes is the 'Database' shape connected to?"*
- *"Put the background shapes on their own layer and lock it"*
- *"Export page 1 as a PNG"*

## Tips for Best Results

- **Be specific** - Include file paths, page names, and shape names when you know them
- **Start simple** - Build complex diagrams step by step
- **Ask to see Visio** - Say *"Show me Visio while you work"* to watch changes in real-time
- **Close files first** - Visio MCP needs exclusive access to a document during automation

## Privacy & Security

Visio MCP Server runs **entirely on your computer**. Your Visio data:
- Never leaves your machine
- Is not sent to any external servers
- Is not used for training AI models

**Zero Logging:** This software does not collect any telemetry, usage statistics, or analytics data. No data is transmitted to external services.

## Troubleshooting

**Claude says the tool isn't available:**
- Restart Claude Desktop after installation
- Check Settings → Integrations to verify Visio MCP Server is enabled

**Visio operations fail:**
- Close the document in Visio before asking Claude to modify it
- Ensure Visio is installed and working normally

**Need help?**
- [Report an issue](https://github.com/trsdn/mcp-server-visio/issues)
- [Full documentation](https://github.com/trsdn/mcp-server-visio)

## Links

- [GitHub Repository](https://github.com/trsdn/mcp-server-visio)
- [Feature Reference](https://github.com/trsdn/mcp-server-visio/blob/main/FEATURES.md)
- [Agent Skills](https://github.com/trsdn/mcp-server-visio/blob/main/skills/README.md) - Cross-platform AI guidance
- [Privacy Policy](https://github.com/trsdn/mcp-server-visio/blob/main/mcpb/README.md#privacy--security)
- [License (MIT)](https://github.com/trsdn/mcp-server-visio/blob/main/LICENSE)
