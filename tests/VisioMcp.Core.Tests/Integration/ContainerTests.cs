using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Container;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for Visio-native containers, list containers, and callouts (#123).
///
/// A container is not a group: it owns membership while members remain independent shapes. These
/// tests verify the relationship through real Visio rather than checking only success flags.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Container")]
public sealed class ContainerTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly ContainerCommands _containers = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void Drop_CreatesAContainerWithHeadingMarginAndInitialMember()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "ContainedA", 1, 1, 2, 2);

        var drop = _containers.Drop(batch, 1, "ContainedA", masterName: "Plain", headingText: "Workstream", margin: 12f, resizeMode: 2);
        var list = _containers.List(batch, 1);

        Assert.True(drop.Success, drop.ErrorMessage);
        Assert.Null(drop.ErrorMessage);
        var container = Assert.Single(list.Containers);
        Assert.Equal(drop.Container?.Name, container.Name);
        Assert.Equal("Workstream", drop.Container?.HeadingText);
        Assert.Equal(0, drop.Container?.ContainerType);
        Assert.False(drop.Container?.IsList);
        Assert.Equal(2, drop.Container?.ResizeAsNeeded);
        Assert.Equal("expand-contract", drop.Container?.ResizeAsNeededName);
        Assert.Equal(12d, drop.Container?.MarginPoints ?? 0d, precision: 3);
        Assert.Contains(drop.Container?.Members ?? [], m => m.Name == "ContainedA");
    }

    [Fact]
    public void AddMemberAndRemoveMember_ManageMembershipInOneSpecificContainer()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "ContainedA", 1, 1, 2, 2);
        CreateNamedRectangle(batch, "ContainedB", 5, 1, 6, 2);
        var containerName = _containers.Drop(batch, 1, "ContainedA", masterName: "Plain").Container!.Name;

        var add = _containers.AddMember(batch, 1, containerName, "ContainedB", addOptions: 1);
        var membersAfterAdd = _containers.ListMembers(batch, 1, containerName);
        var containersOfB = _containers.ContainersOf(batch, 1, "ContainedB");
        var remove = _containers.RemoveMember(batch, 1, containerName, "ContainedB");
        var membersAfterRemove = _containers.ListMembers(batch, 1, containerName);

        Assert.True(add.Success, add.ErrorMessage);
        Assert.True(remove.Success, remove.ErrorMessage);
        Assert.Equal(["ContainedA", "ContainedB"], membersAfterAdd.Members.Select(m => m.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Contains(containersOfB.Containers, c => c.Name == containerName);
        Assert.DoesNotContain(membersAfterRemove.Members, m => m.Name == "ContainedB");
        Assert.Contains(membersAfterRemove.Members, m => m.Name == "ContainedA");
    }

    [Fact]
    public void Membership_SurvivesMovingTheMemberShape()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "MovableMember", 1, 1, 2, 2);
        var containerName = _containers.Drop(batch, 1, "MovableMember", masterName: "Plain").Container!.Name;

        var move = _shapes.MoveResize(batch, 1, "MovableMember", left: 360f, top: 288f, width: null, height: null);
        var memberships = _containers.ContainersOf(batch, 1, "MovableMember");

        Assert.True(move.Success, move.ErrorMessage);
        Assert.Contains(memberships.Containers, c => c.Name == containerName);
    }

    [Fact]
    public void Membership_SurvivesSaveAndReopen()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        string containerName;

        using (var batch = VisioSession.BeginBatch(path))
        {
            CreateNamedRectangle(batch, "PersistedMember", 1, 1, 2, 2);
            containerName = _containers.Drop(batch, 1, "PersistedMember", masterName: "Plain", headingText: "Persisted").Container!.Name;
            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);
        var members = _containers.ListMembers(reopened, 1, containerName);

        Assert.Contains(members.Members, m => m.Name == "PersistedMember");
    }

    [Fact]
    public void DeletingAMember_RemovesItFromTheContainerWithoutDeletingTheContainer()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "DeletedMember", 1, 1, 2, 2);
        var containerName = _containers.Drop(batch, 1, "DeletedMember", masterName: "Plain").Container!.Name;

        var delete = _shapes.Delete(batch, 1, "DeletedMember");
        var members = _containers.ListMembers(batch, 1, containerName);
        var containers = _containers.List(batch, 1);

        Assert.True(delete.Success, delete.ErrorMessage);
        Assert.Empty(members.Members);
        Assert.Contains(containers.Containers, c => c.Name == containerName);
    }

    [Fact]
    public void DropList_CreatesListContainerAndInsertListMemberAddsOrderedMember()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "ListMemberA", 1, 1, 2, 2);
        CreateNamedRectangle(batch, "ListMemberB", 5, 1, 6, 2);
        var list = _containers.DropList(batch, 1, "ListMemberA");

        var insert = _containers.InsertListMember(batch, 1, list.Container!.Name, "ListMemberB", position: 2);

        Assert.True(list.Success, list.ErrorMessage);
        Assert.True(insert.Success, insert.ErrorMessage);
        Assert.True(insert.Container?.IsList);
        Assert.Contains(insert.Container?.ListMembers ?? [], m => m.Name == "ListMemberB");
    }

    [Fact]
    public void DropCallout_AssociatesCalloutWithTargetShape()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "CalloutTarget", 1, 1, 2, 2);

        var drop = _containers.DropCallout(batch, 1, "CalloutTarget", masterName: "Text Callout", text: "Risk note");
        var callouts = _containers.ListCallouts(batch, 1);
        var associated = _containers.CalloutsOf(batch, 1, "CalloutTarget");

        Assert.True(drop.Success, drop.ErrorMessage);
        Assert.Null(drop.ErrorMessage);
        Assert.Equal("Risk note", drop.Callout?.Text);
        Assert.Equal("CalloutTarget", drop.Callout?.TargetShapeName);
        Assert.Contains(callouts.Callouts, c => c.Name == drop.Callout?.Name && c.TargetShapeName == "CalloutTarget");
        Assert.Contains(associated.Callouts, c => c.Name == drop.Callout?.Name);
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private static void CreateNamedRectangle(IVisioBatch batch, string shapeName, double x1, double y1, double x2, double y2)
    {
        batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(1);
                shape = page.DrawRectangle(x1, y1, x2, y2);
                shape.Name = shapeName;
                return 0;
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }
}
