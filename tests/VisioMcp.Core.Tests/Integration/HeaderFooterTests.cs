using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.HeaderFooter;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Headers and footers ported to <c>Document.Header*</c> / <c>Document.Footer*</c> (#63).
///
/// Visio's model is document-scoped and field-based: six independent strings, rather than
/// PowerPoint's per-slide "show footer / show slide number / show date" toggles. A page number is
/// not a boolean here — it is the field code <c>&amp;p</c> placed in whichever of the six fields
/// the caller wants it in.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "HeaderFooter")]
public sealed class HeaderFooterTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly HeaderFooterCommands _headerFooter = new();

    [Fact]
    public void ANewDocument_HasEmptyFieldsAndANonZeroMargin()
    {
        using var batch = CreateDocument();

        var read = _headerFooter.GetInfo(batch);

        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(string.Empty, read.HeaderLeft);
        Assert.Equal(string.Empty, read.FooterRight);

        // Visio ships a default margin; asserting "not zero" rather than a literal keeps this from
        // breaking on a locale that defaults to millimetres.
        Assert.True(read.HeaderMargin > 0, $"Expected a default header margin, got {read.HeaderMargin}.");
        Assert.True(read.FooterMargin > 0, $"Expected a default footer margin, got {read.FooterMargin}.");
    }

    [Fact]
    public void EachOfTheSixFields_RoundTripsIndependently()
    {
        using var batch = CreateDocument();

        _headerFooter.Update(
            batch,
            headerLeft: "HL", headerCenter: "HC", headerRight: "HR",
            footerLeft: "FL", footerCenter: "FC", footerRight: "FR",
            headerMargin: null, footerMargin: null);

        var read = _headerFooter.GetInfo(batch);

        Assert.Equal("HL", read.HeaderLeft);
        Assert.Equal("HC", read.HeaderCenter);
        Assert.Equal("HR", read.HeaderRight);
        Assert.Equal("FL", read.FooterLeft);
        Assert.Equal("FC", read.FooterCenter);
        Assert.Equal("FR", read.FooterRight);
    }

    [Fact]
    public void OmittedFields_AreLeftUnchanged()
    {
        using var batch = CreateDocument();
        SetAllSix(batch, "original");

        _headerFooter.Update(
            batch,
            headerLeft: "replaced", headerCenter: null, headerRight: null,
            footerLeft: null, footerCenter: null, footerRight: null,
            headerMargin: null, footerMargin: null);

        var read = _headerFooter.GetInfo(batch);

        Assert.Equal("replaced", read.HeaderLeft);
        Assert.Equal("original", read.HeaderCenter);
        Assert.Equal("original", read.FooterRight);
    }

    [Fact]
    public void AnEmptyString_ClearsAField()
    {
        using var batch = CreateDocument();
        SetAllSix(batch, "text");

        _headerFooter.Update(
            batch,
            headerLeft: string.Empty, headerCenter: null, headerRight: null,
            footerLeft: null, footerCenter: null, footerRight: null,
            headerMargin: null, footerMargin: null);

        var read = _headerFooter.GetInfo(batch);

        Assert.Equal(string.Empty, read.HeaderLeft);
        Assert.Equal("text", read.HeaderCenter);
    }

    /// <summary>
    /// The margin properties are parameterised — <c>double HeaderMargin(Variant UnitsNameOrCode)</c>
    /// — so a plain dynamic assignment silently fails to bind. This asserts the reflection path
    /// actually writes.
    /// </summary>
    [Fact]
    public void Margins_RoundTrip()
    {
        using var batch = CreateDocument();

        _headerFooter.Update(
            batch,
            headerLeft: null, headerCenter: null, headerRight: null,
            footerLeft: null, footerCenter: null, footerRight: null,
            headerMargin: 0.75, footerMargin: 0.6);

        var read = _headerFooter.GetInfo(batch);

        Assert.Equal(0.75, read.HeaderMargin, precision: 3);
        Assert.Equal(0.6, read.FooterMargin, precision: 3);
    }

    /// <summary>
    /// Field codes are stored verbatim and expanded only on output, so a caller that writes
    /// '&amp;p' must read '&amp;p' back rather than a page number. If this ever returns an expanded
    /// value, the descriptions promising round-tripping are wrong.
    /// </summary>
    [Fact]
    public void FieldCodes_AreStoredVerbatim()
    {
        using var batch = CreateDocument();
        const string withCodes = "Page &p of &P — &f";

        _headerFooter.Update(
            batch,
            headerLeft: null, headerCenter: withCodes, headerRight: null,
            footerLeft: null, footerCenter: null, footerRight: null,
            headerMargin: null, footerMargin: null);

        Assert.Equal(withCodes, _headerFooter.GetInfo(batch).HeaderCenter);
    }

    [Fact]
    public void SupplyingNothing_SucceedsAndChangesNothing()
    {
        using var batch = CreateDocument();
        SetAllSix(batch, "kept");

        var result = _headerFooter.Update(
            batch,
            headerLeft: null, headerCenter: null, headerRight: null,
            footerLeft: null, footerCenter: null, footerRight: null,
            headerMargin: null, footerMargin: null);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("kept", _headerFooter.GetInfo(batch).HeaderLeft);
    }

    [Fact]
    public void Settings_SurviveSaveAndReopen()
    {
        var path = NewDocumentPath();
        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);

        using (var batch = VisioSession.BeginBatch(path))
        {
            _headerFooter.Update(
                batch,
                headerLeft: null, headerCenter: "Persisted &p", headerRight: null,
                footerLeft: null, footerCenter: null, footerRight: "Footer",
                headerMargin: 0.9, footerMargin: null);

            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);
        var read = _headerFooter.GetInfo(reopened);

        Assert.Equal("Persisted &p", read.HeaderCenter);
        Assert.Equal("Footer", read.FooterRight);
        Assert.Equal(0.9, read.HeaderMargin, precision: 3);
    }

    private void SetAllSix(IVisioBatch batch, string text)
    {
        _headerFooter.Update(
            batch,
            headerLeft: text, headerCenter: text, headerRight: text,
            footerLeft: text, footerCenter: text, footerRight: text,
            headerMargin: null, footerMargin: null);
    }

    private string NewDocumentPath()
    {
        var path = Path.Join(Path.GetTempPath(), $"HeaderFooterTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);
        return path;
    }

    private IVisioBatch CreateDocument()
    {
        var path = NewDocumentPath();
        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
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
