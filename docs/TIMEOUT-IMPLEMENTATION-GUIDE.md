# Timeout Implementation Guide

How VisioMcp stops a hung Visio from hanging the caller.

Visio is an out-of-process COM server. A modal dialog, a broken data link or a COM deadlock will
block the automation thread indefinitely, with no error and no return. Every layer below has a
deadline for that reason.

## The timeouts, and what each protects

All are in `src/VisioMcp.ComInterop/ComInteropConstants.cs`.

| Constant | Value | Protects against |
|---|---|---|
| `DefaultOperationTimeout` | 5 minutes | A single operation never returning |
| `SaveOperationTimeout` | 5 minutes | A save blocking on a large or remote file |
| `VisioQuitTimeout` | 30 seconds | `Application.Quit()` hanging on shutdown |
| `StaThreadJoinTimeout` | 45 seconds | `Dispose()` returning before the STA thread finished |
| `SessionFileLockTimeout` | 5 seconds | Waiting forever on a session lock file |

### One relationship is load-bearing

```csharp
public static readonly TimeSpan StaThreadJoinTimeout =
    VisioQuitTimeout + TimeSpan.FromSeconds(15);
```

`StaThreadJoinTimeout` **must remain greater than or equal to** `VisioQuitTimeout`. `Dispose()`
joins the STA thread; if it gave up first, it would return while `CloseAndQuit()` was still
running, leaving a live `VISIO` process behind and a document still locked. The 15-second margin
covers document close and COM release after the quit itself has returned.

Change one and you must change the other.

## How enforcement works

Work is marshalled onto the batch's STA thread and awaited with a deadline
(`VisioBatch.Execute`):

```csharp
var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

// ... queue the operation onto the STA thread ...

using var timeoutCts = new CancellationTokenSource(_operationTimeout);
return tcs.Task.WaitAsync(timeoutCts.Token).GetAwaiter().GetResult();
```

On expiry the caller gets a `TimeoutException` naming the file and the limit, and suggesting the
override:

> Visio operation timed out after 300 seconds for 'Diagram.vsdx'. Visio may be unresponsive or the
> operation is taking longer than expected. Consider increasing timeoutSeconds when opening the
> session.

Because the whole operation runs inside `TaskCompletionSource`, **exceptions from command code
propagate to it and become a failed result**. This is why Core commands must not wrap
`batch.Execute()` in a try-catch that returns an error result — see Rule 1b. A `finally` block for
COM cleanup is correct and expected; a `catch` that swallows is not.

## Overriding per session

`session open` and `session create` accept `timeoutSeconds`, which replaces
`DefaultOperationTimeout` for that session:

```powershell
visiocli session open "Large.vsdx" --timeout-seconds 900
```

Raise it for drawings with many pages, large embedded images, or linked data. Leave it alone
otherwise: a long default hides the hangs this system exists to surface.

## Alerts

A modal dialog is the most common cause of an unrecoverable hang, so sessions set:

```csharp
visioApp.AlertResponse = 7;   // answer "No" and continue
```

Without it, deleting a page that is in use as a background blocks on a dialog no one can click.
The timeout would eventually fire, but the operation would fail rather than proceed — the alert
response is what lets it succeed.

## When something hangs anyway

1. Check for a visible Visio window awaiting input — visible-session mode does not set
   `AlertResponse` on a user-facing instance the same way.
2. Look for orphaned processes: `Get-Process VISIO`
3. Terminate by process id only: `Stop-Process -Id <pid>`
4. Confirm the drawing is not open in an interactive Visio; the server needs exclusive access.

## Related

- `docs/DEVELOPMENT.md` — session and batch architecture
- `.github/instructions/critical-rules.instructions.md` — Rule 1b, exception propagation
- `.github/instructions/architecture-patterns.instructions.md` — the batch pattern
