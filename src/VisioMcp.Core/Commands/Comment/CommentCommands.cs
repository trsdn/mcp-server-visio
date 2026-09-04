using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Comment;

public class CommentCommands : ICommentCommands
{
    private const int VisObjTypeDoc = 10;
    private const int VisObjTypePage = 14;
    private const int VisObjTypeShape = 17;

    public CommentListResult List(IVisioBatch batch, int pageIndex, string? shapeName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? comments = null;
            dynamic? filterShape = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                comments = page.Comments;

                string? normalizedShapeName = null;
                if (!string.IsNullOrWhiteSpace(shapeName))
                {
                    filterShape = GetShape(page, shapeName);
                    normalizedShapeName = filterShape.Name?.ToString();
                }

                var result = new CommentListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = normalizedShapeName
                };

                int count = Convert.ToInt32(comments.Count, CultureInfo.InvariantCulture);
                for (int i = 1; i <= count; i++)
                {
                    dynamic? comment = null;
                    dynamic? associatedObject = null;
                    try
                    {
                        comment = comments.Item(i);
                        associatedObject = comment.AssociatedObject;
                        var info = ReadCommentInfo(comment, associatedObject, pageIndex, i);

                        if (normalizedShapeName is null ||
                            string.Equals(info.AssociatedShapeName, normalizedShapeName, StringComparison.Ordinal))
                        {
                            result.Comments.Add(info);
                        }
                    }
                    finally
                    {
                        if (associatedObject != null) ComUtilities.Release(ref associatedObject!);
                        if (comment != null) ComUtilities.Release(ref comment!);
                    }
                }

                return result;
            }
            finally
            {
                if (filterShape != null) ComUtilities.Release(ref filterShape!);
                if (comments != null) ComUtilities.Release(ref comments!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Add(IVisioBatch batch, int pageIndex, string text, string? shapeName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? comments = null;
            dynamic? comment = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                if (!string.IsNullOrWhiteSpace(shapeName))
                {
                    shape = GetShape(page, shapeName);
                    comments = shape.Comments;
                }
                else
                {
                    comments = page.Comments;
                }

                comment = comments.Add(text);

                return new OperationResult
                {
                    Success = true,
                    Action = "add",
                    Message = shapeName is null
                        ? $"Added page comment on page {pageIndex}"
                        : $"Added shape comment on shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (comment != null) ComUtilities.Release(ref comment!);
                if (comments != null) ComUtilities.Release(ref comments!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, int pageIndex, int commentIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commentIndex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? comments = null;
            dynamic? comment = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                comments = page.Comments;
                comment = comments.Item(commentIndex);
                comment.Delete();

                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted comment {commentIndex} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (comment != null) ComUtilities.Release(ref comment!);
                if (comments != null) ComUtilities.Release(ref comments!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Clear(IVisioBatch batch, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? comments = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                comments = page.Comments;
                int cleared = Convert.ToInt32(comments.Count, CultureInfo.InvariantCulture);
                comments.DeleteAll();

                return new OperationResult
                {
                    Success = true,
                    Action = "clear",
                    Message = $"Cleared {cleared} comment(s) from page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (comments != null) ComUtilities.Release(ref comments!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    private static CommentInfo ReadCommentInfo(dynamic comment, dynamic? associatedObject, int pageIndex, int commentIndex)
    {
        var info = new CommentInfo
        {
            PageIndex = pageIndex,
            CommentIndex = commentIndex,
            Text = comment.Text?.ToString() ?? string.Empty,
            AuthorName = comment.AuthorName?.ToString() ?? string.Empty,
            AuthorInitials = comment.AuthorInitials?.ToString() ?? string.Empty,
            CreateDate = ToIsoString(comment.CreateDate),
            EditDate = ToIsoString(comment.EditDate),
            AssociatedObjectType = "None"
        };

        if (associatedObject is null)
        {
            return info;
        }

        int objectType = Convert.ToInt32(associatedObject.ObjectType, CultureInfo.InvariantCulture);
        info.AssociatedObjectType = ObjectTypeName(objectType);

        if (objectType == VisObjTypeShape)
        {
            info.AssociatedShapeName = associatedObject.Name?.ToString();
        }
        else if (objectType == VisObjTypePage)
        {
            info.AssociatedPageName = associatedObject.Name?.ToString();
        }

        return info;
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        dynamic? pages = null;
        try
        {
            pages = ((dynamic)ctx.Document).Pages;
            return pages.Item(pageIndex);
        }
        finally
        {
            if (pages != null) ComUtilities.Release(ref pages!);
        }
    }

    private static dynamic GetShape(dynamic page, string shapeName)
    {
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            return shapes.Item(shapeName);
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }

    private static string ToIsoString(object value)
    {
        return Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            .ToString("O", CultureInfo.InvariantCulture);
    }

    private static string ObjectTypeName(int objectType)
    {
        return objectType switch
        {
            VisObjTypeDoc => "Document",
            VisObjTypePage => "Page",
            VisObjTypeShape => "Shape",
            _ => $"ObjectType:{objectType}"
        };
    }
}
