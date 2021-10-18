#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:3.1-buster AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:3.1-alpine
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TowerBridge.API.dll"]
EXPOSE 80
EXPOSE 443