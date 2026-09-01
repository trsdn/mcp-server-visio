#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Audits that every Core command action reaches the generated dispatch and MCP surfaces.

.DESCRIPTION
    VisioMcp's public surface is attribute-driven source generation:

      [ServiceCategory("layer")] on an interface     -> a routed domain
      [McpTool(..., PublicSurface = false)]          -> hidden from MCP/CLI/skill surfaces
      [ServiceAction("add-shape")] on a method       -> overrides the derived action name

    From those, the generators emit:

      ServiceRegistry.<Category>.g.cs           the action enum and ToActionString
      ServiceRegistry.<Category>.Dispatch.g.cs  the RouteAction switch
      McpTool.<Category>.g.cs                   the public MCP tool (public surface only)

    This script compares the declared Core surface against what was actually generated and
    reports any action that does not reach dispatch, any public category that produced no MCP
    tool, and any suppressed category that leaked onto the public surface.

    It requires a prior build, because it reads generated output. If the generated files are
    absent, or if discovery finds nothing, it FAILS rather than reporting success.

.PARAMETER FailOnGaps
    Exit non-zero when gaps are found. On by default; pass -FailOnGaps:$false to report only.

.PARAMETER ShowDetail
    Print the per-category breakdown.

.EXAMPLE
    dotnet build VisioMcp.sln -c Debug
    .\scripts\audit-core-coverage.ps1

