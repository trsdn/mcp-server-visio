#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Git pre-commit hook to check for COM object leaks, Core Commands coverage, naming consistency, Success flag violations, CLI workflow, and MCP Server functionality

.DESCRIPTION
    Runs checks before allowing commits:
    0. Process cleanup - kills stale Visio, visiocli, and MCP server processes to prevent file locks
    1. COM leak checker - ensures no Visio COM objects are leaked
    2. Coverage tests - ensures every Core method is exposed via a generated action with a mapping
    3. Success flag validation - ensures Success=true never paired with ErrorMessage (Rule 0)
    4. CLI workflow smoke test - validates end-to-end CLI functionality
    5. MCP Server smoke test - validates all MCP tools work correctly

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
Write-Host "Checking Core Commands coverage (action enums + mappings)..." -ForegroundColor Cyan

try {
    # The reflection-driven coverage tests replace the former hard-coded path audits:
    # they read the generated action enums directly, so they cannot drift out of date.
    dotnet test (Join-Path $rootDir "tests\VisioMcp.McpServer.Tests") `
        --filter "FullyQualifiedName~CoreCommandsCoverageTests" `
        --nologo -v q

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Coverage gaps detected!" -ForegroundColor Red
        Write-Host "   Every Core method must have a generated action value and a ToActionString mapping." -ForegroundColor Red
        Write-Host "   Fix the issues before committing." -ForegroundColor Red
        exit 1
    }

    Write-Host "Coverage checks passed - every Core method is reachable" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error running coverage tests: $($_.Exception.Message)" -ForegroundColor Red
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

# NOTE: CLI coverage checks removed - commands are now auto-generated by Roslyn source generators
# The CLI generator produces all command classes and registration from Core interfaces
# Validation is handled by:
# - Build-time generator errors if interfaces are malformed
# - CLI workflow smoke test below (end-to-end validation)

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
    # The summary line is localized ("Passed! - ... Passed: 1" / "Bestanden! : ... erfolgreich: 1"),
    # so match both rather than silently failing on a non-English Windows.
    if (-not ($testOutput -match "(Passed!|Bestanden!)[^\r\n]*(Passed|erfolgreich):\s*[1-9]")) {
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
Write-Host "Checking for undocumented ((dynamic)) casts..." -ForegroundColor Cyan

try {
    $dynamicCastScript = Join-Path $rootDir "scripts\check-dynamic-casts.ps1"
    & $dynamicCastScript

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Undocumented ((dynamic)) casts detected!" -ForegroundColor Red
        Write-Host "   Add a justification comment (// PIA gap:, // TODO:, or // Reason:) before each cast." -ForegroundColor Red
        Write-Host "   See docs/PIA-COVERAGE.md for guidance." -ForegroundColor Red
        exit 1
    }

    Write-Host "Dynamic cast check passed - all casts are documented" -ForegroundColor Green
}
catch {
    Write-Host "Error running dynamic cast check: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Continuing..." -ForegroundColor Gray
}

Write-Host ""
Write-Host "All pre-commit checks passed!" -ForegroundColor Green
exit 0
