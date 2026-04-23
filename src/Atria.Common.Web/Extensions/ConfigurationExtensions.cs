using Atria.Common.Extensions;
using Atria.Common.Web.Models.Dto;
using Atria.Common.Web.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Atria.Common.Web.Extensions;

public static class ConfigurationExtensions
{
    public static FileContentResult GenerateSpaConfiguration(this ConfigurationOptions configOptions)
    {
        var assembly = Assembly.GetEntryAssembly();

        var config = new ConfigurationDto(configOptions)
        {
            Version = assembly?.GetAssemblyVersion(),
            InformationalVersion = assembly?.GetInformationalVersion(),
        };

        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        var serializedConfig = JsonConvert.SerializeObject(config, jsonSettings);

        var js = $"var appConfig = {serializedConfig};";
        var bytes = System.Text.Encoding.UTF8.GetBytes(js);

        return new FileContentResult(bytes, "text/javascript");
    }
}
