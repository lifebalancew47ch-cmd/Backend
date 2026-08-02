# Use .NET 9 SDK to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy all project files and restore dependencies
COPY Auth_Profile.sln ./
COPY src/Auth.Api/Auth.Api.csproj src/Auth.Api/
COPY src/Auth.Application/Auth.Application.csproj src/Auth.Application/
COPY src/Auth.Domain/Auth.Domain.csproj src/Auth.Domain/
COPY src/Auth.Infrastructure/Auth.Infrastructure.csproj src/Auth.Infrastructure/
COPY src/Auth.Shared/Auth.Shared.csproj src/Auth.Shared/
COPY tests/UnitTests/UnitTests.csproj tests/UnitTests/
COPY tests/IntegrationTests/IntegrationTests.csproj tests/IntegrationTests/

RUN dotnet restore

# Copy all source files and publish release
COPY . ./
RUN dotnet publish src/Auth.Api/Auth.Api.csproj -c Release -o /app/publish

# Use .NET 9 ASP.NET Core Runtime for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Security: run as non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

# Expose port and configure ASP.NET Core URLs (Render compatibility)
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Auth.Api.dll"]
