using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Business.Mapper.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ConfigMappingAttribute : Attribute
{
    public OutputType OutputType { get; }
    public Type EntityType { get; }
    public Type DtoType { get; }

    public ConfigMappingAttribute(OutputType outputType, Type entityType, Type dtoType)
    {
        OutputType = outputType;
        EntityType = entityType;
        DtoType = dtoType;
    }
}
