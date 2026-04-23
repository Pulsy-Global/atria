using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Common;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Environment;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.HttpTrigger;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Ingress;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;
using System.Text;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.ResourceFactories;

public static class FissionResourceFactory
{
    private const string FissionApiGroup = "fission.io";
    private const string FissionApiVersion = "v1";

    public static FissionFunction CreateFunction(FissionFunctionDeployment deployment, string functionsNamespace)
    {
        return new FissionFunction
        {
            ApiVersion = $"{FissionApiGroup}/{FissionApiVersion}",
            Kind = "Function",
            Metadata = new FissionMetadata
            {
                Name = deployment.Name,
                Namespace = functionsNamespace,
            },
            Spec = new FissionFunctionSpec
            {
                Environment = new FissionEnvironmentRef
                {
                    Name = deployment.Environment,
                    Namespace = functionsNamespace,
                },
                Package = new FissionPackageRef
                {
                    Packageref = new FissionPackageReference
                    {
                        Name = deployment.Name,
                        Namespace = functionsNamespace,
                    },
                },
                Entrypoint = deployment.Entrypoint,
                InvokeStrategy = new FissionInvokeStrategy(),
            },
        };
    }

    public static FissionPackage CreatePackage(FissionFunctionDeployment deployment, string functionsNamespace, string? resourceVersion = null)
    {
        return new FissionPackage
        {
            ApiVersion = $"{FissionApiGroup}/{FissionApiVersion}",
            Kind = "Package",
            Metadata = new FissionMetadata
            {
                Name = deployment.Name,
                Namespace = functionsNamespace,
                ResourceVersion = resourceVersion,
            },
            Spec = new FissionPackageSpec
            {
                Deployment = new FissionPackageDeployment
                {
                    Type = "literal",
                    Literal = Convert.ToBase64String(Encoding.UTF8.GetBytes(deployment.Code)),
                },
                Environment = new FissionEnvironmentReference
                {
                    Name = deployment.Environment,
                    Namespace = functionsNamespace,
                },
            },
            Status = new FissionPackageStatus
            {
                BuildStatus = "succeeded",
                LastUpdateTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            },
        };
    }

    public static FissionHttpTrigger CreateHttpTrigger(FissionFunctionDeployment deployment, string functionsNamespace)
    {
        var triggerName = $"{deployment.Name}-trigger";

        return new FissionHttpTrigger
        {
            ApiVersion = $"{FissionApiGroup}/{FissionApiVersion}",
            Kind = "HTTPTrigger",
            Metadata = new FissionMetadata
            {
                Name = triggerName,
                Namespace = functionsNamespace,
            },
            Spec = new FissionHttpTriggerSpec
            {
                FunctionRef = new FissionFunctionReference
                {
                    Name = deployment.Name,
                    Type = "name",
                    FunctionWeights = null,
                },
                Method = "",
                Methods = new[] { "POST" },
                RelativeUrl = $"/{deployment.Name}",
                CreateIngress = false,
                Host = "",
                Prefix = "",
                IngressConfig = new FissionIngressConfig
                {
                    Host = "*",
                    Path = $"/{deployment.Name}",
                    Tls = "",
                    Annotations = null,
                },
                Tls = false,
            },
        };
    }
}