.NOTES
    Rewritten 2026-09-01 (#15). The previous version parsed a hand-written ToolActions.cs /
    ActionExtensions.cs model that no longer exists. It found zero methods and printed
    "No gaps detected - 100% coverage maintained!" with exit code 0 - a gate that reported
    success on an empty dataset, which is worse than no gate at all.
#>

param(
    [switch]$FailOnGaps = $true,
    [switch]$ShowDetail
)

$ErrorActionPreference = 'Stop'
$rootDir = Split-Path -Parent $PSScriptRoot

Write-Host 'Core Commands Coverage Audit' -ForegroundColor Cyan
Write-Host '============================' -ForegroundColor Cyan
Write-Host ''

function Get-DeclaredCategories {
    <#
        Reads src/VisioMcp.Core/Commands/**/I*Commands.cs and returns, per interface:
        category name, public-surface flag and the declared method names.
    #>
    param([string]$CommandsRoot)

    $results = @()

    foreach ($file in Get-ChildItem -Path $CommandsRoot -Recurse -Filter 'I*Commands.cs' -File) {
        $content = Get-Content $file.FullName -Raw

        $categoryMatch = [regex]::Match($content, '\[ServiceCategory\(\s*"(?<name>[^"]+)"')
        if (-not $categoryMatch.Success) { continue }

        # PublicSurface defaults to true; only an explicit "false" hides the domain.
        $isPublic = -not [regex]::IsMatch($content, 'PublicSurface\s*=\s*false')

        # Strip comments so signatures quoted inside XML docs are not counted as methods.
        $stripped = [regex]::Replace($content, '(?m)^\s*///.*$', '')
        $stripped = [regex]::Replace($stripped, '(?s)/\*.*?\*/', '')

        $actions = @()
        # An interface method is a signature terminated by ';' with no body.
        $methodPattern = '(?m)^[\s\t]*(?:\[[^\]]+\]\s*)*(?:[\w<>,\[\]\?\. ]+?)\s+(?<name>\w+)\s*\([^;]*?\)\s*;'
        foreach ($m in [regex]::Matches($stripped, $methodPattern)) {
            $methodName = $m.Groups['name'].Value
            if ($actions -notcontains $methodName) { $actions += $methodName }
        }

        $results += [pscustomobject]@{
            Category  = $categoryMatch.Groups['name'].Value
            Interface = $file.BaseName
            IsPublic  = $isPublic
            Actions   = $actions
        }
    }

    return $results
}

function Get-GeneratedFiles {
    param([string]$SearchRoot, [string]$Filter)

    if (-not (Test-Path $SearchRoot)) { return @() }
    return @(Get-ChildItem -Path $SearchRoot -Recurse -Filter $Filter -File -ErrorAction SilentlyContinue)
}

$commandsRoot = Join-Path $rootDir 'src\VisioMcp.Core\Commands'
if (-not (Test-Path $commandsRoot)) {
    Write-Host "FAIL: Core commands directory not found: $commandsRoot" -ForegroundColor Red
    exit 1
}

$categories = @(Get-DeclaredCategories -CommandsRoot $commandsRoot)

$coreObjRoot = Join-Path $rootDir 'src\VisioMcp.Core\obj'
$mcpObjRoot = Join-Path $rootDir 'src\VisioMcp.McpServer\obj'

$dispatchFiles = Get-GeneratedFiles -SearchRoot $coreObjRoot -Filter 'ServiceRegistry.*.Dispatch.g.cs'
$mcpToolFiles = Get-GeneratedFiles -SearchRoot $mcpObjRoot -Filter 'McpTool.*.g.cs'

# Not every public tool is generated. `file` is implemented by hand in VisioFileTool.cs because
# session lifecycle cannot be expressed as a batch-scoped Core command. Discover hand-written
# [McpServerToolType] classes so they are not reported as missing.
$handWrittenToolNames = @()
$handWrittenToolsRoot = Join-Path $rootDir 'src\VisioMcp.McpServer\Tools'
if (Test-Path $handWrittenToolsRoot) {
    foreach ($file in Get-ChildItem -Path $handWrittenToolsRoot -Recurse -Filter '*.cs' -File) {
        $toolContent = Get-Content $file.FullName -Raw
        foreach ($m in [regex]::Matches($toolContent, '\[McpServerTool\(\s*Name\s*=\s*"(?<name>[^"]+)"')) {
            $handWrittenToolNames += $m.Groups['name'].Value
        }
    }
}
$handWrittenToolNames = @($handWrittenToolNames | Sort-Object -Unique)

# ---------------------------------------------------------------------------
# Empty-discovery guards.
#
# This is the entire point of the rewrite. A parser that finds nothing must fail
# loudly rather than declare full coverage over an empty set.
# ---------------------------------------------------------------------------

$fatal = @()

if ($categories.Count -eq 0) {
    $fatal += "Discovered 0 [ServiceCategory] interfaces under $commandsRoot - the parser is broken or the layout changed."
}

$totalActions = 0
foreach ($c in $categories) { $totalActions += $c.Actions.Count }

if ($categories.Count -gt 0 -and $totalActions -eq 0) {
    $fatal += 'Discovered 0 interface methods across all categories - the method parser is broken.'
}

if ($dispatchFiles.Count -eq 0) {
    $fatal += "Discovered 0 ServiceRegistry.*.Dispatch.g.cs files under $coreObjRoot - run 'dotnet build VisioMcp.sln' first, this audit reads generated output."
}

if ($mcpToolFiles.Count -eq 0) {
    $fatal += "Discovered 0 McpTool.*.g.cs files under $mcpObjRoot - run 'dotnet build VisioMcp.sln' first."
}

if ($fatal.Count -gt 0) {
    Write-Host 'FAIL: discovery returned nothing.' -ForegroundColor Red
    Write-Host ''
    foreach ($f in $fatal) { Write-Host "  - $f" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'Refusing to report coverage on an empty dataset.' -ForegroundColor Red
    exit 1
}

# Deduplicate generated files across Debug/Release output directories.
$dispatchCategories = @($dispatchFiles |
        ForEach-Object { ($_.Name -replace '^ServiceRegistry\.', '') -replace '\.Dispatch\.g\.cs$', '' } |
        Sort-Object -Unique)

$mcpCategories = @($mcpToolFiles |
        ForEach-Object { ($_.Name -replace '^McpTool\.', '') -replace '\.g\.cs$', '' } |
        Sort-Object -Unique)

# ---------------------------------------------------------------------------
# Comparison
# ---------------------------------------------------------------------------

$gaps = @()

foreach ($cat in $categories | Sort-Object Category) {
    $pascal = (Get-Culture).TextInfo.ToTitleCase($cat.Category)

    $hasDispatch = $dispatchCategories -contains $pascal
    $hasMcpTool = $mcpCategories -contains $pascal
    $hasHandWrittenTool = $handWrittenToolNames -contains $cat.Category

    if (-not $hasDispatch) {
        $gaps += "[$($cat.Category)] declared by $($cat.Interface) but no ServiceRegistry.$pascal.Dispatch.g.cs was generated"
        continue
    }

    if ($cat.IsPublic -and -not $hasMcpTool -and -not $hasHandWrittenTool) {
        $gaps += "[$($cat.Category)] is PublicSurface but has neither a generated McpTool.$pascal.g.cs nor a hand-written [McpServerTool(Name = `"$($cat.Category)`")] - the tool is invisible to MCP clients"
    }

    if (-not $cat.IsPublic -and ($hasMcpTool -or $hasHandWrittenTool)) {
        $gaps += "[$($cat.Category)] is PublicSurface = false but an MCP tool exists - a suppressed domain leaked onto the public surface"
    }

    # Every declared interface method must appear in the dispatch switch.
    $dispatchFile = $dispatchFiles | Where-Object { $_.Name -eq "ServiceRegistry.$pascal.Dispatch.g.cs" } | Select-Object -First 1
    $dispatchContent = Get-Content $dispatchFile.FullName -Raw

    foreach ($action in $cat.Actions) {
        if ($dispatchContent -notmatch "\b$([regex]::Escape($action))\b") {
            $gaps += "[$($cat.Category)] method '$action' has no dispatch case in ServiceRegistry.$pascal.Dispatch.g.cs"
        }
    }
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

$publicCount = @($categories | Where-Object { $_.IsPublic }).Count
$hiddenCount = $categories.Count - $publicCount

Write-Host 'Summary' -ForegroundColor Cyan
Write-Host '-------'
Write-Host ("  Categories discovered : {0} ({1} public, {2} suppressed)" -f $categories.Count, $publicCount, $hiddenCount)
Write-Host ("  Interface methods     : {0}" -f $totalActions)
Write-Host ("  Dispatch files        : {0}" -f $dispatchCategories.Count)
Write-Host ("  Generated MCP tools   : {0}" -f $mcpCategories.Count)
Write-Host ("  Hand-written MCP tools: {0}{1}" -f $handWrittenToolNames.Count, $(if ($handWrittenToolNames.Count) { " ($($handWrittenToolNames -join ', '))" } else { '' }))
Write-Host ''

if ($ShowDetail) {
    Write-Host 'Categories' -ForegroundColor Cyan
    Write-Host '----------'
    foreach ($cat in $categories | Sort-Object Category) {
        $flag = if ($cat.IsPublic) { 'public    ' } else { 'suppressed' }
        Write-Host ("  {0,-16} {1}  {2,3} methods" -f $cat.Category, $flag, $cat.Actions.Count)
    }
    Write-Host ''
}

if ($gaps.Count -eq 0) {
    Write-Host "No gaps detected across $($categories.Count) categories and $totalActions methods." -ForegroundColor Green
    exit 0
}

Write-Host "$($gaps.Count) gap(s) detected:" -ForegroundColor Red
Write-Host ''
foreach ($gap in $gaps) { Write-Host "  - $gap" -ForegroundColor Red }
Write-Host ''

if ($FailOnGaps) { exit 1 }
exit 0
