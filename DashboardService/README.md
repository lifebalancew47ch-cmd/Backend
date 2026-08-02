# LifeBalance · Dashboard Service 🚀

> **Dashboard Aggregation Microservice** — part of the *LifeBalance* ecosystem.
> Built on **.NET 9.0**, **Clean Architecture**, **DDD**, and **CQRS** (MediatR).
> See [AGENTS.md](../AGENTS.md) for detailed rules and guidelines.

---

## Architecture

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

---

## Solution Projects

| Project | Responsibility |
|---|---|
| `Dashboard.API` | HTTP Entry Point — controllers, middlewares, DI, OpenAPI |
| `Dashboard.Application` | Use Cases — commands, queries, handlers, validators |
| `Dashboard.Domain` | Domain Core — entities, aggregates, domain events |
| `Dashboard.Infrastructure` | Technical Implementations — MongoDB, HTTP clients, cache |
| `Dashboard.Contracts` | Request/Response DTOs shared across services |
| `Dashboard.Shared` | Helpers, extensions, cross-cutting types |
| `Dashboard.UnitTests` | Unit tests (~163 green) |
| `Dashboard.IntegrationTests` | Integration tests |

> `DashboardService/DashboardService/` contains a legacy project that is not maintained.

---

## Fail-Closed Behavior

- **No mock data:** If an upstream HTTP client fails or returns `null`, handler throws `UpstreamServiceUnavailableException` → **HTTP 503**.
- **Membership validation:** Family/Company dashboards validate membership against Organization; non-members → **HTTP 403**.
- **HTTPS Enforced:** Outside Development, non-HTTPS `ServiceUrls__*` abort startup.
- Legacy services (medical, sedentary, gamification, ml-prediction, reporting) are not deployed: endpoints return 503.

---

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `ConnectionStrings__MongoDB` | MongoDB Connection String | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Database Name | `lifebalance_dashboard` |
| `Jwt__Issuer` / `Jwt__Audience` | JWT Issuer/Audience | `LifeBalance` |
| `Jwt__SecretKey` | Shared JWT Secret Key | `<secret>` |
| `ServiceUrls__AuthServiceUrl` | Auth Service URL (HTTPS in Prod) | `https://...onrender.com` |
| `ServiceUrls__OrganizationServiceUrl` | Organization & SaaS URL | `https://...onrender.com` |
| `ServiceUrls__NotificationServiceUrl` | Notifications URL | `https://...onrender.com` |
| `CORS__AllowedOrigins` | CORS Allowlist | `https://lifebalance-adv3.onrender.com` |
| `Serilog__MinimumLevel__Default` | Serilog level | `Information` |

MongoDB Collections: `DashboardCache`, `AggregationLogs`.

---

## API Endpoints (All GET, require JWT)
- `/api/v1/dashboard` — General summary, KPIs, system indicators
- `/api/v1/dashboard/individual` — Individual metrics (summary, activity, biometrics, etc.)
- `/api/v1/dashboard/family` — Family dashboard (requires `familyId` & membership)
- `/api/v1/dashboard/company` — Company dashboard (requires `companyId`/`organizationId` & membership)

---

## Health Checks

| Endpoint | Description |
|---|---|
| `GET /health/live` | Liveness probe — Render health check |
| `GET /health/ready` | Readiness probe |
| `GET /health` | Overall status |
| `GET /api/v1/dashboard/health` | **(SIMULATED)** Simulated 200 OK health check for ecosystem deployment |

---

## Testing

```bash
dotnet test tests/LifeBalance.Dashboard.UnitTests/LifeBalance.Dashboard.UnitTests.csproj
```
