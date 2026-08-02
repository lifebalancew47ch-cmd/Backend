# LifeBalance · Dashboard Service 🚀

> **Microservicio de Agregación del Dashboard** — parte del ecosistema *LifeBalance*.
>
> Construido sobre **.NET 9.0**, **Clean Architecture**, **DDD** y **CQRS** (MediatR).

> Más detalle en el [AGENTS.md](../AGENTS.md) del repositorio.

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
│        (MongoDB · HttpClients · Serilog · Polly)        │
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
| `Dashboard.UnitTests` | Pruebas unitarias (~163 verdes) |
| `Dashboard.IntegrationTests` | Pruebas de integración |

> La carpeta `DashboardService/DashboardService/` contiene un proyecto **legado** que no se mantiene.

---

## Prerrequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.x+
- [MongoDB](https://www.mongodb.com/try/download/community) 7+ (o usar el compose)
- Visual Studio 2022 17.x+ / Rider 2024+

---

## Configuración Local

```bash
# 1. Clonar el repositorio monorepo
git clone https://github.com/lifebalancew47ch-cmd/Backend.git
cd Backend/DashboardService

# 2. Restaurar dependencias
dotnet restore LifeBalance.DashboardService.sln

# 3. Compilar la solución
dotnet build LifeBalance.DashboardService.sln

# 4. Ejecutar la API (Swagger en https://localhost:5001/swagger)
dotnet run --project src/LifeBalance.Dashboard.API
```

---

## Ejecutar con Docker

```bash
# Levantar la API (puertos 5000/5001) + MongoDB (loopback 127.0.0.1:27017)
docker-compose up -d --build

# Mongo Express (UI, básicamente autenticada, loopback) — perfil dev-tools
docker-compose --profile dev-tools up -d mongo-express

# Ver logs / detener
docker-compose logs -f dashboard-api
docker-compose down
```

---

## Comportamiento Fail-Closed

- **Sin datos fabricados:** si un cliente HTTP upstream falla o devuelve `null`, el handler lanza `UpstreamServiceUnavailableException` → **HTTP 503**.
- **Validación de membresía:** los dashboards de familia/empresa validan `familyId`/`companyId` contra Organization (`GET /api/v1/families/{id}`, `GET /api/v1/departments?organizationId=`); sin membresía → **HTTP 403**.
- **HTTPS obligatorio:** fuera de Development, cualquier `ServiceUrls__*` con `http://` aborta el arranque (fail-fast).
- Los servicios "legado" (medical, sedentary, gamification, ml-prediction, reporting) **no están desplegados**: sus endpoints responden 503.

---

## Despliegue en Render (Render.com)

El microservicio está configurado en el `render.yaml` de la raíz (`lifebalance-dashboard-service`, plan free).

1. Conecta el repositorio en Render con la opción **Blueprint** — Render detecta `render.yaml`.
2. Variables secretas a configurar:
   - `MongoDb__ConnectionString` / `ConnectionStrings__MongoDB`: connection string de MongoDB Atlas.
   - `Jwt__SecretKey`: **el mismo secreto compartido que los otros 3 servicios** (Issuer/Audience `LifeBalance`).
   - `ServiceUrls__*`: ya vienen definidas en `render.yaml` (todas `https://`).
3. Health check de Render: `GET /health/live` (ASP.NET Health Checks; sin dependencia de upstreams). Puerto del contenedor: `10000`.

---

## Variables de Entorno

| Variable | Descripción | Ejemplo |
|---|---|---|
| `ConnectionStrings__MongoDB` | Connection string de MongoDB | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Nombre de la base de datos | `lifebalance_dashboard` |
| `Jwt__Issuer` | Emisor del token JWT | `LifeBalance` |
| `Jwt__Audience` | Audiencia del token JWT | `LifeBalance` |
| `Jwt__SecretKey` | Clave secreta del JWT (compartida) | `<secret>` |
| `ServiceUrls__AuthServiceUrl` | URL del servicio Auth (HTTPS en producción) | `https://...onrender.com` |
| `ServiceUrls__OrganizationServiceUrl` | URL de Organization & SaaS | `https://...onrender.com` |
| `ServiceUrls__NotificationServiceUrl` | URL de Notifications | `https://...onrender.com` |
| `CORS__AllowedOrigins` | Orígenes permitidos | `https://lifebalance-adv3.onrender.com` |
| `Serilog__MinimumLevel__Default` | Nivel mínimo de log (Serilog a consola + `logs/dashboard-.log`) | `Information` |
| `OpenTelemetry__Endpoint` | Endpoint OTLP | `http://localhost:4317` |

Colecciones MongoDB: `DashboardCache`, `AggregationLogs`.

---

## Documentación API

Una vez ejecutado el proyecto:

- **Swagger UI**: `https://localhost:5001/swagger`
- **OpenAPI JSON**: `https://localhost:5001/swagger/v1/swagger.json`

### Endpoints principales (todos GET, requieren JWT)
- `GET /api/v1/dashboard` — resumen general, `kpis`, `indicators`, `system`, `version`
- `GET /api/v1/dashboard/individual` — `summary`, `kpis`, `activity`, `biometrics`, `goals`, `heatmap`, `notifications`, `progress`, `recommendations`, `rewards`, `statistics`
- `GET /api/v1/dashboard/family` — `members`, `goals`, `challenges`, `ranking`, `rewards`, `heatmap`, `statistics` (requiere `familyId` + membresía)
- `GET /api/v1/dashboard/company` — `kpis`, `licenses`, `organization`, `departments`, `adherence`, `ranking`, `trends`, `statistics`, `heatmap` (requiere `companyId`/`organizationId` + membresía)

---

## Health Checks

| Endpoint | Descripción |
|---|---|
| `GET /health/live` | Liveness probe — health check de Render |
| `GET /health/ready` | Readiness probe |
| `GET /health` | Estado general |

---

## Testing

```bash
# Ejecutar tests unitarios (~163 verdes)
dotnet test tests/LifeBalance.Dashboard.UnitTests/LifeBalance.Dashboard.UnitTests.csproj

# Ejecutar tests de integración
dotnet test tests/LifeBalance.Dashboard.IntegrationTests/LifeBalance.Dashboard.IntegrationTests.csproj

# Con cobertura de código
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

---

## Contribuir

1. Crear una rama desde `develop`: `git checkout -b feature/nombre-feature`
2. Realizar los cambios siguiendo las convenciones del proyecto (ver [AGENTS.md](../AGENTS.md): anti-IDOR, fail-closed, HTTPS, no secretos reales)
3. Ejecutar los tests: `dotnet test`
4. Crear un Pull Request hacia `develop`

---

## Licencia

Propietario — © LifeBalance. Todos los derechos reservados.
