﻿FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source="https://github.com/milkyware/towerbridge"
LABEL org.opencontainers.image.title="TowerBridge API"
LABEL org.opencontainers.image.documentation="https://github.com/milkyware/towerbridge/blob/main/README.md"
VOLUME /logs

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /sln
COPY *.sln .
COPY ./src/TowerBridge.API/*.csproj /sln/src/TowerBridge.API/
COPY ./tests/TowerBridge.Tests/*.csproj /sln/tests/TowerBridge.Tests/
RUN dotnet restore

FROM build AS test
COPY . .
RUN dotnet test -c Debug

FROM build AS publish
COPY ./src/ ./src/
RUN dotnet publish -c Release -o /app/publish

FROM base AS scan
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=aquasec/trivy:latest /usr/local/bin/trivy /usr/local/bin/trivy
RUN trivy filesystem --exit-code 1 --no-progress /

FROM base AS final
WORKDIR /app
COPY --from=scan /app .
ENTRYPOINT ["dotnet", "TowerBridge.API.dll"]