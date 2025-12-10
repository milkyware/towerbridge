ARG CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source="https://github.com/milkyware/towerbridge"
LABEL org.opencontainers.image.title="TowerBridge API"
LABEL org.opencontainers.image.documentation="https://github.com/milkyware/towerbridge/blob/main/README.md"
VOLUME /logs

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG CONFIGURATION
WORKDIR /work
COPY *.slnx .
COPY ./src/ src/
RUN dotnet build src/TowerBridge.API/TowerBridge.API.csproj -c $CONFIGURATION --nologo

FROM build AS test
ARG CONFIGURATION
COPY . .
RUN dotnet test -c $CONFIGURATION

FROM test AS publish
ARG CONFIGURATION
RUN dotnet publish src/TowerBridge.API/TowerBridge.API.csproj -c $CONFIGURATION -o /app/publish --no-build --nologo

FROM base AS scan
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=aquasec/trivy:latest /usr/local/bin/trivy /usr/local/bin/trivy
RUN trivy fs --exit-code 1 --severity CRITICAL,HIGH --no-progress /
RUN rm -rf /usr/local/bin/trivy

FROM base AS final
WORKDIR /app
COPY --from=scan /app .
ENTRYPOINT ["dotnet", "TowerBridge.API.dll"]