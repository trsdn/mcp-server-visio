# Claude Desktop Configuration

VisioMcp works with Claude Desktop on Windows, but the host environment still has some practical constraints.

## Configuration Location

Claude Desktop config file:
```
%APPDATA%\Claude\claude_desktop_config.json
```

## Basic Configuration

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "mcp-visio",
      "args": []
    }
  }
}
```

Or using the packaged executable directly:

```json
{
  "mcpServers": {
    "visio-mcp": {
      "command": "visio-mcp-server.exe",
      "args": []
    }
  }
}
```

## Windows Environment Considerations

Claude Desktop runs with practical file-system constraints:

### File System Access

Keep Visio files in locations Claude Desktop can access easily:

- **User Documents**: `C:\Users\<username>\Documents\`
- **User Desktop**: `C:\Users\<username>\Desktop\`
- **Temp directory**: `%TEMP%` or `C:\Users\<username>\AppData\Local\Temp\`

**Recommendation:** Use your Documents folder unless you have a reason not to.

### Visio Instance

- VisioMcp manages its own Visio instance through COM automation
- The Visio window may be visible or hidden depending on the operation
- Long-running operations can briefly surface Office progress UI

### Session Persistence

Sessions are tied to the Claude Desktop session:

- Closing Claude Desktop terminates active Visio sessions
- Unsaved changes may be lost
- Use explicit `file(action: 'close', save: true)` to persist work

## Recommended Workflow

```
1. Create or open a file in an accessible location:
   file(action: 'create', filePath: 'C:\\Users\\Me\\Documents\\process-diagram.vsdx')

2. Perform operations with the returned sessionId

3. Explicitly save and close when done:
   file(action: 'close', sessionId: '...', save: true)
```

## Troubleshooting

### "Visio not found" Error

- Ensure Microsoft Visio desktop is installed on the Windows system
- Current public guidance assumes a supported desktop Visio installation

### "Access denied" Error

- Check that the file path is in an accessible directory
- Ensure the file is not open in another Visio instance
- Try using the Documents folder instead of a protected location

### "COM timeout" Error

- Visio may be showing a dialog; check for a visible Visio window
- The operation may still be in progress; wait for completion
- Restart Claude Desktop if the Visio session becomes unresponsive

## MCPB Bundle Alternative

For simpler installation, use the MCPB bundle which auto-configures Claude Desktop:

1. Download `visio-mcp-{version}.mcpb` from releases
2. Double-click to install
3. Restart Claude Desktop

See the main repository for full MCPB installation instructions.
