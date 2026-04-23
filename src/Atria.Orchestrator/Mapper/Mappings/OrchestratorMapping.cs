using Atria.Core.Data.Entities.Feeds;
using Atria.Orchestrator.Models.Dto.Feed;
using Mapster;
using System.Numerics;

namespace Atria.Orchestrator.Mapper.Mappings;

public class OrchestratorMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FeedManifest, Feed>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.NetworkId, src => src.Config.Source.NetworkId)
            .Map(dest => dest.DataType, src => src.Config.Source.DataType)
            .Map(dest => dest.FilterPath, src => src.Config.Runtime.Filter.Path)
            .Map(dest => dest.FunctionPath, src => src.Config.Runtime.Function.Path)
            .Map(
                dest => dest.StartBlock,
                src => string.IsNullOrEmpty(src.Config.Source.StartBlock)
                    ? (BigInteger?)null
                    : BigInteger.Parse(src.Config.Source.StartBlock))
            .Map(
                dest => dest.EndBlock,
                src => string.IsNullOrEmpty(src.Config.Source.EndBlock)
                    ? (BigInteger?)null
                    : BigInteger.Parse(src.Config.Source.EndBlock))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Status);
    }
}
