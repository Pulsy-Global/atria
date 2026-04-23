using Atria.Common.Models.Generic;
using Mapster;

namespace Atria.Core.Data.Mapper;

public class DataMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig(typeof(PagedList<>), typeof(PagedList<>));
    }
}
