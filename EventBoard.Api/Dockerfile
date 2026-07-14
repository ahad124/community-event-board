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

# The app listens on 8080 (Kestrel default in the aspnet:8.0 image)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

# Ensure the uploads directory exists (also mounted as a volume via compose)
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "EventBoard.Api.dll"]
