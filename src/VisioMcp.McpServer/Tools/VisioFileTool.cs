using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using VisioMcp.Core.Commands.File;

namespace VisioMcp.McpServer.Tools;

/// <summary>
/// Actions for the file tool (hand-coded because session management is not generated).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<VisioFileAction>))]
public enum VisioFileAction
{
    [JsonStringEnumMemberName("open")] Open,
    [JsonStringEnumMemberName("close")] Close,
    [JsonStringEnumMemberName("create")] Create,
    [JsonStringEnumMemberName("list")] List,
    [JsonStringEnumMemberName("test")] Test,
    [JsonStringEnumMemberName("save")] Save
}

/// <summary>
/// File and session management tool for MCP server.
/// </summary>
[McpServerToolType]
public static class VisioFileTool
{
    /// <summary>
    /// File and session management for automation workflows.
    ///
    /// WORKFLOW: open → use session_id with other tools → close (save=true to persist changes).
    /// NEW FILES: Use 'create' action to create file AND start session in one call.
    /// OPTIONAL CONTEXT: page_name/page_index can seed the initial page target stored with the session.
    ///
    /// SESSION REUSE: Call 'list' first to check for existing sessions.
    /// If file is already open, reuse existing session_id instead of opening again.
    /// </summary>
    [McpServerTool(Name = "file", Title = "File Operations", Destructive = true)]
    [Description("File and session management — the FIRST tool for every workflow. "
        + "WORKFLOW: file(open, path='C:\\...\\file.vsdx') → use session_id with page/shape/text/cell/stencil tools → file(close, save=true). "
        + "NEW FILES: file(create, path='C:\\...\\new.vsdx') creates file AND starts session. "
        + "REUSE: file(list) first — if file is already open, reuse the session_id. "
        + "OPTIONAL CONTEXT: page_name/page_index store an initial page target with the session. "
        + "show=true makes Visio visible (Agent Mode). timeout_seconds: max operation time (default 300).")]
    public static string VisioFile(
        [Description("The action to perform")]
        VisioFileAction action,
        [Description("Full Windows path to a Visio document, for example C:\\Users\\me\\Documents\\diagram.vsdx. Required for open, create and test")]
        [DefaultValue(null)] string? path,
        [Description("Session ID returned by open or create. Required for close, save and every page/shape/text/cell/stencil call")]
        [DefaultValue(null)] string? session_id,
        [Description("Optional page name to remember with the session as the default target")]
        [DefaultValue(null)] string? page_name = null,
        [Description("Optional 1-based page index to remember with the session as the default target")]
        [DefaultValue(null)] int? page_index = null,
        [Description("True to write changes to disk when closing. Edits are lost if this is false")]
        [DefaultValue(false)] bool save = false,
        [Description("True to make the Visio window visible so the user can watch the automation run (Agent Mode). Slower, but useful for demonstration and debugging")]
        [DefaultValue(false)] bool show = false,
        [Description("Maximum time in seconds for a single operation before the session is closed. Raise it for very large documents")]
        [DefaultValue(300)] int timeout_seconds = 300)
    {
        return VisioToolsBase.ExecuteToolAction("file", action.ToString().ToLowerInvariant(), path, () =>
        {
            return action switch
            {
                VisioFileAction.List => ListSessions(),
                VisioFileAction.Open => OpenSession(path!, show, timeout_seconds, page_name, page_index),
                VisioFileAction.Close => CloseSession(session_id!, save),
                VisioFileAction.Create => CreateSession(path!, show, timeout_seconds, page_name, page_index),
                VisioFileAction.Test => TestFile(path!),
                VisioFileAction.Save => SaveSession(session_id!),
                _ => throw new ArgumentException($"Unknown action: {action}")
            };
        });
    }

    private static string OpenSession(string path, bool show, int timeoutSeconds, string? pageName, int? pageIndex)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is required for 'open' action");

        var pathError = VisioToolsBase.ValidateWindowsPath(path);
        if (pathError != null) return pathError;

        if (!File.Exists(path))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errorMessage = $"File not found: {path}",
                filePath = path,
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        var response = ServiceBridge.ServiceBridge.SendAsync(
            "session.open", null,
            new { filePath = path, show, timeoutSeconds, pageName, pageIndex },
            timeoutSeconds
        ).GetAwaiter().GetResult();

