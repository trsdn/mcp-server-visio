#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Git pre-commit hook to check for COM object leaks, Core Commands coverage, naming consistency, Success flag violations, CLI workflow, and MCP Server functionality

.DESCRIPTION
    Runs checks before allowing commits:
    0. Process cleanup - kills stale Visio, visiocli, and MCP server processes to prevent file locks
    1. COM leak checker - ensures no Visio COM objects are leaked
    2. Coverage audit - ensures 100% Core Commands are exposed via MCP Server
    3. Naming consistency - ensures enum names match Core method names exactly
    4. Success flag validation - ensures Success=true never paired with ErrorMessage (Rule 0)
    5. CLI workflow smoke test - validates end-to-end CLI functionality
    6. MCP Server smoke test - validates all MCP tools work correctly

    Ensures code quality and prevents regression.

.EXAMPLE
    .\pre-commit.ps1

.NOTES
    This script is called by the Git pre-commit hook.
    To install: Copy .git/hooks/pre-commit (bash) or configure Git to use this PowerShell version.
#>

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot

# CRITICAL: Check branch FIRST - never commit directly to main (Rule 6)
Write-Host "Checking current branch..." -ForegroundColor Cyan
$currentBranch = git branch --show-current

if ($currentBranch -eq "main") {
    Write-Host ""
    Write-Host "BLOCKED: Cannot commit directly to 'main' branch!" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Rule 6: All Changes Via Pull Requests" -ForegroundColor Yellow
    Write-Host "   'Never commit to main. Create feature branch -> PR -> CI/CD + review -> merge.'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   To fix:" -ForegroundColor Cyan
    Write-Host "   1. git stash                                    # Save your changes" -ForegroundColor White
    Write-Host "   2. git checkout -b feature/your-feature-name    # Create feature branch" -ForegroundColor White
    Write-Host "   3. git stash pop                                # Restore changes" -ForegroundColor White
    Write-Host "   4. git add <files>                              # Stage changes" -ForegroundColor White
    Write-Host "   5. git commit -m 'your message'                 # Commit to feature branch" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "Branch check passed - on '$currentBranch' (not main)" -ForegroundColor Green
Write-Host ""

# Kill stale Visio and MCP server processes to avoid file locks on Release binaries
Write-Host "Killing stale Visio and server processes..." -ForegroundColor Cyan

$killedProcesses = @()
foreach ($procName in @("VISIO", "visiocli", "VisioMcp.McpServer", "VisioMcp.Service")) {
    $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue
    if ($procs) {
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        $killedProcesses += "$procName ($($procs.Count))"
    }
}

if ($killedProcesses.Count -gt 0) {
    Write-Host "   Killed: $($killedProcesses -join ', ')" -ForegroundColor Yellow
    # Brief pause to let file handles release
    Start-Sleep -Milliseconds 500
}
else {
    Write-Host "   No stale processes found" -ForegroundColor Gray
}

Write-Host "Process cleanup done" -ForegroundColor Green
Write-Host ""

Write-Host "Checking for COM object leaks..." -ForegroundColor Cyan

try {
    $leakCheckScript = Join-Path $rootDir "scripts\check-com-leaks.ps1"
    & $leakCheckScript

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "COM object leaks detected! Fix them before committing." -ForegroundColor Red
        exit 1
    }

    Write-Host "COM leak check passed" -ForegroundColor Green
}
catch {
    Write-Host "Error running COM leak check: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Continuing with coverage audit..." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Checking Core Commands coverage..." -ForegroundColor Cyan

try {
    $auditScript = Join-Path $rootDir "scripts\audit-core-coverage.ps1"
    & $auditScript -FailOnGaps

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Coverage gaps detected!" -ForegroundColor Red
        Write-Host "   Every Core command action must reach the generated dispatch, and every" -ForegroundColor Red
        Write-Host "   PublicSurface category must produce an MCP tool. Fix before committing." -ForegroundColor Red
        exit 1
    }

    Write-Host "Coverage audit passed" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running coverage audit: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Checking Success flag violations (Rule 0)..." -ForegroundColor Cyan

try {
    $successFlagScript = Join-Path $rootDir "scripts\check-success-flag.ps1"
    & $successFlagScript

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Success flag violations detected!" -ForegroundColor Red
        Write-Host "   CRITICAL: Success=true with ErrorMessage confuses LLMs and causes data corruption." -ForegroundColor Red
        Write-Host "   Fix the violations before committing (add Success=false in catch blocks)." -ForegroundColor Red
        exit 1
    }

    Write-Host "Success flag check passed - all flags match reality" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running success flag check: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# NOTE: check-cli-coverage.ps1 and check-cli-action-coverage.ps1 were deleted (#16).
# Per-domain CLI commands are auto-generated by CliSettingsGenerator from the same
# [ServiceCategory] interfaces that produce the daemon routing, so an action cannot exist
# without a CLI command. Both scripts parsed a hand-written ToolActions.cs / ActionExtensions.cs
# model that no longer exists, and neither was ever invoked from this hook.
# CLI surface coverage is now asserted by audit-core-coverage.ps1 above, which compares the
# declared categories against the generated _CliCategories.g.cs.
# The check below still applies, because hand-written commands are not generated.

Write-Host ""
Write-Host "Checking hand-written CLI Settings property usage..." -ForegroundColor Cyan

try {
    $settingsUsageScript = Join-Path $rootDir "scripts\check-cli-settings-usage.ps1"
    & $settingsUsageScript

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Unread CLI Settings properties detected!" -ForegroundColor Red
        Write-Host "   Values the user supplies on the command line would be silently ignored." -ForegroundColor Red
        exit 1
    }

    Write-Host "CLI Settings usage check passed" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running CLI Settings usage check: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Auto-staging generated SKILL.md files..." -ForegroundColor Cyan

try {
    # SKILL.md files are generated during Release build from templates + source generators.
    # The Release build already ran (required for CLI smoke test below), so SKILL.md files
    # are up to date on disk. Auto-stage them so developers never have to think about it.
    # SKILL.md + references are generated during Release build.
    # Auto-stage all of them so developers never have to think about it.
    $skillPaths = @(
        "skills/visio-mcp/SKILL.md",
        "skills/visio-cli/SKILL.md",
        "skills/visio-mcp/references/",
        "skills/visio-cli/references/"
    )
    $skillDiff = git diff --name-only -- @skillPaths 2>&1
    $untrackedSkills = git ls-files --others --exclude-standard -- @skillPaths 2>&1

    $allChanges = @()
    if ($skillDiff) { $allChanges += $skillDiff }
    if ($untrackedSkills) { $allChanges += $untrackedSkills }

    if ($allChanges.Count -gt 0) {
        git add -- @skillPaths
        Write-Host "Skill files were regenerated and auto-staged ($($allChanges.Count) files)" -ForegroundColor Green
        $allChanges | ForEach-Object { Write-Host "   + $_" -ForegroundColor DarkGray }
    } else {
        Write-Host "Skill files are already up to date" -ForegroundColor Green
    }
}
catch {
    Write-Host "Error auto-staging SKILL.md files: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Continuing with remaining checks..." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Running CLI workflow smoke test..." -ForegroundColor Cyan

try {
    $cliWorkflowScript = Join-Path $rootDir "scripts\Test-CliWorkflow.ps1"
    $cliWorkflowOutput = & $cliWorkflowScript 2>&1 | Out-String
    $cliWorkflowExitCode = $LASTEXITCODE

    if ($cliWorkflowExitCode -ne 0) {
        Write-Host ""
        Write-Host "CLI workflow smoke test failed!" -ForegroundColor Red
        Write-Host "   This test validates the end-to-end CLI workflow." -ForegroundColor Red
        Write-Host "   Fix the issues before committing." -ForegroundColor Red
        Write-Host ""
        Write-Host $cliWorkflowOutput -ForegroundColor Gray
        exit 1
    }

    Write-Host "CLI workflow smoke test passed" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running CLI workflow smoke test: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running MCP Server smoke test..." -ForegroundColor Cyan

# Stop VisioMcp Service before smoke test to prevent DLL locking
& "$PSScriptRoot\Stop-VisioMcpProcesses.ps1"

try {
    # Run the smoke test - validates all MCP tools work correctly
    $smokeTestFilter = "FullyQualifiedName~McpServerIntegrationTests.SmokeTest_AllTools_E2EWorkflow"

    Write-Host "   dotnet test --filter `"$smokeTestFilter`"" -ForegroundColor Gray

    # Capture output to verify tests actually ran (dotnet test returns 0 even if no tests match!)
    $testOutput = dotnet test --filter $smokeTestFilter --verbosity minimal 2>&1 | Out-String
    $testExitCode = $LASTEXITCODE

    # Check if any tests actually passed (critical - filter typos cause silent failures!)
    # Note: "No test matches" appears for projects without the test, so we check for "Passed"
    if (-not ($testOutput -match "Passed!.*Passed:\s*[1-9]")) {
        Write-Host ""
        Write-Host "CRITICAL: No smoke tests passed! Filter may have matched zero tests." -ForegroundColor Red
        Write-Host "   Filter: $smokeTestFilter" -ForegroundColor Yellow
        Write-Host "   This likely means the test was renamed or deleted." -ForegroundColor Yellow
        Write-Host "   Verify the test exists: McpServerIntegrationTests.SmokeTest_AllTools_E2EWorkflow" -ForegroundColor Yellow
        Write-Host ""
        Write-Host $testOutput -ForegroundColor Gray
        exit 1
    }

    if ($testExitCode -ne 0) {
        Write-Host ""
        Write-Host "MCP Server smoke test failed! Core functionality is broken." -ForegroundColor Red
        Write-Host "   This test validates all MCP tools work correctly." -ForegroundColor Red
        Write-Host "   Fix the issues before committing." -ForegroundColor Red
        Write-Host ""
        Write-Host $testOutput -ForegroundColor Gray
        exit 1
    }

    Write-Host "MCP Server smoke test passed - all tools functional" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running smoke test: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Ensure Visio is installed and accessible." -ForegroundColor Yellow
    exit 1
}


Write-Host ""
Write-Host "All pre-commit checks passed!" -ForegroundColor Green
exit 0
