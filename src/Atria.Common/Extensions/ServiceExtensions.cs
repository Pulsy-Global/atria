using Atria.Common.Models.Options;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Atria.Common.Extensions;

public static class ServiceExtensions
{
    public static void RegisterAllTypes<T>(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var typesFromAssemblies = assembly.GetTypesImplementedInterface<T>();

        foreach (var type in typesFromAssemblies)
        {
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }
    }

    public static void AddMapsterService(this IServiceCollection services)
    {
        if (services.Any(sd => sd.ServiceType == typeof(IMapper)))
        {
            return;
        }

        var config = new TypeAdapterConfig();

        bool IsConcreteRegister(Type t) =>
            t is { IsInterface: false, IsAbstract: false } && typeof(IRegister).IsAssignableFrom(t);

        IRegister CreateRegister(Type t) =>
            (IRegister)Activator.CreateInstance(t) !;

        var registers = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(IsConcreteRegister)
            .Select(CreateRegister);

        foreach (var reg in registers)
        {
            reg.Register(config);
        }

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }

    public static void AddNetworksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NetworkGroupOptions>()
            .Bind(configuration);
    }

    public static void ConfigureCommonOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNetworksConfiguration(configuration);
    }
}
