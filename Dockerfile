# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les csproj d'abord pour tirer profit du cache Docker sur le restore.
COPY ["src/AggregatorPlatform.API/AggregatorPlatform.API.csproj",                     "src/AggregatorPlatform.API/"]
COPY ["src/AggregatorPlatform.Application/AggregatorPlatform.Application.csproj",     "src/AggregatorPlatform.Application/"]
COPY ["src/AggregatorPlatform.Domain/AggregatorPlatform.Domain.csproj",               "src/AggregatorPlatform.Domain/"]
COPY ["src/AggregatorPlatform.Infrastructure/AggregatorPlatform.Infrastructure.csproj","src/AggregatorPlatform.Infrastructure/"]

RUN dotnet restore "src/AggregatorPlatform.API/AggregatorPlatform.API.csproj"

# Copier tout le code et publier.
COPY . .
WORKDIR /src/src/AggregatorPlatform.API
RUN dotnet publish "AggregatorPlatform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Healthcheck TCP builtin bash : l'image aspnet n'embarque ni wget ni curl.
# /bin/sh ne supporte PAS /dev/tcp, donc on invoque explicitement bash.
HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=15s \
  CMD bash -c "exec 3<>/dev/tcp/localhost/8080 && echo OK || exit 1"

ENTRYPOINT ["dotnet", "AggregatorPlatform.API.dll"]
