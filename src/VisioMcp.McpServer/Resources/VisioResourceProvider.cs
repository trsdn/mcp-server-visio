using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace VisioMcp.McpServer.Resources;

/// <summary>
/// MCP resources for documenting the current Visio MCP help surface.
/// Resources help LLMs understand the currently exposed workflows without implying
/// drawing/page/shape resources that are not yet available as MCP resources.
/// 
/// NOTE: MCP SDK 0.4.0-preview.2 does NOT support McpServerResourceTemplate yet.
/// Dynamic drawing/page/shape URI patterns can be added when SDK support exists and
/// the corresponding Visio MCP resources are exposed.
/// </summary>
[McpServerResourceType]
public static class VisioResourceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Documents the currently exposed Visio MCP help resources.
    /// </summary>
    [McpServerResource(UriTemplate = "visio://help/resources")]
    [Description("Guide to available Visio MCP help resources")]
    public static Task<string> GetResourceGuide()
    {
        var guide = new
        {
            title = "Visio MCP Help Resources",
            description = "Help URIs currently exposed by the Visio MCP server",
            note = "The current MCP resource surface is documentation-only. Use the file tool for actual sessions and file operations.",
            resources = new[]
            {
                new
                {
                    uri = "visio://help/resources",
                    purpose = "Lists the help resources currently exposed by the server"
                },
                new
                {
                    uri = "visio://help/quickref",
                    purpose = "Shows the validated MCP workflow and examples for the current Visio surface"
                }
            },
            currentToolSurface = new[]
            {
                new
                {
                    tool = "file",
                    supportedActions = new[] { "create", "open", "list", "save", "close", "test" },
                    note = "Use file first in every workflow to create or open a Visio session."
                }
            },
            currentLimitations = new[]
            {
                "No drawing/page/shape/text/cell/stencil MCP resources are exposed yet.",
                "Use MCP tools, not resources, for real operations.",
                "Dynamic resource templates can be added later when SDK support and validated resource surfaces exist."
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(guide, JsonOptions));
    }

    /// <summary>
    /// Quick reference for the current Visio MCP workflow.
    /// </summary>
    [McpServerResource(UriTemplate = "visio://help/quickref")]
    [Description("Quick reference for the current Visio MCP workflow")]
    public static Task<string> GetQuickReference()
    {
        var quickRef = new
        {
            title = "Visio MCP Quick Reference",
            commonOperations = new[]
            {
                new
                {
                    task = "Test whether a Visio file can be used",
                    tool = "file",
                    action = "test",
                    example = @"file(action: 'test', path: 'C:\Users\you\Documents\diagram.vsdx')"
                },
                new
                {
                    task = "Create a new Visio file and start a session",
                    tool = "file",
                    action = "create",
                    example = @"file(action: 'create', path: 'C:\Users\you\Documents\new-diagram.vsdx')"
                },
                new
                {
                    task = "Open an existing Visio file",
                    tool = "file",
                    action = "open",
                    example = @"file(action: 'open', path: 'C:\Users\you\Documents\diagram.vsdx', show: true)"
                },
                new
                {
                    task = "List active sessions before opening again",
                    tool = "file",
                    action = "list",
                    example = "file(action: 'list')"
                },
                new
                {
                    task = "Save changes for an open session",
                    tool = "file",
                    action = "save",
                    example = "file(action: 'save', session_id: 'session-123')"
                },
                new
                {
                    task = "Close a session and persist changes",
                    tool = "file",
                    action = "close",
                    example = "file(action: 'close', session_id: 'session-123', save: true)"
                },
                new
                {
                    task = "Seed an initial page target when opening",
                    tool = "file",
                    action = "open/create",
                    example = @"file(action: 'open', path: 'C:\Users\you\Documents\diagram.vsdx', page_name: 'Page-1')"
                }
            },
            sessionWorkflow = new[]
            {
                @"Open or create a session: file(action: 'open'|'create', path: 'C:\...\diagram.vsdx')",
                "Reuse an existing session when possible: file(action: 'list')",
                "Pass session_id to later tool calls that operate on the same Visio file",
                "Persist or discard changes when finished: file(action: 'close', session_id: '...', save: true|false)"
            },
            futureDomainTargets = new[] { "pages", "shapes", "text", "cells", "stencils" },
            note = "Those Visio domains may be layered on top of the session workflow later, but this quick reference only documents the currently validated MCP surface."
        };

        return Task.FromResult(JsonSerializer.Serialize(quickRef, JsonOptions));
    }
}


