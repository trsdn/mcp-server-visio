#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-time bootstrap for a fresh clone: installs the Git hooks.

.DESCRIPTION
    Points Git at the committed .githooks directory:

        git config core.hooksPath .githooks

    Unlike copying a file into .git/hooks, this survives future changes to the hook - every clone
    that runs this once picks up subsequent updates automatically, because the hook itself is
    version-controlled.

    Safe to run repeatedly.

.EXAMPLE
    .\scripts\Install-GitHooks.ps1

.NOTES
    Added for #17. The hook was previously documented but never installed: .git/hooks contained
    only the stock *.sample files, so no check ran on commit anywhere.
#>

param(
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Not inside a Git repository.' -ForegroundColor Red
    exit 1
}

if ($Uninstall) {
    git config --unset core.hooksPath 2>$null
    Write-Host 'Removed core.hooksPath. Git hooks are no longer installed.' -ForegroundColor Yellow
    exit 0
}

$hooksDir = Join-Path $repoRoot '.githooks'
if (-not (Test-Path $hooksDir)) {
    Write-Host "Expected hooks directory not found: $hooksDir" -ForegroundColor Red
    exit 1
}

git config core.hooksPath .githooks

$configured = git config --get core.hooksPath
if ($configured -ne '.githooks') {
    Write-Host "Failed to set core.hooksPath (got '$configured')." -ForegroundColor Red
    exit 1
}

Write-Host 'Git hooks installed.' -ForegroundColor Green
Write-Host "  core.hooksPath = $configured"
Write-Host ''
Write-Host 'The pre-commit hook now runs scripts/pre-commit.ps1 on every commit.'
Write-Host 'Run the checks manually at any time with:'
Write-Host '  pwsh -File scripts/pre-commit.ps1' -ForegroundColor Gray
Write-Host ''
Write-Host 'To uninstall:  .\scripts\Install-GitHooks.ps1 -Uninstall' -ForegroundColor Gray
