FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les .csproj en premier pour optimiser le cache Docker
COPY ["src/AggregatorPlatform.API/AggregatorPlatform.API.csproj", "src/AggregatorPlatform.API/"]
COPY ["src/AggregatorPlatform.Application/AggregatorPlatform.Application.csproj", "src/AggregatorPlatform.Application/"]
COPY ["src/AggregatorPlatform.Domain/AggregatorPlatform.Domain.csproj", "src/AggregatorPlatform.Domain/"]
COPY ["src/AggregatorPlatform.Infrastructure/AggregatorPlatform.Infrastructure.csproj", "src/AggregatorPlatform.Infrastructure/"]

RUN dotnet restore "src/AggregatorPlatform.API/AggregatorPlatform.API.csproj"

# Copier tout le code et build
COPY . .
WORKDIR /src/src/AggregatorPlatform.API
RUN dotnet build "AggregatorPlatform.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AggregatorPlatform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Healthcheck Docker
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AggregatorPlatform.API.dll"]
