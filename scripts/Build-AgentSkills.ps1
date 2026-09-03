<#
.SYNOPSIS
    Builds the PowerPoint MCP Agent Skills package for distribution.

.DESCRIPTION
    Creates distributable artifacts for Agent Skills:
    - ppt-skills-v{version}.zip: Combined skill package with both visio-mcp and visio-cli
    - packages/visio-mcp-skill/: npm package for visio-mcp skill (publish with npm publish)
    - packages/visio-cli-skill/: npm package for visio-cli skill (publish with npm publish)
    - CLAUDE.md: Claude Code project instructions
    - .cursorrules: Cursor project rules

    Shared behavioral guidance from skills/shared/ is automatically copied
    to both visio-mcp/references/ and visio-cli/references/ during packaging.

    Users install with: npx skills add trsdn/mcp-server-visio
    Or via npm: npx skillpm install visio-mcp-skill

.PARAMETER OutputDir
    Output directory for artifacts. Default: artifacts/skills

.PARAMETER Version
    Override version from skills/visio-mcp/VERSION

.PARAMETER PopulateReferences
    Copy shared references to skill folders for local development (without packaging).

.EXAMPLE
    ./Build-AgentSkills.ps1

.EXAMPLE
    ./Build-AgentSkills.ps1 -OutputDir ./dist -Version 1.2.0

.EXAMPLE
    ./Build-AgentSkills.ps1 -PopulateReferences
