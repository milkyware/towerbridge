# syntax=docker/dockerfile:1@sha256:4edf897a3ffa55b89f906fc8cc78afdb3f1834cc9c7083565e611a8a7d5fe99e

ARG CONFIGURATION=Release
ARG REVISION
ARG VERSION

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS build
ARG CONFIGURATION
ARG REVISION
ARG VERSION
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV NUGET_XMLDOC_MODE=skip
WORKDIR /work

COPY *.slnx ./
COPY --parents src/*/*.csproj tests/*/*.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore TowerBridge.slnx

COPY src/ src/
COPY tests/ tests/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet build TowerBridge.slnx \
        -c "$CONFIGURATION" \
        ${REVISION:+-p:SourceRevisionId=$REVISION} \
        ${VERSION:+-p:Version=$VERSION} \
        --no-restore \
        --nologo

FROM build AS test
ARG CONFIGURATION
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet test TowerBridge.slnx \
        -c "$CONFIGURATION" \
        --no-build \
        --nologo

FROM test AS publish
ARG CONFIGURATION
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/TowerBridge.API/TowerBridge.API.csproj \
        -c "$CONFIGURATION" \
        -o /app/publish \
        --no-build \
        --no-restore \
        --nologo && \
    mkdir -p /logs

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra@sha256:cae5b962bfb786606def03d250c6a645cc98839095c89e806e4bd8978a68115b AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
COPY --from=publish --chown=$APP_UID:$APP_UID /logs /logs
ENV TZ=UTC
EXPOSE 8080
VOLUME ["/logs"]
USER $APP_UID
ARG REVISION
ARG VERSION
LABEL org.opencontainers.image.authors="MilkyWare" \
      org.opencontainers.image.base.name="mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra" \
      org.opencontainers.image.description="Web API for Tower Bridge lift times" \
      org.opencontainers.image.documentation="https://github.com/milkyware/towerbridge/blob/main/README.md" \
      org.opencontainers.image.revision="${REVISION}" \
      org.opencontainers.image.source="https://github.com/milkyware/towerbridge" \
      org.opencontainers.image.title="TowerBridge API" \
      org.opencontainers.image.version="${VERSION}"
ENTRYPOINT ["dotnet", "TowerBridge.API.dll"]
