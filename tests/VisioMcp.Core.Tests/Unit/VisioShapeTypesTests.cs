using VisioMcp.Core.Commands;
using Xunit;

namespace VisioMcp.Core.Tests.Unit;

/// <summary>
/// Guards the Visio <c>Shape.Type</c> mapping.
///
/// This replaces <c>ShapeHelpersTests</c>, which asserted the PowerPoint <c>MsoShapeType</c> table
/// was reproduced faithfully — it was, and it was the wrong table. Every value overlapped a real
/// <c>VisShapeTypes</c> value with a different meaning, so callers received a confident, wrong
/// answer rather than an error. The tests below pin the values confirmed against a live Visio 16.0
/// instance and explicitly assert the three that used to be misreported.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "false")]
[Trait("Feature", "Shape")]
public class VisioShapeTypesTests
{
    [Theory]
    [InlineData(0, "Invalid")]
    [InlineData(1, "Page")]
    [InlineData(2, "Group")]
    [InlineData(3, "Shape")]
    [InlineData(4, "ForeignObject")]
    [InlineData(5, "Guide")]
    [InlineData(6, "Document")]
    public void GetName_ReturnsTheVisioNameForEachDocumentedType(int shapeType, string expected)
    {
        Assert.Equal(expected, VisioShapeTypes.GetName(shapeType));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(17)]
    [InlineData(-1)]
    public void GetName_ReportsUndocumentedValuesRatherThanGuessing(int shapeType)
    {
        Assert.Equal($"Unknown({shapeType})", VisioShapeTypes.GetName(shapeType));
    }

    /// <summary>
    /// The three values the PowerPoint table got wrong, named explicitly so a regression is
    /// legible rather than a diff on an integer.
    /// </summary>
    [Fact]
    public void GetName_DoesNotReproduceThePowerPointMapping()
    {
        // A drawn rectangle. MsoShapeType 3 is msoChart.
        Assert.Equal("Shape", VisioShapeTypes.GetName(VisioShapeTypes.Shape));

        // A grouped selection. MsoShapeType 2 is msoCallout.
        Assert.Equal("Group", VisioShapeTypes.GetName(VisioShapeTypes.Group));

        // An imported image or OLE object. MsoShapeType 4 is msoComment.
        Assert.Equal("ForeignObject", VisioShapeTypes.GetName(VisioShapeTypes.ForeignObject));
    }

    [Fact]
    public void Constants_MatchTheirMappedNames()
    {
        Assert.Equal("Invalid", VisioShapeTypes.GetName(VisioShapeTypes.Invalid));
        Assert.Equal("Page", VisioShapeTypes.GetName(VisioShapeTypes.Page));
        Assert.Equal("Guide", VisioShapeTypes.GetName(VisioShapeTypes.Guide));
        Assert.Equal("Document", VisioShapeTypes.GetName(VisioShapeTypes.Document));
    }
}