#>
param(
    [string]$OutputDir = "artifacts/skills",
    [string]$Version = $null,
    [switch]$PopulateReferences
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$SkillsDir = Join-Path $RepoRoot "skills"
$SharedDir = Join-Path $SkillsDir "shared"

# Function to generate CLI command reference from visiocli --help output
function Generate-CliReference {
    param(
        [string]$SkillPath,
        [string]$visiocliPath = $null
    )

    # Find visiocli binary. Resolved by globbing the target-framework folder rather than naming it:
    # this script previously hard-coded net10.0-windows while the project targets net9.0-windows, so
    # it never found the binary, warned, and returned — leaving the generated CLI reference stale at
    # 5 of 15 commands with nothing failing (#57).
    if (-not $visiocliPath) {
        $cliBin = Join-Path $RepoRoot "src/VisioMcp.CLI/bin"
        $visiocliPath = @("Release", "Debug") |
            ForEach-Object { Join-Path $cliBin $_ } |
            Where-Object { Test-Path $_ } |
            ForEach-Object { Get-ChildItem -Path $_ -Filter "visiocli.exe" -Recurse -ErrorAction SilentlyContinue } |
            Select-Object -First 1 -ExpandProperty FullName
    }

    if (-not (Test-Path $visiocliPath)) {
        Write-Warning "visiocli not found at $visiocliPath - skipping CLI reference generation"
        Write-Warning "Build the CLI first: dotnet build src/VisioMcp.CLI -c Release"
        return
    }

    Write-Host "  Generating CLI command reference from visiocli..." -ForegroundColor Cyan

    $RefsDir = Join-Path $SkillPath "references"
    if (-not (Test-Path $RefsDir)) {
        New-Item -ItemType Directory -Path $RefsDir -Force | Out-Null
    }

    $OutputFile = Join-Path $RefsDir "cli-commands.md"
    $Content = @()
    $Content += "# CLI Command Reference"
    $Content += ""
    $Content += "> Auto-generated from \`visiocli --help\`. Do not edit manually."
    $Content += ""

    # Get main help to extract commands
    $MainHelp = & $visiocliPath --help 2>&1 | Out-String

    # Parse commands from main help (look for lines with command names)
    $Commands = @()
    $InCommands = $false
    foreach ($line in ($MainHelp -split "`r?`n")) {
        if ($line -match "^COMMANDS:") {
            $InCommands = $true
            continue
        }
        if ($InCommands -and $line -match "^\s{4}(\w+)\s") {
            $Commands += $Matches[1]
        }
    }

    # Skip 'session' as it's documented separately
    $Commands = $Commands | Where-Object { $_ -ne "session" }

    foreach ($cmd in $Commands) {
        $Content += "## $cmd"
        $Content += ""

        # Get command help
        $CmdHelp = & $visiocliPath $cmd --help 2>&1 | Out-String

        # Extract actions from the description line
        if ($CmdHelp -match "Actions:\s*(.+?)(?:\r?\n|$)") {
            $ActionsStr = $Matches[1] -replace "\s+", " "
            $Actions = ($ActionsStr -split ",\s*") | ForEach-Object { $_.Trim().TrimEnd('.') } | Where-Object { $_ -ne "" }

            # Extract options section
            $Options = @()
            $InOptions = $false
            foreach ($line in ($CmdHelp -split "`r?`n")) {
                if ($line -match "^OPTIONS:") {
                    $InOptions = $true
                    continue
                }
                if ($InOptions -and $line -match "^\s+--(\S+)\s+<[^>]+>\s+(.+)$") {
                    $Options += @{
                        Name = $Matches[1]
                        Desc = $Matches[2].Trim()
                    }
                }
                elseif ($InOptions -and $line -match "^\s+-\w\|--(\S+)\s+<[^>]+>\s+(.+)$") {
                    $Options += @{
                        Name = $Matches[1]
                        Desc = $Matches[2].Trim()
                    }
                }
            }

            # Output actions
            $ActionsList = $Actions | ForEach-Object { "``$_``" }
            $Content += "**Actions:** $($ActionsList -join ', ')"
            $Content += ""

            # Output parameters table if any
            if ($Options.Count -gt 0) {
                $Content += "| Parameter | Description |"
                $Content += "|-----------|-------------|"
                foreach ($opt in $Options) {
                    $Content += "| ``--$($opt.Name)`` | $($opt.Desc) |"
                }
                $Content += ""
            }
        }
    }

    # Write the file
    $Content -join "`n" | Set-Content -Path $OutputFile -Encoding UTF8 -NoNewline
    Write-Host "  Generated: cli-commands.md" -ForegroundColor Green
}

# Function to copy shared references to a skill's references folder
function Copy-SharedReferences {
    param(
        [string]$SkillPath,
        [string]$SkillName
    )

    $RefsDir = Join-Path $SkillPath "references"

    # Create references directory if it doesn't exist
    if (-not (Test-Path $RefsDir)) {
        New-Item -ItemType Directory -Path $RefsDir -Force | Out-Null
    }

    # Define which files each skill needs (based on SKILL.md @references/)
    $SkillReferences = @{
        "visio-cli" = @(
            "behavioral-rules.md"
            "anti-patterns.md"
            "workflows.md"
            # cli-commands.md is generated dynamically by Generate-CliReference
        )
        "visio-mcp" = @(
            "behavioral-rules.md"
            "anti-patterns.md"
            "workflows.md"
            "chart.md"
            "conditionalformat.md"
            "datamodel.md"
            "powerquery.md"
            "range.md"
            "slicer.md"
            "table.md"
            "worksheet.md"
        )
    }

    # Get the list of files for this skill
    $FilesToCopy = $SkillReferences[$SkillName]
    if (-not $FilesToCopy) {
        Write-Warning "No reference files defined for skill: $SkillName"
        return
    }

    # Copy only the files this skill needs
    if (Test-Path $SharedDir) {
        $CopiedCount = 0
        foreach ($fileName in $FilesToCopy) {
            $sourceFile = Join-Path $SharedDir $fileName
            if (Test-Path $sourceFile) {
                Copy-Item -Path $sourceFile -Destination $RefsDir -Force
                $CopiedCount++
            } else {
                Write-Warning "Reference file not found in shared: $fileName"
            }
        }
        Write-Host "  Copied $CopiedCount shared references to $SkillName/references/" -ForegroundColor Green
    } else {
        Write-Warning "Shared directory not found: $SharedDir"
    }
}

# Handle -PopulateReferences mode (for development)
if ($PopulateReferences) {
    Write-Host "Populating references from shared/ for local development..." -ForegroundColor Cyan

    # Copy to visio-mcp
    $McpPath = Join-Path $SkillsDir "visio-mcp"
    if (Test-Path $McpPath) {
        Copy-SharedReferences -SkillPath $McpPath -SkillName "visio-mcp"
    }

    # Copy to visio-cli
    $CliPath = Join-Path $SkillsDir "visio-cli"
    if (Test-Path $CliPath) {
        Copy-SharedReferences -SkillPath $CliPath -SkillName "visio-cli"
        # Generate CLI command reference from visiocli --help
        Generate-CliReference -SkillPath $CliPath
    }

    Write-Host ""
    Write-Host "Done! References populated for local development." -ForegroundColor Green
    exit 0
}

# Get version
if (-not $Version) {
    $VersionFile = Join-Path $SkillsDir "visio-mcp/VERSION"
    if (Test-Path $VersionFile) {
        $Version = (Get-Content $VersionFile -Raw).Trim()
    } else {
        $Version = "0.0.0"
    }
}

Write-Host "Building Agent Skills package v$Version" -ForegroundColor Cyan
Write-Host "Source: $SkillsDir"
Write-Host "Output: $OutputDir"
Write-Host ""

# Create output directory
$OutputPath = Join-Path $RepoRoot $OutputDir
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Build combined skills package
Write-Host "Building combined skills package..." -ForegroundColor Yellow

# Create staging directory
$StagingDir = Join-Path $env:TEMP "ppt-skills-$([guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null

try {
    # Create skills/ subdirectory (the standard location for npx skills add)
    $SkillsStagingDir = Join-Path $StagingDir "skills"
    New-Item -ItemType Directory -Path $SkillsStagingDir -Force | Out-Null

    # Copy visio-mcp skill
    $McpSource = Join-Path $SkillsDir "visio-mcp"
    if (Test-Path $McpSource) {
        Copy-Item -Path $McpSource -Destination "$SkillsStagingDir/visio-mcp" -Recurse
        Copy-SharedReferences -SkillPath "$SkillsStagingDir/visio-mcp" -SkillName "visio-mcp"
    } else {
        Write-Warning "visio-mcp skill not found"
    }

    # Copy visio-cli skill
    $CliSource = Join-Path $SkillsDir "visio-cli"
    if (Test-Path $CliSource) {
        Copy-Item -Path $CliSource -Destination "$SkillsStagingDir/visio-cli" -Recurse
        Copy-SharedReferences -SkillPath "$SkillsStagingDir/visio-cli" -SkillName "visio-cli"
        # Generate CLI command reference from visiocli --help
        Generate-CliReference -SkillPath "$SkillsStagingDir/visio-cli"
    } else {
        Write-Warning "visio-cli skill not found"
    }

    # Copy skills README to root of package
    $SkillsReadme = Join-Path $SkillsDir "README.md"
    if (Test-Path $SkillsReadme) {
        Copy-Item -Path $SkillsReadme -Destination $StagingDir
    }

    # Create ZIP archive
    $ZipName = "ppt-skills-v$Version.zip"
    $ZipPath = Join-Path $OutputPath $ZipName

    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }

    Compress-Archive -Path "$StagingDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal
    Write-Host "  Created: $ZipName" -ForegroundColor Green

} finally {
    if (Test-Path $StagingDir) {
        Remove-Item $StagingDir -Recurse -Force
    }
}

