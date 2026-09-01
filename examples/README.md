# VisioMcp CLI Examples

This directory contains example configuration for using VisioMcp with MCP clients, plus a walkthrough of session mode.

See [mcp-configs/](mcp-configs/) for ready-to-paste client configuration.

## Session Mode

Session mode keeps a single Visio instance alive across many CLI invocations instead of opening and closing the file for every command.

### Requirements

- Windows with Microsoft Visio installed
- VisioMcp CLI on your PATH (`visiocli`)

### Walkthrough

```powershell
# 1. Create a document and open a session against it
visiocli file create --file test-session.vsdx
$session = visiocli file open --file test-session.vsdx --session-id demo

# 2. Run several operations against the same live Visio instance
visiocli page add    --session-id demo --name "Overview"
visiocli page add    --session-id demo --name "Detail"
visiocli page list   --session-id demo
visiocli shape list  --session-id demo --page-index 1

# 3. Close the session, committing all changes at once
visiocli file close --session-id demo --save
```

Run `visiocli --help` and `visiocli <domain> --help` to discover the exact flags for your build.

### Why Session Mode Is Faster

- Only one Visio instance is started
- No file open/close overhead between operations
- All changes are committed together when the session closes

### Cleanup

```powershell
Remove-Item test-session.vsdx
```

## Use Cases

Session mode is ideal for:

- **Diagram generation** - Building a multi-page document shape by shape
- **Bulk edits** - Applying many ShapeSheet changes across a document
- **Reporting** - Reading pages, shapes, and connections in one pass
- **Testing** - Setting up fixture documents across multiple pages
