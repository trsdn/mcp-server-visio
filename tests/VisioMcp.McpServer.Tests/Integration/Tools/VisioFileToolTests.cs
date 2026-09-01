// Copyright (c) 2026 Torsten Mahr. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.McpServer.Tests.Integration.Tools;

/// <summary>
/// Tests for VisioFileTool action methods.
/// These tests call the tool methods directly without MCP transport.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Fast")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "File")]
public class VisioFileToolTests(ITestOutputHelper output)
{
    [Fact]
    public void Create_ProtectedSystemPath_ReturnsJsonError()
    {
        // Arrange - path that reliably fails (Windows directory is protected)
        var protectedPath = @"C:\Windows\HelloWorld.vsdx";

        // Act
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Create,
            path: protectedPath,
            session_id: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        output.WriteLine($"Result: {result}");

        // Assert - should return JSON error, not crash the server
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result).RootElement;
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("errorMessage", out var errorMsg));
        // Error message may vary based on Visio version and system locale
        var msg = errorMsg.GetString();
        Assert.True(msg!.Contains("Failed") || msg.Contains("Cannot"), $"Expected failure message, got: {msg}");
        Assert.True(json.TryGetProperty("isError", out var isError));
        Assert.True(isError.GetBoolean());
    }

    [Fact]
    public void Create_InvalidPath_ReturnsJsonError()
    {
        // Arrange - use a path that will fail (System32, no permission)
        var invalidPath = @"C:\Windows\System32\test.vsdx";

        // Act
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Create,
            path: invalidPath,
            session_id: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        output.WriteLine($"Result: {result}");

        // Assert - should return JSON error, not crash
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result).RootElement;
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("errorMessage", out var errorMsg));
        // Error message may vary based on Visio version and system locale
        var msg = errorMsg.GetString();
        Assert.True(msg!.Contains("Failed") || msg.Contains("Cannot"), $"Expected failure message, got: {msg}");
        Assert.True(json.TryGetProperty("isError", out var isError));
        Assert.True(isError.GetBoolean());
    }

    [Fact]
    public void Create_NullPath_ReturnsJsonError()
    {
        // Act - null path should be caught and returned as JSON error
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Create,
            path: null,
            session_id: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        output.WriteLine($"Result: {result}");

        // Assert - should return JSON error (ExecuteToolAction wraps exceptions)
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result).RootElement;

        // ExecuteToolAction uses "success" and "errorMessage" for error responses
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("errorMessage", out var errorMsg));
        Assert.Contains("path is required", errorMsg.GetString());
    }

    [Fact]
    public void Create_ValidPath_ReturnsSuccessWithSessionId()
    {
        // Arrange - use temp directory
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioFileToolTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            // Act
            var result = VisioFileTool.VisioFile(
                VisioFileAction.Create,
                path: tempPath,
                session_id: null,
                save: false,
                show: false,
                timeout_seconds: 300);

            output.WriteLine($"Result: {result}");

            // Assert
            Assert.NotNull(result);
            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(File.Exists(tempPath), "File should have been created");
            Assert.True(json.TryGetProperty("session_id", out var sessionIdElement));
            sessionId = sessionIdElement.GetString();
            Assert.NotNull(sessionId);
        }
        finally
        {
            // Cleanup - close session first
            if (!string.IsNullOrEmpty(sessionId))
            {
                VisioFileTool.VisioFile(
                    VisioFileAction.Close,
                    path: null,
                    session_id: sessionId,
                    save: false,
                    show: false,
                    timeout_seconds: 300);
            }

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Create_WithPageTarget_ReturnsDocumentAndPageMetadata()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioFileToolPageTargetTest_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            var result = VisioFileTool.VisioFile(
                VisioFileAction.Create,
                path: tempPath,
                session_id: null,
                page_name: "Overview",
                page_index: 3,
                save: false,
                show: false,
                timeout_seconds: 300);

            output.WriteLine($"Result: {result}");

            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            sessionId = json.GetProperty("session_id").GetString();
            Assert.Equal(tempPath, json.GetProperty("filePath").GetString());
            Assert.Equal(tempPath, json.GetProperty("documentPath").GetString());
            Assert.Equal("Overview", json.GetProperty("pageName").GetString());
            Assert.Equal(3, json.GetProperty("pageIndex").GetInt32());
        }
        finally
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                VisioFileTool.VisioFile(
                    VisioFileAction.Close,
                    path: null,
                    session_id: sessionId,
                    page_name: null,
                    page_index: null,
                    save: false,
                    show: false,
                    timeout_seconds: 300);
            }

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Test_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var fakePath = @"C:\NonExistent\fake.vsdx";

        // Act
        var result = VisioFileTool.VisioFile(
            VisioFileAction.Test,
            path: fakePath,
            session_id: null,
            save: false,
            show: false,
            timeout_seconds: 300);

        output.WriteLine($"Result: {result}");

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result).RootElement;
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(json.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public void Test_ExistingVsdmFile_ReturnsVisioMacroMetadata()
    {
        var tempPath = Path.Join(Path.GetTempPath(), $"VisioFileToolMacroTest_{Guid.NewGuid():N}.vsdm");

        try
        {
            File.WriteAllText(tempPath, "test");

            var result = VisioFileTool.VisioFile(
                VisioFileAction.Test,
                path: tempPath,
                session_id: null,
                save: false,
                show: false,
                timeout_seconds: 300);

            output.WriteLine($"Result: {result}");

            var json = JsonDocument.Parse(result).RootElement;
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("exists").GetBoolean());
            Assert.True(json.GetProperty("isMacroEnabled").GetBoolean());
            Assert.Equal(-1, json.GetProperty("pageCount").GetInt32());
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}