Write-Host ""
Write-Host "Building npm skill packages..." -ForegroundColor Yellow

# Populate visio-mcp-skill npm package
$NpmMcpDir = Join-Path $RepoRoot "packages/visio-mcp-skill/skills/visio-mcp"
if (Test-Path $NpmMcpDir) {
    # Clean previous build output (keep .gitkeep)
    Get-ChildItem $NpmMcpDir -Exclude ".gitkeep" -Recurse | Remove-Item -Recurse -Force
    # Copy SKILL.md
    Copy-Item -Path (Join-Path $SkillsDir "visio-mcp/SKILL.md") -Destination $NpmMcpDir
    Copy-SharedReferences -SkillPath $NpmMcpDir -SkillName "visio-mcp"
    Write-Host "  Populated: packages/visio-mcp-skill/" -ForegroundColor Green
}

# Populate visio-cli-skill npm package
$NpmCliDir = Join-Path $RepoRoot "packages/visio-cli-skill/skills/visio-cli"
if (Test-Path $NpmCliDir) {
    # Clean previous build output (keep .gitkeep)
    Get-ChildItem $NpmCliDir -Exclude ".gitkeep" -Recurse | Remove-Item -Recurse -Force
    # Copy SKILL.md
    Copy-Item -Path (Join-Path $SkillsDir "visio-cli/SKILL.md") -Destination $NpmCliDir
    Copy-SharedReferences -SkillPath $NpmCliDir -SkillName "visio-cli"
    Generate-CliReference -SkillPath $NpmCliDir
    Write-Host "  Populated: packages/visio-cli-skill/" -ForegroundColor Green
}

