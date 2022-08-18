#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS build
WORKDIR /sln
COPY *.sln .
COPY ./src/TowerBridge.API/*.csproj /sln/src/TowerBridge.API/
COPY ./tests/TowerBridge.Tests/*.csproj /sln/tests/TowerBridge.Tests/
RUN dotnet restore

FROM build as test
COPY . .
RUN dotnet test -c Debug

FROM build AS publish
COPY ./src/ ./src/
RUN dotnet publish -c Release -o /app/publish

FROM build AS vulnscan
COPY --from=aquasec/trivy:latest /usr/local/bin/trivy /usr/local/bin/trivy
RUN trivy filesystem --exit-code 1 --no-progress

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TowerBridge.API.dll"]
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source="https://github.com/milkyware/towerbridge"
LABEL org.opencontainers.image.title="TowerBridge API"
LABEL org.opencontainers.image.documentation="https://github.com/milkyware/towerbridge/blob/main/README.md"
VOLUME /logs
