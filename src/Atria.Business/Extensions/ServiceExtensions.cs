using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Atria.Business.Models.Enums;
using Atria.Business.Models.Options;
using Atria.Business.Services.DataServices;
using Atria.Business.Services.DataServices.Interfaces;
using Atria.Business.Services.Deployment;
using Atria.Business.Services.Deployment.Interfaces;
using Atria.Business.Services.Messaging;
using Atria.Business.Services.Messaging.Interfaces;
using Atria.Business.Services.Namespaces;
using Atria.Business.Services.Namespaces.Interfaces;
using Atria.Business.Services.Storage;
using Atria.Business.Services.Storage.Interfaces;
using Atria.Common.Messaging.Extensions;
using Atria.Common.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

namespace Atria.Business.Extensions;

public static class ServiceExtensions
{
    public static void AddBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMessaging(configuration);

        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        services.Configure<LocalStorageOptions>(
            configuration.GetSection(LocalStorageOptions.SectionName));

        services.Configure<S3StorageOptions>(
            configuration.GetSection(S3StorageOptions.SectionName));

        services.Configure<NetworksConfig>(configuration);

        var localStorageSection = configuration.GetSection(LocalStorageOptions.SectionName);
        var s3StorageSection = configuration.GetSection(S3StorageOptions.SectionName);
        var enabledType = configuration.GetValue<FileSystemType>("FileSystem:Type");

        if (s3StorageSection.Exists() && enabledType == FileSystemType.S3)
        {
            var s3Options = s3StorageSection.Get<S3StorageOptions>() !;

            services.AddSingleton<IAmazonS3>(provider =>
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region ?? "us-east-1"),
                    ServiceURL = s3Options.Endpoint,
                    ForcePathStyle = s3Options.ForcePathStyle,
                    SignatureMethod = SigningAlgorithm.HmacSHA256,
                };

                return new AmazonS3Client(
                    s3Options.AccessKey,
                    s3Options.SecretKey,
                    config);
            });

            services.AddTransient<IFileSystemService, S3FileSystemService>();
        }
        else if (localStorageSection.Exists())
        {
            services.AddTransient<IFileSystemService, LocalFileSystemService>();
        }
        else
        {
            throw new ConfigurationErrorsException("Invalid file system configuration");
        }

        services.Configure<KvOptions>(configuration.GetSection("Ekv"));
        services.AddSingleton<IResourceNamespaceResolver, ResourceNamespaceResolver>();

        services.AddTransient<IFeedDataService, FeedDataService>();
        services.AddTransient<IFeedMessageService, FeedMessageService>();
        services.AddTransient<IOutputDataService, OutputDataService>();
        services.AddTransient<IDeployDataService, DeployDataService>();

        services.AddTransient<IFeedEventPublisher, FeedEventPublisher>();
        services.AddTransient<IOutputEventPublisher, OutputEventPublisher>();
    }
}
