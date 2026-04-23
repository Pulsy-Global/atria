using System.Numerics;

namespace Atria.Core.Business.Models.Dto.Network;

public class LatestBlockDto
{
    public string NetworkId { get; set; }

    public BigInteger BlockNumber { get; set; }
}
