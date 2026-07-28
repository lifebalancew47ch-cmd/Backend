# ============================================================
# Stage 1: Restore & Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY ["LifeBalance.DashboardService.sln", "./"]
COPY ["src/LifeBalance.Dashboard.API/LifeBalance.Dashboard.API.csproj",              "src/LifeBalance.Dashboard.API/"]
COPY ["src/LifeBalance.Dashboard.Application/LifeBalance.Dashboard.Application.csproj", "src/LifeBalance.Dashboard.Application/"]
COPY ["src/LifeBalance.Dashboard.Domain/LifeBalance.Dashboard.Domain.csproj",        "src/LifeBalance.Dashboard.Domain/"]
COPY ["src/LifeBalance.Dashboard.Infrastructure/LifeBalance.Dashboard.Infrastructure.csproj", "src/LifeBalance.Dashboard.Infrastructure/"]
COPY ["src/LifeBalance.Dashboard.Contracts/LifeBalance.Dashboard.Contracts.csproj",  "src/LifeBalance.Dashboard.Contracts/"]
COPY ["src/LifeBalance.Dashboard.Shared/LifeBalance.Dashboard.Shared.csproj",        "src/LifeBalance.Dashboard.Shared/"]
COPY ["tests/LifeBalance.Dashboard.UnitTests/LifeBalance.Dashboard.UnitTests.csproj", "tests/LifeBalance.Dashboard.UnitTests/"]
COPY ["tests/LifeBalance.Dashboard.IntegrationTests/LifeBalance.Dashboard.IntegrationTests.csproj", "tests/LifeBalance.Dashboard.IntegrationTests/"]
COPY ["Directory.Build.props", "./"]

# Restore NuGet packages
RUN dotnet restore "LifeBalance.DashboardService.sln"

# Copy all source code
COPY . .

# Build in Release
RUN dotnet build "LifeBalance.DashboardService.sln" \
    --no-restore \
    --configuration Release

# ============================================================
# Stage 2: Publish
# ============================================================
FROM build AS publish
RUN dotnet publish "src/LifeBalance.Dashboard.API/LifeBalance.Dashboard.API.csproj" \
    --no-build \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# ============================================================
# Stage 3: Final runtime image (Optimized for Render & Docker)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Security: run as non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Expose HTTP ports (Default 8080 & Render PORT compatibility)
EXPOSE 8080
EXPOSE 10000

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_GCConserveMemory=9

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/dashboard/health || exit 1

ENTRYPOINT ["dotnet", "LifeBalance.Dashboard.API.dll"]
