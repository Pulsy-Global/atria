namespace Atria.Core.Business.Models.Dto.Output.Config.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DynamicTypeAttribute : Attribute
{
    public string TypePropertyName { get; }

    public DynamicTypeAttribute(string typePropertyName)
    {
        TypePropertyName = typePropertyName;
    }
}
