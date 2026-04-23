FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 4300

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Atria.Core.Api/Atria.Core.Api.csproj", "src/Atria.Core.Api/"]
COPY ["src/Atria.Business/Atria.Business.csproj", "src/Atria.Business/"]
COPY ["src/Atria.Common.Web/Atria.Common.Web.csproj", "src/Atria.Common.Web/"]
COPY ["src/Atria.Common/Atria.Common.csproj", "src/Atria.Common/"]
COPY ["src/Atria.Common.Worker/Atria.Common.Worker.csproj", "src/Atria.Common.Worker/"]
COPY ["src/Atria.Core.Business/Atria.Core.Business.csproj", "src/Atria.Core.Business/"]
COPY ["src/Atria.Core.Data/Atria.Core.Data.csproj", "src/Atria.Core.Data/"]
COPY ["src/Atria.Orchestrator/Atria.Orchestrator.csproj", "src/Atria.Orchestrator/"]
COPY ["src/Atria.Contracts/Atria.Contracts.csproj", "src/Atria.Contracts/"]
COPY ["src/Directory.Packages.props", "src/"]
COPY ["src/Directory.Build.props", "src/"]

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Atria.Core.Api/Atria.Core.Api.csproj"

COPY src/ src/
COPY config/ config/

FROM build AS publish

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Atria.Core.Api/Atria.Core.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Atria.Core.Api.dll"]
