namespace Atria.Common.Web.OData.Attributes;

public class ODataHandleAsString : Attribute
{
    public string? MapTo { get; set; }

    public ODataHandleAsString(string? mapToField = null)
    {
        MapTo = mapToField;
    }
}
