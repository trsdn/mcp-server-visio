using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Stencil;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Container;

public class ContainerCommands : IContainerCommands
{
    private const int VisBuiltInStencilContainers = 2;
    private const int VisBuiltInStencilCallouts = 3;
    private const int VisMSUS = 2;
    private const int VisPoints = 50;
    private const int VisContainerTypeList = 1;
    private const string DefaultContainerMasterName = "Plain";
    private const string DefaultCalloutMasterName = "Text Callout";
    private const string DefaultListMasterName = "Task List";
    private const string DefaultListStencil = "timelinetodolist_u.vssx";

    public ContainerListResult List(IVisioBatch batch, int pageIndex, int nestedOptions = 0)
    {
        ValidateNestedOptions(nestedOptions);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                var result = new ContainerListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                foreach (int shapeId in GetIds(page.GetContainers(nestedOptions)))
                {
                    dynamic? shape = null;
                    try
                    {
                        shape = GetShapeById(page, shapeId);
                        result.Containers.Add(ReadContainerInfo(page, shape, false, 0));
                    }
                    finally
                    {
                        if (shape != null) ComUtilities.Release(ref shape!);
                    }
                }

                return result;
            }
            finally
            {
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerDetailResult Read(IVisioBatch batch, int pageIndex, string containerName, int memberFlags = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ValidateMemberFlags(memberFlags);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? container = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                container = GetShape(page, containerName);
                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, container, true, memberFlags)
                };
            }
            finally
            {
                if (container != null) ComUtilities.Release(ref container!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerDetailResult Drop(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? headingText = null, float? margin = null, int? resizeMode = null)
    {
        return DropContainerFromStencil(
            batch,
            pageIndex,
            targetShapeName,
            masterName ?? DefaultContainerMasterName,
            openStencil: ctx => StencilDocumentHelper.OpenBuiltInStencilDocument(ctx, VisBuiltInStencilContainers, VisMSUS),
            headingText,
            margin,
            resizeMode);
    }

    public ContainerDetailResult DropList(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? stencilPath = null)
    {
        return DropContainerFromStencil(
            batch,
            pageIndex,
            targetShapeName,
            masterName ?? DefaultListMasterName,
            openStencil: ctx => StencilDocumentHelper.OpenStencilDocument(ctx, stencilPath ?? DefaultListStencil),
            headingText: null,
            margin: null,
            resizeMode: null);
    }

    public ContainerDetailResult AddMember(IVisioBatch batch, int pageIndex, string containerName, string memberShapeName, int addOptions = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberShapeName);
        ValidateAddOptions(addOptions);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? container = null;
            dynamic? member = null;
            dynamic? properties = null;
            bool? originalLockMembership = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                container = GetShape(page, containerName);
                member = GetShape(page, memberShapeName);
                properties = container.ContainerProperties;
                originalLockMembership = Convert.ToBoolean(properties.LockMembership, CultureInfo.InvariantCulture);
                properties.LockMembership = false;
                properties.AddMember(member, addOptions);
                originalLockMembership = null;
                properties.LockMembership = true;

                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, container, true, 0)
                };
            }
            finally
            {
                if (properties != null && originalLockMembership.HasValue)
                {
                    properties.LockMembership = originalLockMembership.GetValueOrDefault();
                }

                if (properties != null) ComUtilities.Release(ref properties!);
                if (member != null) ComUtilities.Release(ref member!);
                if (container != null) ComUtilities.Release(ref container!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerDetailResult RemoveMember(IVisioBatch batch, int pageIndex, string containerName, string memberShapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberShapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? container = null;
            dynamic? member = null;
            dynamic? properties = null;
            bool? originalLockMembership = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                container = GetShape(page, containerName);
                member = GetShape(page, memberShapeName);
                properties = container.ContainerProperties;
                originalLockMembership = Convert.ToBoolean(properties.LockMembership, CultureInfo.InvariantCulture);
                properties.LockMembership = false;
                properties.RemoveMember(member);
                originalLockMembership = null;
                properties.LockMembership = true;

                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, container, true, 0)
                };
            }
            finally
            {
                if (properties != null && originalLockMembership.HasValue)
                {
                    properties.LockMembership = originalLockMembership.GetValueOrDefault();
                }

                if (properties != null) ComUtilities.Release(ref properties!);
                if (member != null) ComUtilities.Release(ref member!);
                if (container != null) ComUtilities.Release(ref container!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerMemberListResult ListMembers(IVisioBatch batch, int pageIndex, string containerName, int memberFlags = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ValidateMemberFlags(memberFlags);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? container = null;
            dynamic? properties = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                container = GetShape(page, containerName);
                properties = container.ContainerProperties;

                var result = new ContainerMemberListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ContainerName = container.Name?.ToString() ?? containerName,
                    Members = ReadMembers(page, properties, memberFlags)
                };

                int containerType = Convert.ToInt32(properties.ContainerType, CultureInfo.InvariantCulture);
                if (containerType == VisContainerTypeList)
                {
                    result.ListMembers = ReadListMembers(page, properties);
                }

                return result;
            }
            finally
            {
                if (properties != null) ComUtilities.Release(ref properties!);
                if (container != null) ComUtilities.Release(ref container!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerMembershipResult ContainersOf(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                shape = GetShape(page, shapeName);
                var result = new ContainerMembershipResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName
                };

                foreach (int shapeId in GetIds(shape.MemberOfContainers))
                {
                    dynamic? container = null;
                    try
                    {
                        container = GetShapeById(page, shapeId);
                        result.Containers.Add(ReadContainerInfo(page, container, false, 0));
                    }
                    finally
                    {
                        if (container != null) ComUtilities.Release(ref container!);
                    }
                }

                return result;
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerDetailResult FitToContents(IVisioBatch batch, int pageIndex, string containerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? container = null;
            dynamic? properties = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                container = GetShape(page, containerName);
                properties = container.ContainerProperties;
                properties.FitToContents();

                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, container, true, 0)
                };
            }
            finally
            {
                if (properties != null) ComUtilities.Release(ref properties!);
                if (container != null) ComUtilities.Release(ref container!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public ContainerDetailResult InsertListMember(IVisioBatch batch, int pageIndex, string listName, string memberShapeName, int position = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listName);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberShapeName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(position);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? list = null;
            dynamic? member = null;
            dynamic? properties = null;
            bool? originalLockMembership = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                list = GetShape(page, listName);
                member = GetShape(page, memberShapeName);
                properties = list.ContainerProperties;
                originalLockMembership = Convert.ToBoolean(properties.LockMembership, CultureInfo.InvariantCulture);
                properties.LockMembership = false;
                properties.InsertListMember(member, position);
                originalLockMembership = null;
                properties.LockMembership = true;

                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, list, true, 0)
                };
            }
            finally
            {
                if (properties != null && originalLockMembership.HasValue)
                {
                    properties.LockMembership = originalLockMembership.GetValueOrDefault();
                }

                if (properties != null) ComUtilities.Release(ref properties!);
                if (member != null) ComUtilities.Release(ref member!);
                if (list != null) ComUtilities.Release(ref list!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public CalloutDetailResult DropCallout(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? text = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetShapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? targetShape = null;
            dynamic? stencilDocument = null;
            dynamic? masters = null;
            dynamic? master = null;
            dynamic? callout = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                targetShape = GetShape(page, targetShapeName);
                stencilDocument = StencilDocumentHelper.OpenBuiltInStencilDocument(ctx, VisBuiltInStencilCallouts, VisMSUS);
                masters = stencilDocument.Masters;
                string resolvedMasterName = masterName ?? DefaultCalloutMasterName;
                master = StencilDocumentHelper.FindMasterByName(masters, resolvedMasterName)
                    ?? throw new InvalidOperationException($"Callout master '{resolvedMasterName}' was not found.");

                callout = page.DropCallout(master, targetShape);
                if (text is not null)
                {
                    callout.Text = text;
                }

                return new CalloutDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Callout = ReadCalloutInfo(callout)
                };
            }
            finally
            {
                if (callout != null) ComUtilities.Release(ref callout!);
                if (master != null) ComUtilities.Release(ref master!);
                if (masters != null) ComUtilities.Release(ref masters!);
                StencilDocumentHelper.CloseStencilDocument(ref stencilDocument);
                if (targetShape != null) ComUtilities.Release(ref targetShape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public CalloutListResult ListCallouts(IVisioBatch batch, int pageIndex, int nestedOptions = 0)
    {
        ValidateNestedOptions(nestedOptions);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                var result = new CalloutListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                foreach (int shapeId in GetIds(page.GetCallouts(nestedOptions)))
                {
                    dynamic? callout = null;
                    try
                    {
                        callout = GetShapeById(page, shapeId);
                        result.Callouts.Add(ReadCalloutInfo(callout));
                    }
                    finally
                    {
                        if (callout != null) ComUtilities.Release(ref callout!);
                    }
                }

                return result;
            }
            finally
            {
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public CalloutDetailResult ReadCallout(IVisioBatch batch, int pageIndex, string calloutName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calloutName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? callout = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                callout = GetShape(page, calloutName);
                return new CalloutDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Callout = ReadCalloutInfo(callout)
                };
            }
            finally
            {
                if (callout != null) ComUtilities.Release(ref callout!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public CalloutAssociationResult CalloutsOf(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                shape = GetShape(page, shapeName);
                var result = new CalloutAssociationResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName
                };

                foreach (int shapeId in GetIds(shape.CalloutsAssociated))
                {
                    dynamic? callout = null;
                    try
                    {
                        callout = GetShapeById(page, shapeId);
                        result.Callouts.Add(ReadCalloutInfo(callout));
                    }
                    finally
                    {
                        if (callout != null) ComUtilities.Release(ref callout!);
                    }
                }

                return result;
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    private static ContainerDetailResult DropContainerFromStencil(
        IVisioBatch batch,
        int pageIndex,
        string targetShapeName,
        string masterName,
        Func<VisioContext, dynamic> openStencil,
        string? headingText,
        float? margin,
        int? resizeMode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetShapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);
        if (margin.HasValue && margin.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin), "Margin must be zero or greater.");
        }

        if (resizeMode.HasValue)
        {
            ValidateResizeMode(resizeMode.Value);
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? targetShape = null;
            dynamic? stencilDocument = null;
            dynamic? masters = null;
            dynamic? master = null;
            dynamic? container = null;
            dynamic? properties = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                targetShape = GetShape(page, targetShapeName);
                stencilDocument = openStencil(ctx);
                masters = stencilDocument.Masters;
                master = StencilDocumentHelper.FindMasterByName(masters, masterName)
                    ?? throw new InvalidOperationException($"Container master '{masterName}' was not found.");

                container = page.DropContainer(master, targetShape);
                properties = container.ContainerProperties;
                properties.LockMembership = true;

                if (headingText is not null)
                {
                    container.Text = headingText;
                }

                if (resizeMode.HasValue)
                {
                    properties.ResizeAsNeeded = resizeMode.Value;
                }

                if (margin.HasValue)
                {
                    properties.SetMargin(VisPoints, Convert.ToDouble(margin.Value, CultureInfo.InvariantCulture));
                }

                return new ContainerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Container = ReadContainerInfo(page, container, true, 0)
                };
            }
            finally
            {
                if (properties != null) ComUtilities.Release(ref properties!);
                if (container != null) ComUtilities.Release(ref container!);
                if (master != null) ComUtilities.Release(ref master!);
                if (masters != null) ComUtilities.Release(ref masters!);
                StencilDocumentHelper.CloseStencilDocument(ref stencilDocument);
                if (targetShape != null) ComUtilities.Release(ref targetShape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    private static ContainerInfo ReadContainerInfo(dynamic page, dynamic container, bool includeMembers, int memberFlags)
    {
        dynamic? properties = null;
        try
        {
            properties = container.ContainerProperties;
            int containerType = Convert.ToInt32(properties.ContainerType, CultureInfo.InvariantCulture);
            int resizeAsNeeded = Convert.ToInt32(properties.ResizeAsNeeded, CultureInfo.InvariantCulture);
            var members = ReadMembers(page, properties, memberFlags);

            var info = new ContainerInfo
            {
                ShapeId = Convert.ToInt32(container.ID, CultureInfo.InvariantCulture),
                Name = container.Name?.ToString() ?? string.Empty,
                NameU = container.NameU?.ToString() ?? string.Empty,
                HeadingText = container.Text?.ToString() ?? string.Empty,
                ContainerType = containerType,
                ContainerTypeName = ContainerTypeName(containerType),
                IsList = containerType == VisContainerTypeList,
                ResizeAsNeeded = resizeAsNeeded,
                ResizeAsNeededName = ResizeAsNeededName(resizeAsNeeded),
                MarginPoints = Convert.ToDouble(properties.GetMargin(VisPoints), CultureInfo.InvariantCulture),
                LockMembership = Convert.ToBoolean(properties.LockMembership, CultureInfo.InvariantCulture),
                ContainerStyle = Convert.ToInt32(properties.ContainerStyle, CultureInfo.InvariantCulture),
                HeadingStyle = Convert.ToInt32(properties.HeadingStyle, CultureInfo.InvariantCulture),
                MemberCount = members.Count
            };

            if (includeMembers)
            {
                info.Members = members;
                if (info.IsList)
                {
                    info.ListMembers = ReadListMembers(page, properties);
                }
            }

            return info;
        }
        finally
        {
            if (properties != null) ComUtilities.Release(ref properties!);
        }
    }

    private static List<ContainerMemberInfo> ReadMembers(dynamic page, dynamic properties, int memberFlags)
    {
        var results = new List<ContainerMemberInfo>();
        foreach (int shapeId in GetIds(properties.GetMemberShapes(memberFlags)))
        {
            dynamic? shape = null;
            try
            {
                shape = GetShapeById(page, shapeId);
                results.Add(ReadMemberInfo(shape));
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }

        return results;
    }

    private static List<ContainerMemberInfo> ReadListMembers(dynamic page, dynamic properties)
    {
        var results = new List<ContainerMemberInfo>();
        foreach (int shapeId in GetIds(properties.GetListMembers()))
        {
            dynamic? shape = null;
            try
            {
                shape = GetShapeById(page, shapeId);
                results.Add(ReadMemberInfo(shape));
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }

        return results;
    }

    private static ContainerMemberInfo ReadMemberInfo(dynamic shape)
    {
        return new ContainerMemberInfo
        {
            ShapeId = Convert.ToInt32(shape.ID, CultureInfo.InvariantCulture),
            Name = shape.Name?.ToString() ?? string.Empty,
            NameU = shape.NameU?.ToString() ?? string.Empty
        };
    }

    private static CalloutInfo ReadCalloutInfo(dynamic callout)
    {
        var info = new CalloutInfo
        {
            ShapeId = Convert.ToInt32(callout.ID, CultureInfo.InvariantCulture),
            Name = callout.Name?.ToString() ?? string.Empty,
            NameU = callout.NameU?.ToString() ?? string.Empty,
            Text = callout.Text?.ToString() ?? string.Empty,
            IsCallout = Convert.ToBoolean(callout.IsCallout, CultureInfo.InvariantCulture)
        };

        dynamic? target = null;
        try
        {
            target = callout.CalloutTarget;
            if (target != null)
            {
                info.TargetShapeId = Convert.ToInt32(target.ID, CultureInfo.InvariantCulture);
                info.TargetShapeName = target.Name?.ToString();
            }
        }
        finally
        {
            if (target != null) ComUtilities.Release(ref target!);
        }

        return info;
    }

    private static List<int> GetIds(object? rawIds)
    {
        if (rawIds is null)
        {
            return [];
        }

        if (rawIds is Array array)
        {
            var ids = new List<int>(array.Length);
            foreach (object? value in array)
            {
                if (value is not null)
                {
                    ids.Add(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                }
            }

            return ids;
        }

        return [Convert.ToInt32(rawIds, CultureInfo.InvariantCulture)];
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
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

    private static dynamic GetShapeById(dynamic page, int shapeId)
    {
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            return shapes.ItemFromID(shapeId);
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }

    private static void ValidateNestedOptions(int nestedOptions)
    {
        if (nestedOptions is not 0 and not 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nestedOptions), "Nested options must be 0 (include nested) or 1 (exclude nested).");
        }
    }

    private static void ValidateMemberFlags(int memberFlags)
    {
        if (memberFlags is < 0 or > 63)
        {
            throw new ArgumentOutOfRangeException(nameof(memberFlags), "Member flags must be a bitmask from 0 through 63.");
        }
    }

    private static void ValidateAddOptions(int addOptions)
    {
        if (addOptions is not 0 and not 1 and not 2)
        {
            throw new ArgumentOutOfRangeException(nameof(addOptions), "Add options must be 0 (use resize setting), 1 (expand), or 2 (do not expand).");
        }
    }

    private static void ValidateResizeMode(int resizeMode)
    {
        if (resizeMode is not 0 and not 1 and not 2)
        {
            throw new ArgumentOutOfRangeException(nameof(resizeMode), "Resize mode must be 0 (none), 1 (expand), or 2 (expand and contract).");
        }
    }

    private static string ContainerTypeName(int containerType)
    {
        return containerType switch
        {
            0 => "normal",
            1 => "list",
            _ => $"unknown:{containerType}"
        };
    }

    private static string ResizeAsNeededName(int resizeAsNeeded)
    {
        return resizeAsNeeded switch
        {
            0 => "none",
            1 => "expand",
            2 => "expand-contract",
            _ => $"unknown:{resizeAsNeeded}"
        };
    }
}
