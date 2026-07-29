FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Api/LifeBalance.OrganizationSaaS.Api/LifeBalance.OrganizationSaaS.Api.csproj", "src/Api/LifeBalance.OrganizationSaaS.Api/"]
COPY ["src/Infrastructure/LifeBalance.OrganizationSaaS.Infrastructure/LifeBalance.OrganizationSaaS.Infrastructure.csproj", "src/Infrastructure/LifeBalance.OrganizationSaaS.Infrastructure/"]
COPY ["src/Core/LifeBalance.OrganizationSaaS.Application/LifeBalance.OrganizationSaaS.Application.csproj", "src/Core/LifeBalance.OrganizationSaaS.Application/"]
COPY ["src/Core/LifeBalance.OrganizationSaaS.Domain/LifeBalance.OrganizationSaaS.Domain.csproj", "src/Core/LifeBalance.OrganizationSaaS.Domain/"]
RUN dotnet restore "src/Api/LifeBalance.OrganizationSaaS.Api/LifeBalance.OrganizationSaaS.Api.csproj"

COPY . .
WORKDIR "/src/src/Api/LifeBalance.OrganizationSaaS.Api"
RUN dotnet build "LifeBalance.OrganizationSaaS.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LifeBalance.OrganizationSaaS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LifeBalance.OrganizationSaaS.Api.dll"]
