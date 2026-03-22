using Microsoft.Extensions.Logging.Abstractions;
using VisioMcp.ComInterop.Session;
using Xunit;

namespace VisioMcp.ComInterop.Tests;

public sealed class VisioShutdownServiceTests
{
    [Fact]
    public void CloseAndQuit_SaveFalse_ClosesAllOpenDocumentsWithoutSavingPrompt()
    {
        dynamic primary = new FakeDocument("primary.vsdx");
        dynamic secondary = new FakeDocument("secondary.vssx");
        dynamic app = new FakeApplication(primary, secondary);

        VisioShutdownService.CloseAndQuit((dynamic)primary, (dynamic)app, save: false, filePath: primary.FullName, logger: NullLogger.Instance);

        Assert.Equal(7, app.AlertResponse);
        Assert.False(app.Visible);
        Assert.True(primary.Saved);
        Assert.True(primary.CloseCalled);
        Assert.True(primary.CloseWithSaveCalled);
        Assert.False(primary.CloseSaveChangesValue);
        Assert.True(secondary.Saved);
        Assert.True(secondary.CloseCalled);
        Assert.True(secondary.CloseWithSaveCalled);
        Assert.False(secondary.CloseSaveChangesValue);
        Assert.True(app.QuitCalled);
    }

    [Fact]
    public void CloseAndQuit_SaveTrue_SavesPrimaryBeforeClosingOpenDocuments()
    {
        dynamic primary = new FakeDocument("primary.vsdx");
        dynamic stencil = new FakeDocument("stencil.vssx");
        dynamic app = new FakeApplication(primary, stencil);

        VisioShutdownService.CloseAndQuit((dynamic)primary, (dynamic)app, save: true, filePath: primary.FullName, logger: NullLogger.Instance);

        Assert.True(primary.SaveCalled);
        Assert.True(primary.CloseCalled);
        Assert.True(primary.CloseWithSaveCalled);
        Assert.True(primary.CloseSaveChangesValue);
        Assert.True(stencil.CloseCalled);
        Assert.True(stencil.CloseWithSaveCalled);
        Assert.True(stencil.CloseSaveChangesValue);
        Assert.True(app.QuitCalled);
    }

    public sealed class FakeApplication(params FakeDocument[] documents)
    {
        public int AlertResponse { get; set; }

        public bool Visible { get; set; } = true;

        public FakeDocuments Documents { get; } = new(documents);

        public bool QuitCalled { get; private set; }

        public void Quit()
        {
            QuitCalled = true;
        }
    }

    public sealed class FakeDocuments(params FakeDocument[] documents)
    {
        private readonly List<FakeDocument> _documents = [.. documents];

        public int Count => _documents.Count;

        public FakeDocument Item(int index) => _documents[index - 1];
    }

    public sealed class FakeDocument
    {
        public FakeDocument(string fullName)
        {
            FullName = fullName;
        }

        public string FullName { get; }

        public string Name => Path.GetFileName(FullName);

        public bool Saved { get; set; }

        public bool SaveCalled { get; private set; }

        public bool CloseCalled { get; private set; }

        public bool CloseWithSaveCalled { get; private set; }

        public bool? CloseSaveChangesValue { get; private set; }

        public void Save()
        {
            SaveCalled = true;
            Saved = true;
        }

        public void Close()
        {
            CloseCalled = true;
        }

        public void Close(bool saveChanges)
        {
            CloseWithSaveCalled = true;
            CloseSaveChangesValue = saveChanges;
            CloseCalled = true;

            if (saveChanges)
            {
                Saved = true;
            }
        }
    }
}
