using VisioMcp.ComInterop.Session;
using Xunit;

namespace VisioMcp.ComInterop.Tests.Integration.Session;

/// <summary>
/// Every batch operation runs inside a Visio undo scope (#36a).
///
/// This is a safety property rather than a feature. Without it, an operation that writes several
/// cells and then fails leaves the document half-edited, and the caller has no way to tell which
/// writes landed. <c>EndUndoScope(id, commit: false)</c> reverts them.
///
/// It also makes an agent's edit undoable in one step: a command that writes five cells becomes a
/// single Ctrl+Z for the user watching, rather than five.
///
/// Verified against real Visio (Rule 30) — the rollback behaviour of <c>EndUndoScope</c> is not
/// something a mock could establish.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "ComInterop")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "VisioBatch")]
public sealed class UndoScopeTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    [Fact]
    public void FailedOperation_RollsBackItsPartialChanges()
    {
        var path = CreateDocument();
        using var batch = VisioSession.BeginBatch(path);

        double before = batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.DrawRectangle(1, 1, 3, 2);
            shape.Name = "Target";
            return (double)shape.CellsU["Width"].ResultIU;
        });

        // An operation that writes, then fails. Without an undo scope the write survives.
        Assert.Throws<InvalidOperationException>(() => batch.Execute<int>((ctx, ct) =>
        {
            dynamic shape = ctx.Document.Pages[1].Shapes["Target"];
            shape.CellsU["Width"].FormulaU = "5 in";

            throw new InvalidOperationException("simulated failure after a partial write");
        }));

        double after = batch.Execute((ctx, ct) =>
            (double)ctx.Document.Pages[1].Shapes["Target"].CellsU["Width"].ResultIU);

        Assert.Equal(before, after, precision: 4);
    }

    [Fact]
    public void SuccessfulOperation_KeepsItsChanges()
    {
        var path = CreateDocument();
        using var batch = VisioSession.BeginBatch(path);

        batch.Execute<int>((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.DrawRectangle(1, 1, 3, 2);
            shape.Name = "Target";
            return 0;
        });

        batch.Execute<int>((ctx, ct) =>
        {
            dynamic shape = ctx.Document.Pages[1].Shapes["Target"];
            shape.CellsU["Width"].FormulaU = "4 in";
            return 0;
        });

        double width = batch.Execute((ctx, ct) =>
            (double)ctx.Document.Pages[1].Shapes["Target"].CellsU["Width"].ResultIU);

        Assert.Equal(4.0, width, precision: 4);
    }

    /// <summary>
    /// Grouping is what makes rollback atomic across several writes: if the scope covered only the
    /// last write, the earlier ones would survive a failure.
    /// </summary>
    /// <remarks>
    /// The undo-stack behaviour itself cannot be asserted from inside a batch, because every
    /// <c>Execute</c> now runs in a scope and calling <c>Application.Undo()</c> from within one is
    /// not meaningful. It was verified directly against a live instance instead: after two cells
    /// were written inside one committed scope, a single <c>Undo()</c> reverted both
    /// (4 in / 2 in returned to 2 in / 1 in).
    /// </remarks>
    [Fact]
    public void FailedOperation_RollsBackEveryWrite_NotJustTheLast()
    {
        var path = CreateDocument();
        using var batch = VisioSession.BeginBatch(path);

        var (beforeWidth, beforeHeight) = batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.DrawRectangle(1, 1, 3, 2);
            shape.Name = "Target";
            return ((double)shape.CellsU["Width"].ResultIU, (double)shape.CellsU["Height"].ResultIU);
        });

        Assert.Throws<InvalidOperationException>(() => batch.Execute<int>((ctx, ct) =>
        {
            dynamic shape = ctx.Document.Pages[1].Shapes["Target"];
            shape.CellsU["Width"].FormulaU = "4 in";
            shape.CellsU["Height"].FormulaU = "3 in";

            throw new InvalidOperationException("simulated failure after two writes");
        }));

        var (afterWidth, afterHeight) = batch.Execute((ctx, ct) =>
        {
            dynamic shape = ctx.Document.Pages[1].Shapes["Target"];
            return ((double)shape.CellsU["Width"].ResultIU, (double)shape.CellsU["Height"].ResultIU);
        });

        Assert.Equal(beforeWidth, afterWidth, precision: 4);
        Assert.Equal(beforeHeight, afterHeight, precision: 4);
    }

    private string CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"UndoScopeTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // The file may still be briefly held after the batch disposes.
            }
        }
    }
}
