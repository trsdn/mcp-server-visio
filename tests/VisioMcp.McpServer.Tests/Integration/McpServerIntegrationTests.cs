// Copyright (c) Sbroenne.
// Copyright (c) 2026 Torsten Mahr. All rights reserved.
// Licensed under the MIT License.

using System.IO.Pipelines;
using System.Text.Json;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using VisioMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

// Avoid namespace conflict: McpServer is both a type and namespace
using Server = ModelContextProtocol.Server;

namespace VisioMcp.McpServer.Tests.Integration;

/// <summary>
/// Integration tests that exercise the full MCP protocol using in-memory transport.
/// These tests use the official MCP SDK client to connect to our server, ensuring:
/// - DI pipeline is correctly configured
/// - Tool discovery via WithToolsFromAssembly() works
/// - Tool schemas are correctly generated
/// - Tools execute properly through the MCP protocol
///
/// This is the CORRECT way to test MCP servers - using the SDK's client to verify
/// the actual protocol behavior, not reflection or direct method calls.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Fast")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "McpProtocol")]
public class McpServerIntegrationTests(ITestOutputHelper output) : IAsyncLifetime, IAsyncDisposable
{
    private readonly Pipe _clientToServerPipe = new();
    private readonly Pipe _serverToClientPipe = new();
    private readonly CancellationTokenSource _cts = new();
    private Server.McpServer? _server;
    private McpClient? _client;
    private IServiceProvider? _serviceProvider;
    private Task? _serverTask;

    /// <summary>
    /// Expected tool names, derived from the assembly rather than hand-maintained.
    ///
    /// This is the same source of truth the server itself uses: <c>WithToolsFromAssembly()</c>
    /// discovers every <c>[McpServerToolType]</c> class and registers its <c>[McpServerTool]</c>
    /// methods. Deriving the set here means adding a public tool cannot silently break the gate,
    /// which is exactly what happened when <c>layer</c> was added and this list was not updated
    /// (see #26) — the designated CI smoke test went red and stayed red.
    ///
    /// The assertions that carry real signal are therefore not "does this list match itself" but:
    /// the derived set is non-empty and contains the known anchors (guarding against reflection
    /// silently finding nothing), every declared tool actually reaches the client over the MCP
    /// protocol, and no <see cref="HiddenLegacyToolNames"/> entry leaks onto the public surface.
    /// </summary>
    private static readonly HashSet<string> ExpectedToolNames = DiscoverDeclaredToolNames();

