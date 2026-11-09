# =============================================================================
# NEXUS API - ASP.NET Core Web API (.NET 10)
# Build:  docker build -f backend/Dockerfile -t nexus-api .
# =============================================================================

# --------------------------- Stage 1: build --------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy riêng file project trước để tận dụng cache layer của Docker.
COPY backend/Nexus.slnx ./
COPY backend/src/Nexus.Api/Nexus.Api.csproj src/Nexus.Api/
RUN dotnet restore src/Nexus.Api/Nexus.Api.csproj

# Copy toàn bộ mã nguồn backend rồi publish.
COPY backend/ ./
RUN dotnet publish src/Nexus.Api/Nexus.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --------------------------- Stage 2: runtime ------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Chạy dưới user không phải root có sẵn trong image aspnet.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

ENTRYPOINT ["dotnet", "Nexus.Api.dll"]