#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks that every property on a hand-written CLI command's Settings class is actually read.

.DESCRIPTION
    Detects the pattern where a developer adds a Settings property but never reads it, so the
    value the user supplied on the command line is silently dropped.

    Scope: hand-written commands under src/VisioMcp.CLI/Commands only. The per-domain CLI
    commands are emitted by CliSettingsGenerator from the same [ServiceCategory] interfaces that
    produce the daemon routing, so their settings cannot drift out of sync by construction.

.EXAMPLE
    .\check-cli-settings-usage.ps1

.NOTES
    Part of pre-commit checks. Fails if any hand-written CLI command has an unread Settings property.
#>

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
$cliCommandsDir = Join-Path $rootDir "src\VisioMcp.CLI\Commands"

# Properties that are legitimately not passed to the daemon (session management, meta properties)
$globalExclusions = @(
    "Action",
    "SessionId"
)

function Get-SettingsProperties {
    param([string]$content)

    $properties = @()

    # Locate the Settings class and take ONLY its body by brace matching.
    #
    # The previous version used '(?s)internal sealed class Settings[^{]*\{(.+)$', whose (.+)$
    # under RegexOptions.Singleline swallowed the rest of the file. In BatchCommand.cs that
    # pulled in the BatchEntry and BatchResult DTOs declared below it, so their properties
    # (Command, Args, Index, Success, Result, Error) were reported as "unused Settings
    # properties" - a false positive that failed the gate on a clean checkout.
    $classMatch = [regex]::Match($content, 'class\s+Settings\b[^{]*\{')
    if (-not $classMatch.Success) { return $properties }

    $start = $classMatch.Index + $classMatch.Length
    $depth = 1
    $end = $start

    while ($end -lt $content.Length -and $depth -gt 0) {
        $ch = $content[$end]
        if ($ch -eq '{') { $depth++ }
        elseif ($ch -eq '}') { $depth-- }
        $end++
    }

    if ($depth -ne 0) { return $properties }

    $settingsBlock = $content.Substring($start, $end - $start - 1)

    foreach ($match in [regex]::Matches($settingsBlock, 'public\s+[\w<>,\[\]\.]+\??\s+(\w+)\s*\{\s*get')) {
        $properties += $match.Groups[1].Value
    }

    return $properties
}

$issues = @()
$totalChecked = 0
$totalPassed = 0

function Get-UsedProperties {
    param([string]$content)

    $usedProps = @()
    # Find all settings.PropertyName usages
    $usageMatches = [regex]::Matches($content, 'settings\.(\w+)')
    foreach ($match in $usageMatches) {
        $usedProps += $match.Groups[1].Value
    }
    return $usedProps | Sort-Object -Unique
}

Write-Host "Checking CLI Settings property usage..." -ForegroundColor Cyan
Write-Host ""

$commandFiles = Get-ChildItem -Path $cliCommandsDir -Filter "*Command.cs" -File

foreach ($file in $commandFiles) {
    # Skip ListActionsCommand - it's a meta command
    if ($file.Name -eq "ListActionsCommand.cs") {
        continue
    }

    $content = Get-Content $file.FullName -Raw
    $fileName = $file.Name

    # Skip if no Settings class
    if (-not ($content -match 'internal sealed class Settings')) {
        continue
    }

    $totalChecked++

    $settingsProps = Get-SettingsProperties $content
    $usedProps = Get-UsedProperties $content

    $unusedProps = @()
    foreach ($prop in $settingsProps) {
        # Skip global exclusions
        if ($globalExclusions -contains $prop) {
            continue
        }

        # Check if property is used
        if ($usedProps -notcontains $prop) {
            $unusedProps += $prop
        }
    }

    if ($unusedProps.Count -gt 0) {
        $issues += [PSCustomObject]@{
            File = $fileName
            UnusedProperties = $unusedProps -join ", "
        }
    }
    else {
        $totalPassed++
    }
}

if ($issues.Count -gt 0) {
    Write-Host "Found CLI commands with unused Settings properties:" -ForegroundColor Red
    Write-Host ""

    foreach ($issue in $issues) {
        Write-Host "   $($issue.File)" -ForegroundColor Yellow
        Write-Host "      Unused: $($issue.UnusedProperties)" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "   These Settings properties are defined but never read." -ForegroundColor Red
    Write-Host "   Values the user supplies on the command line will be silently ignored." -ForegroundColor Red
    Write-Host ""
    Write-Host "   Fix: read the property in ExecuteAsync, or delete it." -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

Write-Host "CLI Settings usage check passed - $totalPassed/$totalChecked hand-written commands" -ForegroundColor Green
Write-Host "   Every Settings property is read somewhere in its command" -ForegroundColor Gray
exit 0
