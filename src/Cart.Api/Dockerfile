FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/Shared.BuildingBlocks/Shared.BuildingBlocks.csproj src/Shared.BuildingBlocks/
COPY src/Cart.Api/Cart.Api.csproj src/Cart.Api/
RUN dotnet restore src/Cart.Api/Cart.Api.csproj

COPY src/Shared.BuildingBlocks src/Shared.BuildingBlocks
COPY src/Cart.Api src/Cart.Api
RUN dotnet publish src/Cart.Api/Cart.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cart.Api.dll"]
