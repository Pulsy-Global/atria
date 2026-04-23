using Atria.Business.Models;
using Atria.Common.KV.Models;
using Atria.Common.Models.Options;
using Atria.Core.Business.Mapper.Helpers;
using Atria.Core.Business.Models.Dto.Feed;
using Atria.Core.Business.Models.Dto.Kv;
using Atria.Core.Business.Models.Dto.Network;
using Atria.Core.Business.Models.Dto.Output;
using Atria.Core.Business.Models.Dto.Output.Config;
using Atria.Core.Business.Models.Dto.Tag;
using Atria.Core.Data.Entities.Deploys;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.Entities.Tags;
using Mapster;

namespace Atria.Core.Business.Mapper;

public class BusinessMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        RegisterOutputConfigMappings(config);
        RegisterOutputMappings(config);
        RegisterFeedMappings(config);
        RegisterDeployMappings(config);
        RegisterNetworkMappings(config);
        RegisterTagMappings(config);
        RegisterKvMappings(config);
    }

    private static void RegisterOutputConfigMappings(TypeAdapterConfig config)
    {
        ConfigMappingHelpers.RegisterMappings(config);
    }

    private static void RegisterOutputMappings(TypeAdapterConfig config)
    {
        var mapEntityToDto = (OutputConfigBase? config, OutputType type) =>
        {
            if (config == null)
            {
                return null;
            }

            var (entityType, dtoType) = ConfigMappingHelpers.GetMapping(type);

            return (ConfigBaseDto?)config.Adapt(config.GetType(), dtoType);
        };

        var mapDtoToEntity = (ConfigBaseDto? dto, OutputType type) =>
        {
            if (dto == null)
            {
                return null;
            }

            var (entityType, dtoType) = ConfigMappingHelpers.GetMapping(type);

            return (OutputConfigBase?)dto.Adapt(dto.GetType(), entityType);
        };

        config.NewConfig<Output, OutputDto>()
            .Map(dest => dest.Config, src => mapEntityToDto(src.Config, src.Type))
            .Map(dest => dest.TagIds, src => src.OutputTags.Select(ot => ot.TagId).ToList());

        config.NewConfig<CreateOutputDto, Output>()
            .Map(dest => dest.Config, src => mapDtoToEntity(src.Config, src.Type))
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.OutputTags);

        config.NewConfig<UpdateOutputDto, Output>()
            .Map(dest => dest.Config, src => mapDtoToEntity(src.Config, src.Type))
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.OutputTags);

        config.NewConfig<OutputDto, Output>()
            .Map(dest => dest.Config, src => mapDtoToEntity(src.Config, src.Type))
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.OutputTags);
    }

    private static void RegisterFeedMappings(TypeAdapterConfig config)
    {
        config.NewConfig<CreateFeedDto, Feed>()
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.FeedTags);

        config.NewConfig<UpdateFeedDto, Feed>()
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.FeedTags);

        config.NewConfig<Feed, FeedDto>()
            .Map(dest => dest.StartBlockNumeric, src => src.StartBlock)
            .Map(dest => dest.StartBlock, src => src.StartBlock.HasValue
                ? src.StartBlock.Value.ToString()
                : null)
            .Map(dest => dest.EndBlockNumeric, src => src.EndBlock)
            .Map(dest => dest.EndBlock, src => src.EndBlock.HasValue
                ? src.EndBlock.Value.ToString()
                : null)
            .Map(dest => dest.TagIds, src => src.FeedTags.Select(ft => ft.TagId).ToList())
            .Map(dest => dest.OutputIds, src => src.FeedOutputs.Select(fo => fo.OutputId).ToList());

        config.NewConfig<FeedDto, Feed>()
            .Map(dest => dest.StartBlock, src => src.StartBlockNumeric)
            .Map(dest => dest.EndBlock, src => src.EndBlockNumeric)
            .Map(dest => dest.Hash, src => src.Name)
            .Ignore(dest => dest.FeedTags);

        config.NewConfig<TestRequestDto, TestRequest>();
        config.NewConfig<TestResultDto, TestResult>();

        config.NewConfig<ExecutionErrorDto, ExecutionError>();
    }

    private static void RegisterDeployMappings(TypeAdapterConfig config)
    {
        config.NewConfig<Deploy, DeployDto>();
    }

    private static void RegisterNetworkMappings(TypeAdapterConfig config)
    {
        config.NewConfig<NetworksConfig, NetworksDto>()
            .Map(dest => dest.Networks, src => src.Networks.Values.ToArray());

        config.NewConfig<NetworkGroupOptions, NetworkDto>()
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.IconUrl, src => src.IconUrl)
            .Map(dest => dest.Environments, src => src.Environments.ToArray());

        config.NewConfig<NetworkOptions, EnvironmentDto>();
    }

    private static void RegisterTagMappings(TypeAdapterConfig config)
    {
        config.NewConfig<CreateTagDto, Tag>();
        config.NewConfig<UpdateTagDto, Tag>();
        config.NewConfig<Tag, TagDto>();
        config.NewConfig<TagDto, Tag>();
    }

    private static void RegisterKvMappings(TypeAdapterConfig config)
    {
        config.NewConfig<KvBucketEntry, BucketItemDto>();
        config.NewConfig<KvBucketValuesResult, BucketValuesDto>();
    }
}
