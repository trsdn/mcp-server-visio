using System.ComponentModel;
using System.Text.Json;
using VisioMcp.CLI.Infrastructure;
using VisioMcp.Service;
using Spectre.Console;
using Spectre.Console.Cli;

namespace VisioMcp.CLI.Commands;

// ============================================================================
// SESSION COMMANDS
// ============================================================================

internal sealed class SessionCreateCommand : AsyncCommand<SessionCreateCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            AnsiConsole.MarkupLine("[red]File path is required.[/]");
            return 1;
        }

        using var client = await DaemonAutoStart.EnsureAndConnectAsync(cancellationToken);
        var response = await client.SendAsync(new ServiceRequest
        {
            Command = "session.create",
            Args = JsonSerializer.Serialize(new
            {
                filePath = settings.FilePath,
                show = settings.Show,
                pageName = settings.PageName,
                pageIndex = settings.PageIndex,
                timeoutSeconds = settings.TimeoutSeconds
            }, ServiceProtocol.JsonOptions)
        }, cancellationToken);

        if (response.Success)
        {
            Console.WriteLine(response.Result);
            return 0;
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = response.ErrorMessage }, ServiceProtocol.JsonOptions));
            return 1;
        }
    }

    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to the new file to create")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("--page-name <NAME>")]
        [Description("Optional initial page name to associate with the session metadata")]
        public string? PageName { get; init; }

        [CommandOption("--show")]
        [Description("Show the Visio application window while the session is open")]
        public bool Show { get; init; }

        [CommandOption("--page-index <INDEX>")]
        [Description("Optional initial 1-based page index to associate with the session metadata")]
        public int? PageIndex { get; init; }

        [CommandOption("--timeout <SECONDS>")]
        [Description("Session timeout in seconds")]
        public int? TimeoutSeconds { get; init; }
    }
}

internal sealed class SessionOpenCommand : AsyncCommand<SessionOpenCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            AnsiConsole.MarkupLine("[red]File path is required.[/]");
            return 1;
        }

        using var client = await DaemonAutoStart.EnsureAndConnectAsync(cancellationToken);
        var response = await client.SendAsync(new ServiceRequest
        {
            Command = "session.open",
            Args = JsonSerializer.Serialize(new
            {
                filePath = settings.FilePath,
                show = settings.Show,
                pageName = settings.PageName,
                pageIndex = settings.PageIndex,
                timeoutSeconds = settings.TimeoutSeconds
            }, ServiceProtocol.JsonOptions)
        }, cancellationToken);

        if (response.Success)
        {
            Console.WriteLine(response.Result);
            return 0;
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = response.ErrorMessage }, ServiceProtocol.JsonOptions));
            return 1;
        }
    }

    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to the file to open")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("--page-name <NAME>")]
        [Description("Optional initial page name to associate with the session metadata")]
        public string? PageName { get; init; }

        [CommandOption("--show")]
        [Description("Show the Visio application window while the session is open")]
        public bool Show { get; init; }

        [CommandOption("--page-index <INDEX>")]
        [Description("Optional initial 1-based page index to associate with the session metadata")]
        public int? PageIndex { get; init; }

        [CommandOption("--timeout <SECONDS>")]
        [Description("Session timeout in seconds")]
        public int? TimeoutSeconds { get; init; }
    }
}

internal sealed class SessionCloseCommand : AsyncCommand<SessionCloseCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.SessionId))
        {
            AnsiConsole.MarkupLine("[red]Session ID is required.[/]");
            return 1;
        }

        var save = settings.Save.GetValueOrDefault();

        using var client = await DaemonAutoStart.EnsureAndConnectAsync(cancellationToken);
        var response = await client.SendAsync(new ServiceRequest
        {
            Command = "session.close",
            SessionId = settings.SessionId,
            Args = JsonSerializer.Serialize(new { save }, ServiceProtocol.JsonOptions)
        }, cancellationToken);

        if (response.Success)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = true, message = save ? "Session closed and saved." : "Session closed." }, ServiceProtocol.JsonOptions));
            return 0;
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = response.ErrorMessage }, ServiceProtocol.JsonOptions));
            return 1;
        }
    }

    internal sealed class Settings : CommandSettings
    {
        [CommandOption("-s|--session <SESSION>")]
        [Description("Session ID to close")]
        public string SessionId { get; init; } = string.Empty;

        [CommandOption("--save [SAVE]")]
        [Description("Save changes before closing. Supports --save, --save true, or --save false")]
        public OptionalBoolFlagValue Save { get; init; } = new();
    }
}

internal sealed class OptionalBoolFlagValue : IFlagValue
{
    public bool IsSet { get; set; }

    public Type Type => typeof(bool);

    public object? Value { get; set; }

    public bool GetValueOrDefault(bool defaultValue = false)
    {
        if (!IsSet)
        {
            return defaultValue;
        }

        if (Value is null)
        {
            return true;
        }

        return Value switch
        {
            bool flag => flag,
            string text when bool.TryParse(text, out var parsed) => parsed,
            _ => true
        };
    }
}

internal sealed class SessionListCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var pipeName = DaemonAutoStart.GetPipeName();
        using var client = new ServiceClient(pipeName, connectTimeout: TimeSpan.FromSeconds(2));

        try
        {
            var response = await client.SendAsync(new ServiceRequest { Command = "session.list" }, cancellationToken);
            if (response.Success)
            {
                Console.WriteLine(response.Result);
                return 0;
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = response.ErrorMessage }, ServiceProtocol.JsonOptions));
                return 1;
            }
        }
        catch (Exception)
        {
            // Daemon not running — no sessions
            Console.WriteLine(JsonSerializer.Serialize(new { sessions = Array.Empty<object>() }, ServiceProtocol.JsonOptions));
            return 0;
        }
    }
}

internal sealed class SessionSaveCommand : AsyncCommand<SessionSaveCommand.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.SessionId))
        {
            AnsiConsole.MarkupLine("[red]Session ID is required.[/]");
            return 1;
        }

        using var client = await DaemonAutoStart.EnsureAndConnectAsync(cancellationToken);
        var response = await client.SendAsync(new ServiceRequest
        {
            Command = "session.save",
            SessionId = settings.SessionId
        }, cancellationToken);

        if (response.Success)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = true, message = "Session saved." }, ServiceProtocol.JsonOptions));
            return 0;
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = response.ErrorMessage }, ServiceProtocol.JsonOptions));
            return 1;
        }
    }

    internal sealed class Settings : CommandSettings
    {
        [CommandOption("-s|--session <SESSION>")]
        [Description("Session ID to save")]
        public string SessionId { get; init; } = string.Empty;
    }
}



