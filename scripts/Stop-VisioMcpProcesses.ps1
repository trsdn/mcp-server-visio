<#
.SYNOPSIS
    Stops the VisioMcp Service gracefully and kills Visio processes before build.
.DESCRIPTION
    Pre-build cleanup script that:
    1. Gracefully stops the VisioMcp Service via named pipe (service.shutdown)
    2. Kills any remaining Visio (VISIO.EXE) processes

    This prevents file locking issues during build when the service or Visio
    holds handles to assemblies or documents.
.NOTES
    Called from Directory.Build.props as a BeforeBuild target.
    Safe to run when no processes are running (silently succeeds).
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = 'SilentlyContinue'

function Write-Status($message) {
    if ($Verbose) {
        Write-Host "  [pre-build] $message" -ForegroundColor DarkGray
    }
}

# ----------------------------------------------
# 1. Gracefully stop VisioMcp Service via CLI
# ----------------------------------------------
function Stop-VisioMcpService {
    # Look for visiocli in build output. Globbed rather than naming the target framework: this
    # listed only net10.0-windows while the project targets net9.0-windows, so it never found the
    # CLI and silently skipped the graceful service stop (#57).
    $scriptDir = Split-Path -Parent $PSScriptRoot  # repo root
    $cliBin = "$scriptDir\src\VisioMcp.CLI\bin"
    $visiocli = @("Release", "Debug") |
        ForEach-Object { Join-Path $cliBin $_ } |
        Where-Object { Test-Path $_ } |
        ForEach-Object { Get-ChildItem -Path $_ -Filter "visiocli.exe" -Recurse -ErrorAction SilentlyContinue } |
        Select-Object -First 1 -ExpandProperty FullName

    if ($visiocli) {
        Write-Status "Using CLI: $visiocli"
        $output = & $visiocli service stop --quiet 2>&1
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0) {
            # Parse JSON to check if service was running
            try {
                $result = $output | ConvertFrom-Json
                if ($result.message -eq 'Service is not running.') {
                    Write-Status "VisioMcp Service was not running"
                } else {
                    Write-Host "  VisioMcp Service stopped gracefully" -ForegroundColor Green
                }
            } catch {
                Write-Status "Service stop completed (exit code 0)"
            }
        } else {
            Write-Status "CLI service stop returned exit code $exitCode, falling back to process kill"
            Stop-VisioMcpServiceFallback
        }
    } else {
        Write-Status "visiocli not found (first build?), using fallback"
        Stop-VisioMcpServiceFallback
    }
}

function Stop-VisioMcpServiceFallback {
    # Fallback: direct named pipe shutdown (works without CLI binary)
    $sid = ([System.Security.Principal.WindowsIdentity]::GetCurrent()).User.Value
    $pipeName = "VisioMcp-$sid"

    $pipeExists = Test-Path "\\.\pipe\$pipeName"
    if (-not $pipeExists) {
        Write-Status "VisioMcp Service not running (no pipe found)"
        return
    }

    Write-Status "VisioMcp Service detected, sending shutdown via pipe..."
    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $pipe.Connect(3000)

        $writer = New-Object System.IO.StreamWriter($pipe, [System.Text.Encoding]::UTF8, 4096)
        $writer.AutoFlush = $true
        $reader = New-Object System.IO.StreamReader($pipe, [System.Text.Encoding]::UTF8)

        $writer.WriteLine('{"Command":"service.shutdown"}')
        $response = $reader.ReadLine()
        Write-Status "Service response: $response"

        $reader.Dispose()
        $writer.Dispose()
        $pipe.Dispose()

        Start-Sleep -Milliseconds 500
        Write-Host "  VisioMcp Service stopped gracefully" -ForegroundColor Green
    }
    catch {
        Write-Status "Could not connect to pipe: $($_.Exception.Message)"
        $serviceProcs = Get-Process -Name 'VisioMcp.McpServer', 'VisioMcp.Service' -ErrorAction SilentlyContinue
        if ($serviceProcs) {
            $serviceProcs | Stop-Process -Force -ErrorAction SilentlyContinue
            Write-Host "  VisioMcp Service processes killed (pipe unavailable)" -ForegroundColor Yellow
        }
    }
}

# ----------------------------------------------
# 2. Kill Visio processes
# ----------------------------------------------
function Stop-VisioProcesses {
    $visioProcs = Get-Process -Name 'VISIO' -ErrorAction SilentlyContinue
    if ($visioProcs) {
        $count = $visioProcs.Count
        $visioProcs | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
        Write-Host "  Killed $count Visio process(es)" -ForegroundColor Yellow
    }
    else {
        Write-Status "No Visio processes running"
    }
}

# ----------------------------------------------
# Run cleanup
# ----------------------------------------------
Stop-VisioMcpService
Stop-VisioProcesses
