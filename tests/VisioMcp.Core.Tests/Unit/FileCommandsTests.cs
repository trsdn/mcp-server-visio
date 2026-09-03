using VisioMcp.Core.Commands.File;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

[Trait("Layer", "Core")]
[Trait("Category", "Unit")]
[Trait("Feature", "File")]
[Trait("Speed", "Fast")]
[Trait("RequiresVisio", "false")]
public sealed class FileCommandsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileCommands _commands = new();

    public FileCommandsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"VisioMcp_File_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Test_VsdmFile_ReturnsMacroEnabledForVisio()
    {
        var path = Path.Combine(_tempDir, "macro-diagram.vsdm");
        File.WriteAllText(path, "test");

        var result = _commands.Test(path);

        Assert.True(result.Success);
        Assert.True(result.Exists);
        Assert.True(result.IsMacroEnabled);
        Assert.Equal(-1, result.PageCount);
        Assert.Equal(-1, result.SlideCount);
    }

    [Fact]
    public void Test_VsdxFile_DoesNotReportMacroEnabled()
    {
        var path = Path.Combine(_tempDir, "plain-diagram.vsdx");
        File.WriteAllText(path, "test");

        var result = _commands.Test(path);

        Assert.True(result.Success);
        Assert.True(result.Exists);
        Assert.False(result.IsMacroEnabled);
        Assert.Equal(-1, result.PageCount);
    }
}
