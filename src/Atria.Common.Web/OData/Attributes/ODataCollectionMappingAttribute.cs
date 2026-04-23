namespace Atria.Common.Web.OData.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ODataCollectionMappingAttribute : Attribute
{
    public string EntityCollectionProperty { get; }

    public string EntityItemProperty { get; }

    public ODataCollectionMappingAttribute(
        string entityCollectionProperty,
        string entityItemProperty)
    {
        EntityCollectionProperty = entityCollectionProperty;
        EntityItemProperty = entityItemProperty;
    }
}
