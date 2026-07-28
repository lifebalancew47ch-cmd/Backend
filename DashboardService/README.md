# LifeBalance · Dashboard Service 🚀

> **Microservicio de Agregación del Dashboard** — parte del ecosistema *LifeBalance*.
>
> Construido sobre **.NET 10**, **Clean Architecture**, **DDD** y **CQRS**.

---

## Tabla de Contenidos

- [Arquitectura](#arquitectura)
- [Proyectos de la Solución](#proyectos-de-la-solución)
- [Prerrequisitos](#prerrequisitos)
- [Configuración Local](#configuración-local)
- [Ejecutar con Docker](#ejecutar-con-docker)
- [Variables de Entorno](#variables-de-entorno)
- [Documentación API](#documentación-api)
- [Health Checks](#health-checks)
- [Testing](#testing)
- [Contribuir](#contribuir)

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│                LifeBalance.Dashboard.API                │
│          (Controllers · Middleware · OpenAPI)           │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────┐
│             LifeBalance.Dashboard.Application           │
│       (CQRS · MediatR · FluentValidation · AutoMapper)  │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────┐
│               LifeBalance.Dashboard.Domain              │
│         (Entities · Aggregates · Domain Events)         │
└─────────────────────────────────────────────────────────┘
                           ▲
┌──────────────────────────┴──────────────────────────────┐
│            LifeBalance.Dashboard.Infrastructure         │
│         (MongoDB · HttpClients · Caching · Polly)       │
└─────────────────────────────────────────────────────────┘
```

**Contratos compartidos** → `LifeBalance.Dashboard.Contracts`
**Utilidades transversales** → `LifeBalance.Dashboard.Shared`

---

## Proyectos de la Solución

| Proyecto | Responsabilidad |
|---|---|
| `Dashboard.API` | Entry point HTTP — controllers, middlewares, DI, OpenAPI |
| `Dashboard.Application` | Casos de uso — commands, queries, handlers, validators |
| `Dashboard.Domain` | Núcleo del dominio — entidades, agregados, eventos |
| `Dashboard.Infrastructure` | Implementaciones técnicas — MongoDB, HTTP clients, caché |
| `Dashboard.Contracts` | DTOs de request/response compartidos entre servicios |
| `Dashboard.Shared` | Helpers, extensiones y tipos cross-cutting |
| `Dashboard.UnitTests` | Pruebas unitarias de dominio y aplicación |
| `Dashboard.IntegrationTests` | Pruebas de integración con WebApplicationFactory |

---

## Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.x+
- [MongoDB](https://www.mongodb.com/try/download/community) 7+ (o usar el compose)
- Visual Studio 2022 17.x+ / Rider 2024+

---

## Configuración Local

```bash
# 1. Clonar el repositorio
git clone https://github.com/LifeBalance/dashboard-service.git
cd dashboard-service

# 2. Restaurar dependencias
dotnet restore

# 3. Compilar la solución
dotnet build

# 4. Ejecutar la API
dotnet run --project src/LifeBalance.Dashboard.API
```

---

## Ejecutar con Docker

```bash
# Levantar todos los servicios (API + MongoDB)
docker-compose up -d

# Ver logs
docker-compose logs -f dashboard-api

# Detener
docker-compose down
```

---

## Despliegue en Render (Render.com)

El microservicio está **100% optimizado para Render** mediante el archivo `render.yaml` e imagen `Dockerfile`.

### Pasos para Desplegar en Render:

1. **Vincular el Repositorio en Render**:
   - Ingresa a [Render Dashboard](https://dashboard.render.com/).
   - Selecciona **New +** → **Blueprint**.
   - Conecta el repositorio de GitHub de `DashboardService`.
   - Render detectará automáticamente el archivo `render.yaml`.

2. **Variables de Entorno Secretas en Render**:
   Configure las siguientes variables secretas en la consola de Render:
   - `MongoDb__ConnectionString`: Connection String de MongoDB Atlas (ej. `mongodb+srv://<user>:<pass>@cluster0.mongodb.net/LifeBalanceDashboard`).
   - `Jwt__SecretKey`: Clave secreta para validación de tokens JWT.

3. **Verificación de Health Check**:
   - Render monitoreará el servicio mediante la ruta de Health Check configurada:
     `GET /api/v1/dashboard/health`

---

## Variables de Entorno

| Variable | Descripción | Ejemplo |
|---|---|---|
| `ConnectionStrings__MongoDB` | Connection string de MongoDB | `mongodb://localhost:27017` |
| `Jwt__Issuer` | Emisor del token JWT | `https://auth.lifebalance.io` |
| `Jwt__Audience` | Audiencia del token JWT | `dashboard-service` |
| `Jwt__SecretKey` | Clave secreta del JWT | `<secret>` |
| `Serilog__MinimumLevel__Default` | Nivel mínimo de log | `Information` |
| `OpenTelemetry__Endpoint` | Endpoint OTLP | `http://localhost:4317` |

---

## Documentación API

Una vez ejecutado el proyecto, acceder a:

- **Swagger UI**: `https://localhost:5001/swagger`
- **OpenAPI JSON**: `https://localhost:5001/swagger/v1/swagger.json`

---

## Health Checks

| Endpoint | Descripción |
|---|---|
| `GET /health` | Estado general del servicio |
| `GET /health/live` | Liveness probe (Kubernetes) |
| `GET /health/ready` | Readiness probe (Kubernetes) |
| `GET /health/ui` | UI de Health Checks |

---

## Testing

```bash
# Ejecutar tests unitarios
dotnet test tests/LifeBalance.Dashboard.UnitTests

# Ejecutar tests de integración
dotnet test tests/LifeBalance.Dashboard.IntegrationTests

# Con cobertura de código
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

---

## Contribuir

1. Crear una rama desde `develop`: `git checkout -b feature/nombre-feature`
2. Realizar los cambios siguiendo las convenciones del proyecto
3. Ejecutar los tests: `dotnet test`
4. Crear un Pull Request hacia `develop`

---

## Licencia

Propietario — © LifeBalance. Todos los derechos reservados.
