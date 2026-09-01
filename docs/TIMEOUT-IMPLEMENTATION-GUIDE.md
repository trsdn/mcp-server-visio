# Timeout Behaviour

How VisioMcp bounds long-running Visio COM operations.

> Reference doc. The authoritative values live in
> `src/VisioMcp.ComInterop/ComInteropConstants.cs`; if this file and the code disagree, the code wins.

## Constants

All timeouts are defined in `ComInteropConstants`:

| Constant | Value | Purpose |
|---|---|---|
| `DefaultOperationTimeout` | 5 min | Default bound for a single `batch.Execute(...)` call |
| `SaveOperationTimeout` | 5 min | Bound for save operations |
| `VisioQuitTimeout` | 30 s | Bound for `Visio.Quit()` |
| `StaThreadJoinTimeout` | `VisioQuitTimeout` + 15 s | STA thread join after quit; must exceed `VisioQuitTimeout` so `Dispose()` waits for `CloseAndQuit()` |
| `SessionFileLockTimeout` | 5 s | Bound for acquiring the session file lock |

## Where the timeout is set

The timeout is **per batch**, not per call. It is chosen when the session is created and then applies
to every operation executed through that batch:

```csharp
// Default (ComInteropConstants.DefaultOperationTimeout)
using var batch = VisioSession.BeginBatch(filePath);

// Explicit override
using var batch = VisioSession.BeginBatch(show: false, operationTimeout: TimeSpan.FromMinutes(10), filePath);

// Read back what is in effect
TimeSpan effective = batch.OperationTimeout;
```

`IVisioBatch.Execute` deliberately takes **no** timeout argument:

```csharp
void Execute(Action<VisioContext, CancellationToken> operation, CancellationToken cancellationToken = default);
T    Execute<T>(Func<VisioContext, CancellationToken, T> operation, CancellationToken cancellationToken = default);
```

Use the `CancellationToken` for caller-driven cancellation; use `operationTimeout` on the session for
a different time bound.

## How enforcement works

`VisioBatch` marshals every operation onto its dedicated STA thread and awaits the result through a
`TaskCompletionSource` guarded by a `CancellationTokenSource(_operationTimeout)`. On expiry it throws
`TimeoutException` and logs the operation timeout together with the document name.

Because the exception propagates out of `Execute`, Core commands must **not** wrap it (see Rule 1b).
It surfaces to the caller as `OperationResult { Success = false, ErrorMessage = ... }`, which the MCP
layer serializes to JSON (see Rule 17).

## Choosing a timeout

- Reads (list pages, list shapes, read a cell) finish in well under a second — keep the default.
- Bulk authoring (dropping hundreds of masters, writing many ShapeSheet cells) and export/rendering
  are the operations worth raising the session timeout for.
- Raising the timeout does not make an operation faster; prefer a single batch over many sessions,
  since each session start pays the Visio launch cost.
