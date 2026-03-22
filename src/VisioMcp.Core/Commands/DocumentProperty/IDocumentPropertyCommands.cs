using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.DocumentProperty;

/// <summary>
/// Document property management: read and write Visio document metadata like title, creator, subject, and keywords.
/// </summary>
[ServiceCategory("docproperty")]
[McpTool("docproperty", Title = "Document Properties", Destructive = false, Category = "metadata",
    Description = "Read and write Visio document metadata: title, author/creator, subject, keywords, comments, company, category. "
    + "Use 'get' for built-in document properties. Use 'set' (omit or pass empty values to leave unchanged). "
    + "'get-custom'/'set-custom' manage document-level custom metadata stored in the document Shape Data section.")]
public interface IDocumentPropertyCommands
{
    /// <summary>
    /// Get built-in Visio document properties (title, creator, subject, keywords, comments, company, category).
    /// </summary>
    [ServiceAction("get")]
    DocumentPropertyResult GetAll(IVisioBatch batch);

    /// <summary>
    /// Set built-in Visio document properties. Pass null or empty to leave a property unchanged.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="title">Document title</param>
    /// <param name="subject">Subject or topic</param>
    /// <param name="author">Creator/author name</param>
    /// <param name="keywords">Keywords for search (comma-separated)</param>
    /// <param name="comments">Description or comments</param>
    /// <param name="company">Company or organization name</param>
    /// <param name="category">Category</param>
    [ServiceAction("set")]
    OperationResult SetAll(IVisioBatch batch, string title, string subject, string author, string keywords, string comments, string company, string category);

    /// <summary>
    /// Get a custom document property by name.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="propertyName">Custom property name. Stored as document Shape Data when written through this API.</param>
    [ServiceAction("get-custom")]
    OperationResult GetCustom(IVisioBatch batch, string propertyName);

    /// <summary>
    /// Set a custom document property (creates if not exists).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="propertyName">Custom property name. Names that are not valid Shape Data row names are normalized automatically.</param>
    /// <param name="propertyValue">Property value (string)</param>
    [ServiceAction("set-custom")]
    OperationResult SetCustom(IVisioBatch batch, string propertyName, string propertyValue);
}
