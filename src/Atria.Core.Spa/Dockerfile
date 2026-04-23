FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 7150

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs

# Install Angular CLI globally
RUN npm install -g @angular/cli@19.0.6

# Copy .NET project files for restore
COPY ["src/Atria.Core.Spa/Atria.Core.Spa.csproj", "src/Atria.Core.Spa/"]
COPY ["src/Atria.Common.Web/Atria.Common.Web.csproj", "src/Atria.Common.Web/"]
COPY ["src/Atria.Common/Atria.Common.csproj", "src/Atria.Common/"]
COPY ["src/Directory.Packages.props", "src/"]
COPY ["src/Directory.Build.props", "src/"]

# Restore .NET dependencies
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Atria.Core.Spa/Atria.Core.Spa.csproj"

# Copy package.json from wwwroot for npm install
COPY ["src/Atria.Core.Spa/wwwroot/package.json", "src/Atria.Core.Spa/wwwroot/"]

# Install npm dependencies
WORKDIR "/src/src/Atria.Core.Spa/wwwroot"
RUN --mount=type=cache,target=/root/.npm,sharing=locked \
    npm install

# Copy all source code
WORKDIR "/src"
COPY src/ src/

FROM build AS publish
WORKDIR "/src/src/Atria.Core.Spa"

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "Atria.Core.Spa.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Atria.Core.Spa.dll"]
