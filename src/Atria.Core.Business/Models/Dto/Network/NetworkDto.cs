namespace Atria.Core.Business.Models.Dto.Network;

public class NetworkDto
{
    public string Title { get; set; }
    public string IconUrl { get; set; }
    public EnvironmentDto[] Environments { get; set; }
}
