FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Atria.Feed.Delivery/Atria.Feed.Delivery.csproj", "src/Atria.Feed.Delivery/"]
COPY ["src/Atria.Common/Atria.Common.csproj", "src/Atria.Common/"]
COPY ["src/Atria.Common.Web/Atria.Common.Web.csproj", "src/Atria.Common.Web/"]
COPY ["src/Atria.Common.Worker/Atria.Common.Worker.csproj", "src/Atria.Common.Worker/"]
COPY ["src/Atria.Contracts/Atria.Contracts.csproj", "src/Atria.Contracts/"]
COPY ["src/Directory.Packages.props", "src/"]
COPY ["src/Directory.Build.props", "src/"]

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Atria.Feed.Delivery/Atria.Feed.Delivery.csproj"

COPY src/ src/
COPY config/ config/

FROM build AS publish

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Atria.Feed.Delivery/Atria.Feed.Delivery.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Atria.Feed.Delivery.dll"]
