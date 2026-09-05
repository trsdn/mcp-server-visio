using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.DataRecordset;
using VisioMcp.Core.Tests.Helpers;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for Visio data recordsets (#127).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "DataRecordset")]
public sealed class DataRecordsetTests(TempDirectoryFixture fixture) : IClassFixture<TempDirectoryFixture>
{
    private readonly DataRecordsetCommands _dataRecordsets = new();

    [Fact]
    public void AddFromXmlAndReadRows_CreatesARealDataRecordset()
    {
        using var batch = CreateDocument();

        var add = _dataRecordsets.AddFromXml(batch, "Probe Data", BuildAdoXml(), addOptions: 1);
        var list = _dataRecordsets.List(batch);
        var rows = _dataRecordsets.ReadRows(batch, add.DataRecordset!.Id);

        Assert.True(add.Success, add.ErrorMessage);
        Assert.Null(add.ErrorMessage);
        Assert.NotNull(add.DataRecordset);
        Assert.Equal("Probe Data", add.DataRecordset.Name);
        Assert.Equal([1, 2], add.DataRecordset.RowIds);
        Assert.Contains(list.DataRecordsets, recordset => recordset.Id == add.DataRecordset.Id && recordset.RowCount == 2);
        Assert.Collection(
            rows.Rows,
            row => Assert.Equal(["Probe A", "Open"], row.Values),
            row => Assert.Equal(["Probe B", "Done"], row.Values));
    }

    [Fact]
    public void LinkShape_LinksAnExistingShapeToASpecificDataRow()
    {
        using var batch = CreateDocument();
        CreateNamedRectangle(batch, "DataLinkedShape");
        var add = _dataRecordsets.AddFromXml(batch, "Probe Data", BuildAdoXml(), addOptions: 1);
        var rowId = add.DataRecordset!.RowIds[1];

        var link = _dataRecordsets.LinkShape(batch, 1, "DataLinkedShape", add.DataRecordset.Id, rowId);

        Assert.True(link.Success, link.ErrorMessage);
        Assert.Null(link.ErrorMessage);
        Assert.Equal("DataLinkedShape", link.ShapeName);
        Assert.Equal(add.DataRecordset.Id, link.DataRecordsetId);
        Assert.Equal(rowId, link.RowId);
        AssertLinkedRow(batch, link.ShapeName, link.DataRecordsetId, rowId);
    }

    private IVisioBatch CreateDocument()
    {
        var path = fixture.CreateTestFile(extension: ".vsdx");
        return VisioSession.BeginBatch(path);
    }

    private static string BuildAdoXml()
    {
        return """
            <xml xmlns:s='uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882' xmlns:dt='uuid:C2F41010-65B3-11d1-A29F-00AA00C14882' xmlns:rs='urn:schemas-microsoft-com:rowset' xmlns:z='#RowsetSchema'>
            <s:Schema id='RowsetSchema'>
            <s:ElementType name='row' content='eltOnly' rs:updatable='true'>
            <s:AttributeType name='c1' rs:name='Name' rs:number='1' rs:nullable='true' rs:maydefer='true' rs:write='true'><s:datatype dt:type='string' dt:maxLength='255' rs:precision='0'/></s:AttributeType>
            <s:AttributeType name='c2' rs:name='Status' rs:number='2' rs:nullable='true' rs:maydefer='true' rs:write='true'><s:datatype dt:type='string' dt:maxLength='255' rs:precision='0'/></s:AttributeType>
            <s:extends type='rs:rowbase'/>
            </s:ElementType>
            </s:Schema>
            <rs:data>
            <z:row c1='Probe A' c2='Open'/>
            <z:row c1='Probe B' c2='Done'/>
            </rs:data>
            </xml>
            """;
    }

    private static void CreateNamedRectangle(IVisioBatch batch, string shapeName)
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
                shape = page.DrawRectangle(1, 1, 3, 2);
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

    private static void AssertLinkedRow(IVisioBatch batch, string shapeName, int dataRecordsetId, int rowId)
    {
        batch.Execute((ctx, ct) =>
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
                shape = shapes.ItemU(shapeName);
                Assert.Equal(rowId, (int)shape.GetLinkedDataRow(dataRecordsetId));
                return 0;
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
}
