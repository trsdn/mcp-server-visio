using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Container;

/// <summary>
/// Visio containers, list containers, and callouts for structural membership while shapes remain independent.
/// </summary>
[ServiceCategory("container")]
[McpTool("container", Title = "Container, List, and Callout Operations", Destructive = true, Category = "containers",
    Description = "Manage Visio containers, lists, and callouts. Use this tool when shapes must keep a structural membership relationship. "
    + "A group fuses selected shapes into one composite shape and is managed by shape(group). "
    + "A container is different: it owns membership, can resize around members, and keeps the association when a member moves. "
    + "Use container(drop/add-member/list-members/containers-of) for membership; use shape(group) only when the shapes should become one grouped shape. "
    + "drop uses Visio's built-in container gallery (master_name default 'Plain'). drop-list uses the installed Timeline To Do List stencil (master_name default 'Task List'). "
    + "drop-callout uses Visio's built-in callout gallery (master_name default 'Text Callout'). "
    + "nested_options: 0=include nested containers/callouts, 1=exclude nested. member_flags: 0=all; add 1 exclude containers, 2 connectors, 4 callouts, 8 plain elements, 16 nested, 32 explicit list members. "
    + "add_options: 0=use container resize setting, 1=expand container to fit the member, 2=do not expand. resize_mode: 0=no automatic resize, 1=expand only, 2=expand and contract. margin is in points.")]
public interface IContainerCommands
{
    /// <summary>List container and list shapes on a page.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="nestedOptions">0 includes nested containers and lists; 1 excludes containers and lists nested inside another container</param>
    [ServiceAction("list")]
    ContainerListResult List(IVisioBatch batch, int pageIndex, int nestedOptions = 0);

    /// <summary>Read one container or list, including member shapes.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="containerName">Container or list shape name, as reported by list</param>
    /// <param name="memberFlags">0 returns all members; add 1 to exclude containers, 2 connectors, 4 callouts, 8 plain elements, 16 nested members, or 32 explicit list members</param>
    [ServiceAction("read")]
    ContainerDetailResult Read(IVisioBatch batch, int pageIndex, string containerName, int memberFlags = 0);

    /// <summary>Drop a built-in Visio container around an existing shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="targetShapeName">Existing top-level shape to make the first container member</param>
    /// <param name="masterName">Built-in container master name, for example 'Plain', 'Classic', or 'Banner'. Omitted uses 'Plain'</param>
    /// <param name="headingText">Optional heading text to write into the new container shape</param>
    /// <param name="margin">Optional member margin in points</param>
    /// <param name="resizeMode">Optional resize mode: 0=no automatic resize, 1=expand only, 2=expand and contract</param>
    [ServiceAction("drop")]
    ContainerDetailResult Drop(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? headingText = null, float? margin = null, int? resizeMode = null);

    /// <summary>Drop a list container around an existing shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="targetShapeName">Existing top-level shape to place in the list container</param>
    /// <param name="masterName">List master name in the stencil. Omitted uses 'Task List'</param>
    /// <param name="stencilPath">Optional stencil path or installed stencil file name. Omitted uses 'timelinetodolist_u.vssx'</param>
    [ServiceAction("drop-list")]
    ContainerDetailResult DropList(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? stencilPath = null);

    /// <summary>Add a shape to a specific container or list container.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="containerName">Target container or list shape name</param>
    /// <param name="memberShapeName">Existing top-level shape to add as a member</param>
    /// <param name="addOptions">0 uses ResizeAsNeeded, 1 expands the container to fit the member, 2 does not expand</param>
    [ServiceAction("add-member")]
    ContainerDetailResult AddMember(IVisioBatch batch, int pageIndex, string containerName, string memberShapeName, int addOptions = 1);

    /// <summary>Remove a shape from a specific container or list container without deleting the shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="containerName">Target container or list shape name</param>
    /// <param name="memberShapeName">Member shape to remove</param>
    [ServiceAction("remove-member")]
    ContainerDetailResult RemoveMember(IVisioBatch batch, int pageIndex, string containerName, string memberShapeName);

    /// <summary>List members of one container or list container.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="containerName">Container or list shape name</param>
    /// <param name="memberFlags">0 returns all members; add 1 to exclude containers, 2 connectors, 4 callouts, 8 plain elements, 16 nested members, or 32 explicit list members</param>
    [ServiceAction("list-members")]
    ContainerMemberListResult ListMembers(IVisioBatch batch, int pageIndex, string containerName, int memberFlags = 0);

    /// <summary>List the containers and lists that include a shape as a member.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape whose memberships should be read</param>
    [ServiceAction("containers-of")]
    ContainerMembershipResult ContainersOf(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>Force a normal container to resize tightly around its members.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="containerName">Container shape name. This action does not work for list containers</param>
    [ServiceAction("fit-to-contents")]
    ContainerDetailResult FitToContents(IVisioBatch batch, int pageIndex, string containerName);

    /// <summary>Insert a shape into a list container at a 1-based position.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="listName">List container shape name</param>
    /// <param name="memberShapeName">Shape to insert into the list</param>
    /// <param name="position">1-based insertion point. 1 inserts before the first item; a value greater than the list length appends</param>
    [ServiceAction("insert-list-member")]
    ContainerDetailResult InsertListMember(IVisioBatch batch, int pageIndex, string listName, string memberShapeName, int position = 1);

    /// <summary>Drop a built-in callout and associate it with an existing shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="targetShapeName">Existing top-level shape the callout should annotate</param>
    /// <param name="masterName">Built-in callout master name, for example 'Text Callout', 'Reverse', or 'Sled'. Omitted uses 'Text Callout'</param>
    /// <param name="text">Optional callout text</param>
    [ServiceAction("drop-callout")]
    CalloutDetailResult DropCallout(IVisioBatch batch, int pageIndex, string targetShapeName, string? masterName = null, string? text = null);

    /// <summary>List callout shapes on a page.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="nestedOptions">0 includes callouts in containers and lists; 1 excludes callouts nested inside containers and lists</param>
    [ServiceAction("list-callouts")]
    CalloutListResult ListCallouts(IVisioBatch batch, int pageIndex, int nestedOptions = 0);

    /// <summary>Read one callout shape and its target association.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="calloutName">Callout shape name, as reported by list-callouts</param>
    [ServiceAction("read-callout")]
    CalloutDetailResult ReadCallout(IVisioBatch batch, int pageIndex, string calloutName);

    /// <summary>List callouts associated with a target shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Target shape whose associated callouts should be read</param>
    [ServiceAction("callouts-of")]
    CalloutAssociationResult CalloutsOf(IVisioBatch batch, int pageIndex, string shapeName);
}