    /// <summary>
    /// Reflects over the MCP server assembly for tools the SDK would register.
    /// </summary>
    private static HashSet<string> DiscoverDeclaredToolNames()
    {
        var assembly = typeof(VisioFileTool).Assembly;

        var names = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(Server.McpServerToolTypeAttribute), inherit: false).Length > 0)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(m => m.GetCustomAttribute<Server.McpServerToolAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .ToHashSet(StringComparer.Ordinal);

        // A discovery routine that finds nothing must fail loudly rather than vacuously pass.
        // audit-core-coverage.ps1 reported "100% coverage" on zero discovered methods for exactly
        // this reason (#15); this gate will not repeat it.
        Assert.NotEmpty(names);
        foreach (var anchor in new[] { "file", "page", "shape", "text", "cell" })
        {
            Assert.Contains(anchor, names);
        }

        return names;
    }

    private static readonly HashSet<string> HiddenLegacyToolNames =
    [
        "accessibility",
        "comment",
        "design",
        "headerfooter",
        // hyperlink was reimplemented on Shape.Hyperlinks in #35 and master on Document.Masters
        // in #34; both are public.
        "image",
        "printoptions",
        "tag",
        "vba"
    ];

    /// <summary>
    /// Setup: Create MCP server with DI and connect client via in-memory pipes.
    /// This exercises the exact same code path as Program.cs.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Build the server with DI - same pattern as Program.cs
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Add MCP server with tools using stream transport for testing
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "VisioMcp-Test", Version = "1.0.0" };
                options.ServerInstructions = "Test server for integration tests";
            })
            .WithStreamServerTransport(
                _clientToServerPipe.Reader.AsStream(),
                _serverToClientPipe.Writer.AsStream())
            .WithToolsFromAssembly(typeof(VisioFileTool).Assembly);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);

        // Get the server and start it
        _server = _serviceProvider.GetRequiredService<Server.McpServer>();
        _serverTask = _server.RunAsync(_cts.Token);

        // Create client connected to the server via pipes
        _client = await McpClient.CreateAsync(
            new StreamClientTransport(
                serverInput: _clientToServerPipe.Writer.AsStream(),
                serverOutput: _serverToClientPipe.Reader.AsStream()),
            clientOptions: new McpClientOptions
            {
                ClientInfo = new() { Name = "TestClient", Version = "1.0.0" }
            },
            cancellationToken: _cts.Token);

        output.WriteLine($"✓ Connected to server: {_client.ServerInfo?.Name} v{_client.ServerInfo?.Version}");
    }

    public async Task DisposeAsync()
    {
        await DisposeAsyncCore();
    }

    // Explicit IAsyncDisposable implementation to satisfy CA1001 analyzer
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeAsyncCore()
    {
        await _cts.CancelAsync();

        _clientToServerPipe.Writer.Complete();
        _serverToClientPipe.Writer.Complete();

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _cts.Dispose();
    }

    /// <summary>
    /// Canonical MCP smoke test used by pre-commit.
    /// Verifies that all expected tools are discoverable through the real MCP protocol surface.
    /// This is THE definitive test - it uses client.ListToolsAsync() which exercises:
    /// - DI pipeline
    /// - WithToolsFromAssembly() discovery
    /// - MCP protocol serialization
    /// - Tool schema generation
    /// </summary>
    [Fact]
    public async Task SmokeTest_AllTools_E2EWorkflow()
    {
        output.WriteLine("=== TOOL DISCOVERY VIA MCP PROTOCOL ===\n");

        // Act - Use the REAL MCP protocol to list tools
        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);

        // Assert - Verify count
        output.WriteLine($"Discovered {tools.Count} tools via MCP protocol:\n");

        foreach (var tool in tools.OrderBy(t => t.Name))
        {
            var descPreview = tool.Description?.Length > 60 ? tool.Description[..60] + "..." : tool.Description;
            output.WriteLine($"  • {tool.Name}: {descPreview}");
        }

        Assert.Equal(ExpectedToolNames.Count, tools.Count);

        // Verify all expected tools are present
        var actualToolNames = tools.Select(t => t.Name).ToHashSet();

        var missingTools = ExpectedToolNames.Except(actualToolNames).ToList();
        if (missingTools.Count > 0)
        {
            output.WriteLine($"\n❌ Missing tools: {string.Join(", ", missingTools)}");
        }
        Assert.Empty(missingTools);

        var unexpectedTools = actualToolNames.Except(ExpectedToolNames).ToList();
        if (unexpectedTools.Count > 0)
        {
            output.WriteLine($"\n❌ Unexpected tools: {string.Join(", ", unexpectedTools)}");
        }
        Assert.Empty(unexpectedTools);

        var leakedLegacyTools = actualToolNames.Intersect(HiddenLegacyToolNames).ToList();
        if (leakedLegacyTools.Count > 0)
        {
            output.WriteLine($"\n❌ Legacy tools still public: {string.Join(", ", leakedLegacyTools)}");
        }
        Assert.Empty(leakedLegacyTools);

        output.WriteLine($"\n✓ All {ExpectedToolNames.Count} tools discovered successfully via MCP protocol");
    }

    /// <summary>
    /// Tests that each tool has proper schema (parameters, descriptions).
    /// </summary>
    [Fact]
    public async Task ListTools_AllToolsHaveValidSchema()
    {
        output.WriteLine("=== TOOL SCHEMA VALIDATION ===\n");

        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);

        foreach (var tool in tools)
        {
            // Every tool must have a name
            Assert.False(string.IsNullOrEmpty(tool.Name), "Tool has empty name");

            // Every tool should have a description
            Assert.False(string.IsNullOrEmpty(tool.Description), $"Tool {tool.Name} has no description");

            // McpClientTool implements AIFunction which has Parameters property
            // The SDK generates schema from tool methods

            output.WriteLine($"✓ {tool.Name}: Has description ({tool.Description?.Length} chars)");
        }

        output.WriteLine($"\n✓ All {tools.Count} tools have valid schemas");
    }

    /// <summary>
    /// Tests that file tool's Test action works via MCP protocol.
    /// This exercises the complete tool invocation path.
    /// </summary>
    [Fact]
    public async Task CallTool_PptFileTest_ReturnsSuccess()
    {
        output.WriteLine("=== TOOL INVOCATION VIA MCP PROTOCOL ===\n");

        // Arrange - Test action doesn't require an actual file
        // Parameter names shortened for token optimization: documentPath -> path
        var arguments = new Dictionary<string, object?>
        {
            ["action"] = "test",
            ["path"] = "C:\\fake\\test.pptx"
        };

        // Act - Call tool via MCP protocol
        var result = await _client!.CallToolAsync(
            "file",
            arguments,
            cancellationToken: _cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        // Get text content - need to cast from ContentBlock base class
        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);

        var textPreview = textBlock.Text.Length > 200 ? textBlock.Text[..200] + "..." : textBlock.Text;
        output.WriteLine($"Tool response: {textPreview}");

        // The test action should return success (property name is "success" in success responses)
        Assert.Contains("success", textBlock.Text.ToLowerInvariant());

        output.WriteLine("\n✓ file Test action executed successfully via MCP protocol");
    }

    /// <summary>
    /// Tests that server information is correctly exposed via MCP protocol.
    /// </summary>
    [Fact]
    public async Task ServerInfo_ReturnsCorrectInformation()
    {
        output.WriteLine("=== SERVER INFO VIA MCP PROTOCOL ===\n");

        // Act - Server info is available after connection
        var serverInfo = _client!.ServerInfo;
        var serverInstructions = _client.ServerInstructions;

        // Assert
        Assert.NotNull(serverInfo);
        Assert.Equal("VisioMcp-Test", serverInfo.Name);
        Assert.Equal("1.0.0", serverInfo.Version);
        Assert.Equal("Test server for integration tests", serverInstructions);

        output.WriteLine($"Server Name: {serverInfo.Name}");
        output.WriteLine($"Server Version: {serverInfo.Version}");
        output.WriteLine($"Server Instructions: {serverInstructions}");

        output.WriteLine("\n✓ Server info correctly exposed via MCP protocol");
        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <summary>
    /// Tests that all tools can be discovered and iterated via ListToolsAsync.
    /// Note: SDK 0.5.0+ replaced EnumerateToolsAsync with ListToolsAsync.
    /// </summary>
    [Fact]
    public async Task ListTools_CanIterateAllTools()
    {
        output.WriteLine("=== TOOL ITERATION ===\n");

        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);
        var toolCount = 0;
        foreach (var tool in tools)
        {
            toolCount++;
            output.WriteLine($"  Discovered: {tool.Name}");
        }

        Assert.Equal(ExpectedToolNames.Count, toolCount);

        output.WriteLine($"\n✓ Iterated {toolCount} tools");
    }

    /// <summary>
    /// Tests that server capabilities include tools.
    /// </summary>
    [Fact]
    public void ServerCapabilities_IncludesTools()
    {
        output.WriteLine("=== SERVER CAPABILITIES ===\n");

        var capabilities = _client!.ServerCapabilities;

        Assert.NotNull(capabilities);
        Assert.NotNull(capabilities.Tools);

        output.WriteLine($"✓ Tools capability: {capabilities.Tools != null}");
        output.WriteLine($"✓ ListChanged: {capabilities.Tools?.ListChanged}");

        output.WriteLine("\n✓ Server capabilities correctly exposed");
    }

    /// <summary>
    /// REGRESSION (#19): no publicly listed tool may fail with <c>RuntimeBinderException</c>
    /// against a valid <c>.vsdx</c> session.
    ///
    /// That exception means the tool called a COM member the object does not have — in practice,
    /// PowerPoint-era code reaching for <c>Slides</c> or <c>SlideMasters</c> on a Visio Document.
    /// It is an opaque failure for an LLM: the tool was advertised with a confident description,
    /// selected in good faith, and returned an error naming a .NET binder type.
    ///
    /// The assertion is deliberately narrow. A tool returning a *validation* error ("page_index is
    /// required") is fine — the surface is honest about what it needs. A tool that binds against
    /// the wrong object model is not.
    /// </summary>
    [Fact]
    public async Task AllPublicTools_DoNotThrowRuntimeBinderException_AgainstVsdx()
    {
        output.WriteLine("=== RUNTIME BINDER REGRESSION SWEEP ===\n");

        var tempPath = Path.Join(Path.GetTempPath(), $"BinderSweep_{Guid.NewGuid():N}.vsdx");
        string? sessionId = null;

        try
        {
            var createResponse = await CallToolTextAsync("file", new Dictionary<string, object?>
            {
                ["action"] = "create",
                ["path"] = tempPath,
                ["show"] = false
            });

            using (var createDoc = JsonDocument.Parse(createResponse))
            {
                Assert.True(
                    createDoc.RootElement.TryGetProperty("session_id", out var sid),
                    $"Could not create a session for the sweep. Response: {createResponse}");
                sessionId = sid.GetString();
            }

            Assert.False(string.IsNullOrWhiteSpace(sessionId));

            var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);
            var offenders = new List<string>();

            foreach (var tool in tools.OrderBy(t => t.Name))
            {
                // 'file' is the session tool itself and was already exercised above.
                if (tool.Name == "file")
                {
                    continue;
                }

                // A read-only enumeration is the cheapest action that still forces the tool to
                // bind against the document object model.
                var response = await CallToolTextAsync(tool.Name, new Dictionary<string, object?>
                {
                    ["action"] = "list",
                    ["session_id"] = sessionId,
                    ["page_index"] = 1
                });

                if (response.Contains("RuntimeBinderException", StringComparison.Ordinal))
                {
                    offenders.Add($"{tool.Name}: {response}");
                    output.WriteLine($"  ✗ {tool.Name}");
                }
                else
                {
                    var preview = response.Length > 90 ? response[..90] + "..." : response;
                    output.WriteLine($"  ✓ {tool.Name}: {preview}");
                }
            }

            Assert.True(
                offenders.Count == 0,
                "Publicly listed tools failed with RuntimeBinderException against a valid .vsdx:\n"
                + string.Join("\n", offenders));

            output.WriteLine($"\n✓ All {tools.Count - 1} non-session tools bound successfully");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await CallToolTextAsync("file", new Dictionary<string, object?>
                {
                    ["action"] = "close",
                    ["session_id"] = sessionId,
                    ["save"] = false
                });
            }

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>Calls a tool over the MCP protocol and returns its text payload.</summary>
    private async Task<string> CallToolTextAsync(string toolName, Dictionary<string, object?> arguments)
    {
        var result = await _client!.CallToolAsync(toolName, arguments, cancellationToken: _cts.Token);
        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        return textBlock?.Text ?? string.Empty;
    }
}