# Copy CLAUDE.md and .cursorrules
Write-Host "Copying platform-specific files..." -ForegroundColor Yellow

$ClaudeSrc = Join-Path $SkillsDir "CLAUDE.md"
if (Test-Path $ClaudeSrc) {
    Copy-Item -Path $ClaudeSrc -Destination $OutputPath
    Write-Host "  Created: CLAUDE.md" -ForegroundColor Green
}

$CursorSrc = Join-Path $SkillsDir ".cursorrules"
if (Test-Path $CursorSrc) {
    Copy-Item -Path $CursorSrc -Destination $OutputPath
    Write-Host "  Created: .cursorrules" -ForegroundColor Green
}

# Generate manifest
$Manifest = @{
    name = "ppt-skills"
    version = $Version
    description = "PowerPoint MCP Server Agent Skills for AI coding assistants"
    platforms = @("github-copilot", "claude-code", "cursor", "windsurf", "gemini-cli", "goose", "codex", "opencode", "amp", "kilo", "roo", "trae")
    skills = @(
        @{
            name = "visio-mcp"
            path = "skills/visio-mcp"
            description = "MCP Server skill - for conversational AI (Claude Desktop, VS Code Chat)"
            target = "MCP Server"
        }
        @{
            name = "visio-cli"
            path = "skills/visio-cli"
            description = "CLI skill - for coding agents (Copilot, Cursor, Windsurf)"
            target = "CLI Tool"
        }
    )
    installation = @{
        npx = "npx skills add trsdn/mcp-server-visio"
        selectSkill = "npx skills add trsdn/mcp-server-visio --skill visio-cli"
        installBoth = "npx skills add trsdn/mcp-server-visio --skill '*'"
    }
    files = @(
        @{ name = "CLAUDE.md"; type = "config"; description = "Claude Code project instructions" }
        @{ name = ".cursorrules"; type = "config"; description = "Cursor project rules" }
    )
    repository = "https://github.com/trsdn/mcp-server-visio"
    documentation = "https://VisioMcpserver.dev/"
    buildDate = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
}

$ManifestPath = Join-Path $OutputPath "manifest.json"
$Manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $ManifestPath -Encoding UTF8
Write-Host "  Created: manifest.json" -ForegroundColor Green

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Output files in: $OutputPath" -ForegroundColor Cyan
Get-ChildItem $OutputPath | ForEach-Object {
    $Size = if ($_.Length -gt 1MB) { "{0:N2} MB" -f ($_.Length / 1MB) }
            elseif ($_.Length -gt 1KB) { "{0:N2} KB" -f ($_.Length / 1KB) }
            else { "{0} bytes" -f $_.Length }
    Write-Host "  $($_.Name) ($Size)"
}

Write-Host ""
Write-Host "Installation:" -ForegroundColor Cyan
Write-Host "  npx skills add trsdn/mcp-server-visio" -ForegroundColor White
Write-Host "  (users will be prompted to select visio-cli, visio-mcp, or both)"
