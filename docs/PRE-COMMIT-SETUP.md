# Pre-Commit Hook Setup

This repository includes automated pre-commit checks to prevent code quality issues and coverage regression.

## What Gets Checked

1. **Branch Protection** — Blocks direct commits to `main` (Rule 6)
2. **Process cleanup** — Kills stale `VISIO`, `visiocli` and server processes so the build can replace locked binaries
3. **COM Object Leaks** — Ensures all `dynamic` COM objects are released in a `finally`
4. **Core Commands Coverage** — Every Core action reaches dispatch; every public domain reaches both MCP and CLI; no suppressed domain leaks
5. **Success Flag Violations** — `Success=true` is never paired with `ErrorMessage` (Rule 1)
6. **CLI Settings Usage** — Every Settings property on a hand-written CLI command is actually read
7. **CLI Workflow Smoke Test** — End-to-end CLI round-trip against a real `.vsdx`
8. **MCP Server Smoke Test** — All MCP tools reachable over the protocol

## Setup

**One command, once per clone:**

```powershell
.\scripts\Install-GitHooks.ps1
```

This sets `core.hooksPath` to the committed `.githooks` directory:

```powershell
git config core.hooksPath .githooks
```

Because the hook itself is version-controlled, every clone that runs the bootstrap once also picks
up later changes to the hook automatically. This replaces the previous instruction to
`Copy-Item scripts\pre-commit.ps1 .git\hooks\pre-commit`, which produced a private copy that
drifted — and which, in practice, nobody ran (#17).

**Verify it is installed:**

```powershell
git config --get core.hooksPath   # -> .githooks
```

**Run the checks without committing:**

```powershell
pwsh -File scripts\pre-commit.ps1
```

**Uninstall:**

```powershell
.\scripts\Install-GitHooks.ps1 -Uninstall
```

### Requirements

The hook entry point `.githooks/pre-commit` is a POSIX shell script, because Git invokes hooks
through its bundled shell even on Windows. It locates `pwsh` (PowerShell 7+), falling back to
`powershell`, and delegates to `scripts/pre-commit.ps1`. If neither is on `PATH` it fails with an
explanatory message rather than silently skipping the checks.

### CI runs the same gates

The hook is not the only line of defence. The `quality-gates` job in `build-cli.yml` runs the
Visio-independent gates — COM leaks, coverage audit, success flag, CLI settings usage — on every
PR, so bypassing the hook with `--no-verify` does not bypass the checks.

The CLI workflow and MCP smoke tests need a real Visio instance and run on the self-hosted
integration runner.

## What Happens on Failure

### Branch Protection Violation
```
❌ BLOCKED: Cannot commit directly to 'main' branch!

   Rule 6: All Changes Via Pull Requests
   'Never commit to main. Create feature branch → PR → CI/CD + review → merge.'

   To fix:
   1. git stash                                    # Save your changes
   2. git checkout -b feature/your-feature-name    # Create feature branch
   3. git stash pop                                # Restore changes
   4. git add <files>                              # Stage changes
   5. git commit -m 'your message'                 # Commit to feature branch
```

**Fix:** Follow the 5 steps above to move your work to a feature branch.

### COM Leak Detected
```
❌ COM object leaks detected! Fix them before committing.
```

**Fix:** Run `.\scripts\check-com-leaks.ps1` to see which files have leaks, then add proper `finally` blocks with `ComUtilities.Release(ref obj!)` calls.

### Coverage Gap Detected
```
❌ Coverage gaps detected! All Core methods must be exposed via MCP Server.
   Fix the gaps before committing (add enum values and mappings).
```

**Fix:** Follow the 5-step process:
1. Add enum values to `ToolActions.cs`
2. Add `ToActionString` mappings to `ActionExtensions.cs`
3. Add switch cases to appropriate MCP Tool
4. Implement MCP methods
5. Build and verify

See `.github/instructions/coverage-prevention-strategy.instructions.md` for details.

## Bypass Pre-Commit Checks (Emergency Only)

If you absolutely must commit without passing the checks (NOT recommended):

```powershell
git commit --no-verify -m "Emergency commit message"
```

**⚠️ Warning:** This should only be used in emergencies. Coverage gaps and COM leaks must be fixed before merging to main.

## Testing the Hook

Run manually before committing:

```powershell
# PowerShell
.\scripts\pre-commit.ps1

# Git Bash
bash .git/hooks/pre-commit
```

## Troubleshooting

### PowerShell not found
Install PowerShell 7+ from https://github.com/PowerShell/PowerShell/releases

### Scripts disabled on Windows
Run once as Administrator:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
```

### Hook not running automatically
Verify the file is executable:
```powershell
chmod +x .git/hooks/pre-commit
```

## Continuous Integration

These same checks run in CI/CD pipelines:
- `check-com-leaks.ps1` runs in build workflow
- `audit-core-coverage.ps1` runs **after every build** in:
  - `build-mcp-server.yml` - MCP Server builds
  - `build-cli.yml` - CLI builds  
  - `integration-tests.yml` - Visio integration runs when `ENABLE_VISIO_INTEGRATION_CI=true`

**Pipeline enforcement ensures:**
- Pre-commit hook provides **instant local feedback**
- CI/CD provides **safety net** if hook bypassed with `--no-verify`
- **Double protection** against coverage regression

`integration-tests.yml` is intentionally gated behind a self-hosted PowerPoint runner. See `docs/AZURE_SELFHOSTED_RUNNER_SETUP.md` for activation steps.

The pre-commit hook gives you **instant feedback** before pushing to remote.
