# ============================================================
# Stage 1: Restore & Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY ["LifeBalance.Reporting.sln", "./"]
COPY ["src/LifeBalance.Reporting.API/LifeBalance.Reporting.API.csproj",           "src/LifeBalance.Reporting.API/"]
COPY ["src/LifeBalance.Reporting.Application/LifeBalance.Reporting.Application.csproj", "src/LifeBalance.Reporting.Application/"]
COPY ["src/LifeBalance.Reporting.Domain/LifeBalance.Reporting.Domain.csproj",     "src/LifeBalance.Reporting.Domain/"]
COPY ["src/LifeBalance.Reporting.Infrastructure/LifeBalance.Reporting.Infrastructure.csproj", "src/LifeBalance.Reporting.Infrastructure/"]
COPY ["src/LifeBalance.Reporting.Contracts/LifeBalance.Reporting.Contracts.csproj", "src/LifeBalance.Reporting.Contracts/"]
COPY ["src/LifeBalance.Reporting.Shared/LifeBalance.Reporting.Shared.csproj",     "src/LifeBalance.Reporting.Shared/"]
COPY ["tests/LifeBalance.Reporting.UnitTests/LifeBalance.Reporting.UnitTests.csproj", "tests/LifeBalance.Reporting.UnitTests/"]
COPY ["tests/LifeBalance.Reporting.IntegrationTests/LifeBalance.Reporting.IntegrationTests.csproj", "tests/LifeBalance.Reporting.IntegrationTests/"]
COPY ["Directory.Build.props", "./"]

# Restore NuGet packages
RUN dotnet restore "LifeBalance.Reporting.sln"

# Copy all source code
COPY . .

# Build in Release
RUN dotnet build "LifeBalance.Reporting.sln" \
    --no-restore \
    --configuration Release

# ============================================================
# Stage 2: Publish
# ============================================================
FROM build AS publish
RUN dotnet publish "src/LifeBalance.Reporting.API/LifeBalance.Reporting.API.csproj" \
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
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LifeBalance.Reporting.API.dll"]
