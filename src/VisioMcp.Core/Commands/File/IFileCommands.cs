using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.File;

/// <summary>
/// File management commands for Visio documents.
/// Handles file validation and metadata retrieval.
/// </summary>
[ServiceCategory("file")]
[NoSession]
public interface IFileCommands
{
    /// <summary>
    /// Validate a Visio document and return metadata (size, page count, macro status).
    /// </summary>
    /// <param name="filePath">Path to the .vsdx, .vsdm or .vssx file</param>
    [ServiceAction("test")]
    FileValidationInfo Test(string filePath);
}
