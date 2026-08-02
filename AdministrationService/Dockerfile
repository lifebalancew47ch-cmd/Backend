FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Api/LifeBalance.Administration.Api/LifeBalance.Administration.Api.csproj", "src/Api/LifeBalance.Administration.Api/"]
COPY ["src/Core/LifeBalance.Administration.Application/LifeBalance.Administration.Application.csproj", "src/Core/LifeBalance.Administration.Application/"]
COPY ["src/Core/LifeBalance.Administration.Domain/LifeBalance.Administration.Domain.csproj", "src/Core/LifeBalance.Administration.Domain/"]
COPY ["src/Infrastructure/LifeBalance.Administration.Infrastructure/LifeBalance.Administration.Infrastructure.csproj", "src/Infrastructure/LifeBalance.Administration.Infrastructure/"]
RUN dotnet restore "src/Api/LifeBalance.Administration.Api/LifeBalance.Administration.Api.csproj"

COPY . .
WORKDIR "/src/src/Api/LifeBalance.Administration.Api"
RUN dotnet build "LifeBalance.Administration.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LifeBalance.Administration.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Security: run as non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

ENTRYPOINT ["dotnet", "LifeBalance.Administration.Api.dll"]
