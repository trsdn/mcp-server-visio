using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Image;

public class ImageCommands : IImageCommands
{
    public OperationResult Insert(IVisioBatch batch, int pageIndex, string imagePath, float left, float top, float width, float height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        return batch.Execute((ctx, ct) =>
        {
            string fullImagePath = Path.GetFullPath(imagePath);
            if (!System.IO.File.Exists(fullImagePath))
                throw new FileNotFoundException($"Image file not found: '{fullImagePath}'");

            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(pageIndex);
                shape = page.Import(fullImagePath);

                SetPointFormula(shape, "PinX", left);
                SetPointFormula(shape, "PinY", top);
                if (width > 0)
                {
                    SetPointFormula(shape, "Width", width);
                }

                if (height > 0)
                {
                    SetPointFormula(shape, "Height", height);
                }

                string name = shape.Name?.ToString() ?? "";

                return new OperationResult
                {
                    Success = true,
                    Action = "insert",
                    Message = $"Inserted image '{Path.GetFileName(fullImagePath)}' as '{name}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    public OperationResult Crop(IVisioBatch batch, int pageIndex, string shapeName, float cropLeft, float cropRight, float cropTop, float cropBottom)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(pageIndex);
                shape = page.Shapes.Item(shapeName);

                EnsureImageCell(shape, shapeName, "ImgOffsetX");
                EnsureImageCell(shape, shapeName, "ImgOffsetY");
                EnsureImageCell(shape, shapeName, "ImgWidth");
                EnsureImageCell(shape, shapeName, "ImgHeight");

                SetPointFormula(shape, "ImgOffsetX", -cropLeft);
                SetPointFormula(shape, "ImgOffsetY", -cropBottom);
                SetFormula(shape, "ImgWidth", $"Width+{FormatPoints(cropLeft + cropRight)}");
                SetFormula(shape, "ImgHeight", $"Height+{FormatPoints(cropTop + cropBottom)}");

                return new OperationResult
                {
                    Success = true,
                    Action = "crop",
                    Message = $"Cropped image '{shapeName}' on page {pageIndex} by point amounts (L:{FormatNumber(cropLeft)}, R:{FormatNumber(cropRight)}, T:{FormatNumber(cropTop)}, B:{FormatNumber(cropBottom)})",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    public OperationResult SetBrightnessContrast(IVisioBatch batch, int pageIndex, string shapeName, float brightness, float contrast)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ValidatePercentageFraction(brightness, nameof(brightness));
        ValidatePercentageFraction(contrast, nameof(contrast));

        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(pageIndex);
                shape = page.Shapes.Item(shapeName);

                EnsureImageCell(shape, shapeName, "Brightness");
                EnsureImageCell(shape, shapeName, "Contrast");
                SetPercentFormula(shape, "Brightness", brightness);
                SetPercentFormula(shape, "Contrast", contrast);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-brightness-contrast",
                    Message = $"Set brightness={FormatNumber(brightness)}, contrast={FormatNumber(contrast)} on image '{shapeName}' on page {pageIndex} (0.5 is neutral)",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static void EnsureImageCell(dynamic shape, string shapeName, string cellName)
    {
        if (!ShapeSheetHelpers.CellExists(shape, cellName))
        {
            throw new InvalidOperationException(
                $"Shape '{shapeName}' is not an imported image; ShapeSheet cell '{cellName}' is missing.");
        }
    }

    private static void ValidatePercentageFraction(float value, string parameterName)
    {
        if (value is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Visio image brightness and contrast are percentage fractions from 0.0 to 1.0; 0.5 is neutral.");
        }
    }

    private static void SetPointFormula(dynamic shape, string cellName, float points) =>
        SetFormula(shape, cellName, FormatPoints(points));

    private static string FormatPoints(float points) =>
        $"{points.ToString("0.############", CultureInfo.InvariantCulture)} pt";

    private static string FormatNumber(float value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);

    private static void SetPercentFormula(dynamic shape, string cellName, float fraction) =>
        SetFormula(shape, cellName, $"{(fraction * 100f).ToString("0.############", CultureInfo.InvariantCulture)}%");

    private static void SetFormula(dynamic shape, string cellName, string formula)
    {
        ShapeSheetHelpers.SetFormula(shape, cellName, formula);
    }
}