        if (!response.Success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errorMessage = response.ErrorMessage ?? "Failed to open session",
                filePath = path,
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        return TransformSessionResponse(response.Result, path);
    }

    private static string CloseSession(string sessionId, bool save)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("session_id is required for 'close' action");

        var response = ServiceBridge.ServiceBridge.SendAsync(
            "session.close", sessionId, new { save }
        ).GetAwaiter().GetResult();

        if (!response.Success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                session_id = sessionId,
                errorMessage = response.ErrorMessage ?? "Failed to close session",
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        return response.Result ?? JsonSerializer.Serialize(new
        {
            success = true,
            session_id = sessionId,
            saved = save
        }, VisioToolsBase.JsonOptions);
    }

    private static string CreateSession(string path, bool show, int timeoutSeconds, string? pageName, int? pageIndex)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is required for 'create' action");

        var pathError = VisioToolsBase.ValidateWindowsPath(path);
        if (pathError != null) return pathError;

        var response = ServiceBridge.ServiceBridge.SendAsync(
            "session.create", null,
            new { filePath = path, show, timeoutSeconds, pageName, pageIndex },
            timeoutSeconds
        ).GetAwaiter().GetResult();

        if (!response.Success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errorMessage = response.ErrorMessage ?? "Failed to create session",
                filePath = path,
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        return TransformSessionResponse(response.Result, path);
    }

    private static string SaveSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("session_id is required for 'save' action");

        var response = ServiceBridge.ServiceBridge.SendAsync(
            "session.save", sessionId
        ).GetAwaiter().GetResult();

        if (!response.Success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                session_id = sessionId,
                errorMessage = response.ErrorMessage ?? "Failed to save",
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            session_id = sessionId
        }, VisioToolsBase.JsonOptions);
    }

    private static string ListSessions()
    {
        var response = ServiceBridge.ServiceBridge.SendAsync("session.list").GetAwaiter().GetResult();

        if (!response.Success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errorMessage = response.ErrorMessage ?? "Failed to list sessions",
                isError = true
            }, VisioToolsBase.JsonOptions);
        }

        return response.Result ?? JsonSerializer.Serialize(new
        {
            success = true,
            sessions = Array.Empty<object>(),
            count = 0
        }, VisioToolsBase.JsonOptions);
    }

    private static string TestFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is required for 'test' action");

        var pathError = VisioToolsBase.ValidateWindowsPath(path);
        if (pathError != null) return pathError;

        var fileCommands = new FileCommands();
        var info = fileCommands.Test(path);

        return JsonSerializer.Serialize(new
        {
            success = info.Success,
            exists = info.Exists,
            filePath = info.FilePath,
            fileName = info.FileName,
            fileSizeBytes = info.FileSizeBytes,
            isReadOnly = info.IsReadOnly,
            isMacroEnabled = info.IsMacroEnabled,
            pageCount = info.PageCount
        }, VisioToolsBase.JsonOptions);
    }

    /// <summary>
    /// Transforms the service response to use snake_case session_id for MCP compatibility.
    /// </summary>
    private static string TransformSessionResponse(string? result, string path)
    {
        if (!string.IsNullOrEmpty(result))
        {
            try
            {
                using var doc = JsonDocument.Parse(result);
                if (doc.RootElement.TryGetProperty("sessionId", out var sessionIdProp))
                {
                    var sessionId = sessionIdProp.GetString();
                    string? filePath = doc.RootElement.TryGetProperty("filePath", out var fp) ? fp.GetString() : path;
                    string? documentPath = doc.RootElement.TryGetProperty("documentPath", out var dp) ? dp.GetString() : filePath;
                    string? pageName = doc.RootElement.TryGetProperty("pageName", out var pn) ? pn.GetString() : null;
                    int? pageIndex = doc.RootElement.TryGetProperty("pageIndex", out var pi) && pi.ValueKind == JsonValueKind.Number
                        ? pi.GetInt32()
                        : null;
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        session_id = sessionId,
                        filePath,
                        documentPath,
                        pageName,
                        pageIndex
                    }, VisioToolsBase.JsonOptions);
                }
            }
            catch (JsonException) { }
            return result;
        }

        return JsonSerializer.Serialize(new { success = true, filePath = path, documentPath = path }, VisioToolsBase.JsonOptions);
    }
}

