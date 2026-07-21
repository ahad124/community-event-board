# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching)
COPY EventBoard.Api/EventBoard.Api.csproj EventBoard.Api/
RUN dotnet restore EventBoard.Api/EventBoard.Api.csproj

# Copy the rest of the API source and publish
COPY EventBoard.Api/ EventBoard.Api/
RUN dotnet publish EventBoard.Api/EventBoard.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for Docker health checks
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "EventBoard.Api.dll"]