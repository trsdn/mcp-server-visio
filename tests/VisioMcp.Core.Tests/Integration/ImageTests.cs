using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Image;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Image operations ported to Visio-native <c>Page.Import</c> and image ShapeSheet cells (#64).
///
/// Integration tests against real Visio (Rule 30). The brightness/contrast tests pin Visio's
/// percentage contract: <c>0.5</c> is neutral, unlike PowerPoint's signed zero-centred values.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Image")]
public sealed class ImageTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly ImageCommands _images = new();

    [Fact]
    public void Insert_ImportsRealPngAsForeignObjectAndPositionsInPoints()
    {
        using var batch = CreateDocument();

        var result = _images.Insert(batch, 1, ImagePath(), left: 72f, top: 144f, width: 216f, height: 108f);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);

        var image = ReadSingleImage(batch);
        Assert.Equal(4, image.Type);
        Assert.Equal(32, image.ForeignType);
        Assert.Equal(72d, image.PinXPoints, precision: 3);
        Assert.Equal(144d, image.PinYPoints, precision: 3);
        Assert.Equal(216d, image.WidthPoints, precision: 3);
        Assert.Equal(108d, image.HeightPoints, precision: 3);
    }

    [Fact]
    public void ANewImportedImage_ReadsNeutralBrightnessAndContrastAsHalf()
    {
        using var batch = CreateDocument();
        Assert.True(_images.Insert(batch, 1, ImagePath(), 72f, 72f, 144f, 144f).Success);

        var image = ReadSingleImage(batch);

        Assert.Equal(0.5d, ReadCellResult(batch, image.Name, "Brightness"), precision: 3);
        Assert.Equal(0.5d, ReadCellResult(batch, image.Name, "Contrast"), precision: 3);
    }

    [Fact]
    public void SetBrightnessContrast_WritesFractionsWhereHalfIsNeutral()
    {
        using var batch = CreateDocument();
        Assert.True(_images.Insert(batch, 1, ImagePath(), 72f, 72f, 144f, 144f).Success);
        var image = ReadSingleImage(batch);

        var result = _images.SetBrightnessContrast(batch, 1, image.Name, brightness: 0.5f, contrast: 0.75f);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(0.5d, ReadCellResult(batch, image.Name, "Brightness"), precision: 3);
        Assert.Equal(0.75d, ReadCellResult(batch, image.Name, "Contrast"), precision: 3);
        Assert.Equal("50%", ReadCellFormula(batch, image.Name, "Brightness"));
        Assert.Equal("75%", ReadCellFormula(batch, image.Name, "Contrast"));
    }

    [Fact]
    public void Crop_StoresPointAmountsInImageCellsWithoutMovingTheShapeFrame()
    {
        using var batch = CreateDocument();
        Assert.True(_images.Insert(batch, 1, ImagePath(), 72f, 72f, 288f, 144f).Success);
        var image = ReadSingleImage(batch);

        var result = _images.Crop(batch, 1, image.Name, cropLeft: 18f, cropRight: 36f, cropTop: 9f, cropBottom: 27f);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(72d, ReadCellResult(batch, image.Name, "PinX") * 72d, precision: 3);
        Assert.Equal(72d, ReadCellResult(batch, image.Name, "PinY") * 72d, precision: 3);
        Assert.Equal(-18d, ReadCellResult(batch, image.Name, "ImgOffsetX") * 72d, precision: 3);
        Assert.Equal(-27d, ReadCellResult(batch, image.Name, "ImgOffsetY") * 72d, precision: 3);
        Assert.Equal(342d, ReadCellResult(batch, image.Name, "ImgWidth") * 72d, precision: 3);
        Assert.Equal(180d, ReadCellResult(batch, image.Name, "ImgHeight") * 72d, precision: 3);
    }

    [Fact]
    public void ImageSettings_SurviveSaveAndReopen()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        string shapeName;

        using (var batch = VisioSession.BeginBatch(path))
        {
            Assert.True(_images.Insert(batch, 1, ImagePath(), 72f, 72f, 144f, 144f).Success);
            shapeName = ReadSingleImage(batch).Name;
            _images.SetBrightnessContrast(batch, 1, shapeName, brightness: 0.65f, contrast: 0.55f);
            _images.Crop(batch, 1, shapeName, cropLeft: 6f, cropRight: 12f, cropTop: 18f, cropBottom: 24f);
            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);

        Assert.Equal(0.65d, ReadCellResult(reopened, shapeName, "Brightness"), precision: 3);
        Assert.Equal(0.55d, ReadCellResult(reopened, shapeName, "Contrast"), precision: 3);
        Assert.Equal(-6d, ReadCellResult(reopened, shapeName, "ImgOffsetX") * 72d, precision: 3);
        Assert.Equal(-24d, ReadCellResult(reopened, shapeName, "ImgOffsetY") * 72d, precision: 3);
        Assert.Equal(162d, ReadCellResult(reopened, shapeName, "ImgWidth") * 72d, precision: 3);
        Assert.Equal(186d, ReadCellResult(reopened, shapeName, "ImgHeight") * 72d, precision: 3);
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private static string ImagePath() => Path.Combine(FindRepositoryRoot(), "mcpb", "icon-512.png");

    private static ImageShapeInfo ReadSingleImage(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shapes = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shapes = page.Shapes;
                Assert.Equal(1, Convert.ToInt32(shapes.Count));
                shape = shapes.Item(1);

                return new ImageShapeInfo(
                    shape.Name?.ToString() ?? string.Empty,
                    Convert.ToInt32(shape.Type),
                    Convert.ToInt32(shape.ForeignType),
                    ReadRequiredCellResult(shape, "PinX") * 72d,
                    ReadRequiredCellResult(shape, "PinY") * 72d,
                    ReadRequiredCellResult(shape, "Width") * 72d,
                    ReadRequiredCellResult(shape, "Height") * 72d);
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (shapes != null) ComUtilities.Release(ref shapes!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static double ReadCellResult(IVisioBatch batch, string shapeName, string cellName)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shape = page.Shapes.Item(shapeName);
                return ReadRequiredCellResult(shape, cellName);
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static string ReadCellFormula(IVisioBatch batch, string shapeName, string cellName)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? cell = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shape = page.Shapes.Item(shapeName);
                cell = shape.CellsU[cellName];
                return cell.FormulaU?.ToString() ?? string.Empty;
            }
            finally
            {
                if (cell != null) ComUtilities.Release(ref cell!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static double ReadRequiredCellResult(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            Assert.NotEqual(0, Convert.ToInt32(shape.CellExistsU[cellName, 0]));
            cell = shape.CellsU[cellName];
            return Convert.ToDouble(cell.ResultIU);
        }
        finally
        {
            if (cell != null) ComUtilities.Release(ref cell!);
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FEATURES.md"))
                && File.Exists(Path.Combine(current.FullName, "VisioMcp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Repository root not found walking up from '{AppContext.BaseDirectory}'.");
    }

    private sealed record ImageShapeInfo(
        string Name,
        int Type,
        int ForeignType,
        double PinXPoints,
        double PinYPoints,
        double WidthPoints,
        double HeightPoints);
}
