FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/Shared.BuildingBlocks/Shared.BuildingBlocks.csproj src/Shared.BuildingBlocks/
COPY src/User.Api/User.Api.csproj src/User.Api/
RUN dotnet restore src/User.Api/User.Api.csproj

COPY src/Shared.BuildingBlocks src/Shared.BuildingBlocks
COPY src/User.Api src/User.Api
RUN dotnet publish src/User.Api/User.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "User.Api.dll"]
