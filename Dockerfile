# syntax=docker/dockerfile:1
# Multi-stage build. No -r/RID on publish: the output is portable framework-dependent IL that runs
# under either the amd64 or arm64 runtime image below, so buildx's per-platform base image
# selection is all that's needed for linux/amd64 + linux/arm64 - no cross-compilation step.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props MusicOrganizer.sln ./
COPY src/ ./src/
RUN dotnet restore src/MusicOrganizer.Cli/MusicOrganizer.Cli.csproj

RUN dotnet publish src/MusicOrganizer.Cli/MusicOrganizer.Cli.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

USER app
ENTRYPOINT ["dotnet", "musicorganizer.dll"]
