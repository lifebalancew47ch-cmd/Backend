FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/LifeBalance.Notifications.Presentation/LifeBalance.Notifications.Presentation.csproj", "src/LifeBalance.Notifications.Presentation/"]
COPY ["src/LifeBalance.Notifications.Application/LifeBalance.Notifications.Application.csproj", "src/LifeBalance.Notifications.Application/"]
COPY ["src/LifeBalance.Notifications.Domain/LifeBalance.Notifications.Domain.csproj", "src/LifeBalance.Notifications.Domain/"]
COPY ["src/LifeBalance.Notifications.Infrastructure/LifeBalance.Notifications.Infrastructure.csproj", "src/LifeBalance.Notifications.Infrastructure/"]
COPY ["src/LifeBalance.Notifications.Shared/LifeBalance.Notifications.Shared.csproj", "src/LifeBalance.Notifications.Shared/"]
RUN dotnet restore "src/LifeBalance.Notifications.Presentation/LifeBalance.Notifications.Presentation.csproj"
COPY . .
WORKDIR "/src/src/LifeBalance.Notifications.Presentation"
RUN dotnet build "LifeBalance.Notifications.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LifeBalance.Notifications.Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Security: run as non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

ENTRYPOINT ["dotnet", "LifeBalance.Notifications.Presentation.dll"]
