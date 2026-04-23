using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Business.Models.Dto.Network;

public class EnvironmentDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public AtriaDataType[] AvailableDatasets { get; set; }
}
