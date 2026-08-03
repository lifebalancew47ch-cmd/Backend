# LifeBalance · Reporting Service 🚀

> **Reporting Microservice** — part of the *LifeBalance* ecosystem.
> Generates historical health reports (PDF/Excel/CSV), statistics, trends, and platform system metrics.
> Built on **.NET 9.0**, **Clean Architecture**, **DDD**, and **CQRS** (MediatR).
> See [AGENTS.md](../AGENTS.md) for detailed rules and guidelines.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                LifeBalance.Reporting.API                 │
│          (Controllers · Middleware · OpenAPI)            │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────┐
│            LifeBalance.Reporting.Application             │
│       (CQRS · MediatR · FluentValidation · AutoMapper)   │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────┐
│               LifeBalance.Reporting.Domain              │
│         (Entities · Aggregates · Domain Services)        │
└─────────────────────────────────────────────────────────┘
                           ▲
┌──────────────────────────┴──────────────────────────────┐
│          LifeBalance.Reporting.Infrastructure            │
│      (MongoDB · HttpClients · ReportGenerators · Polly)  │
└─────────────────────────────────────────────────────────┘
```

---

## Solution Projects

| Project | Responsibility |
|---|---|
| `Reporting.API` | HTTP Entry Point — controllers, middlewares, DI, OpenAPI |
| `Reporting.Application` | Use Cases — queries, handlers, validators, mapping |
| `Reporting.Domain` | Domain Core — entities, enums, statistical analyzer |
| `Reporting.Infrastructure` | Technical Implementations — MongoDB, HTTP clients, PDF/Excel/CSV generators |
| `Reporting.Contracts` | Request/Response DTOs shared across services |
| `Reporting.Shared` | Helpers, `ApiResponse<T>`, `Result<T>` |
| `Reporting.UnitTests` | Unit tests (49 green) |
| `Reporting.IntegrationTests` | Integration tests |

---

## Fail-Closed Behavior

- **No mock data:** If an upstream HTTP client fails or returns `null`, handlers throw `UpstreamServiceUnavailableException` → **HTTP 503**.
- **Membership validation:** Family/Company reports validate membership against Organization; non-members → **HTTP 403**.
- **Anti-IDOR:** `userId` always comes from the JWT claim `ClaimTypes.NameIdentifier`, never from query/body/route.
- **HTTPS Enforced:** Outside Development, non-HTTPS `ServiceUrls__*` abort startup.
- **JWT Fail-fast:** In Production, an empty/placeholder/short `Jwt__SecretKey` aborts startup.

---

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `MongoDb__ConnectionString` | MongoDB Connection String | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Database Name | `lifebalance_reporting` |
| `Jwt__Issuer` / `Jwt__Audience` | JWT Issuer/Audience | `LifeBalance` |
| `Jwt__SecretKey` | Shared JWT Secret Key | `<secret>` |
| `ServiceUrls__AuthServiceUrl` | Auth Service URL (HTTPS in Prod) | `https://...onrender.com` |
| `ServiceUrls__MedicalDataServiceUrl` | Medical Data Service URL | `https://...onrender.com` |
| `ServiceUrls__SedentaryEngineServiceUrl` | Sedentary Engine URL | `https://...onrender.com` |
| `ServiceUrls__DashboardServiceUrl` | Dashboard Service URL | `https://...onrender.com` |
| `ServiceUrls__OrganizationServiceUrl` | Organization & SaaS URL | `https://...onrender.com` |
| `Cors__AllowedOrigins` | CORS Allowlist | `https://lifebalance-adv3.onrender.com` |

MongoDB Collections: `report_generation_logs`.

---

## API Endpoints (require JWT)

| Endpoint | Policy | Description |
|---|---|---|
| `GET /api/v1/reports/individual` | `ReportRead` | Individual historical report |
| `GET /api/v1/reports/family/{familyId}` | `ReportRead` | Family report (membership validated) |
| `GET /api/v1/reports/company/{companyId}` | `ReportRead` | Company report (membership validated) |
| `GET /api/v1/reports/statistics` | `ReportRead` | Descriptive statistics per metric |
| `GET /api/v1/reports/trends` | `ReportRead` | Trend analysis (regression + moving average) |
| `GET /api/v1/reports/dashboard-summary` | `ReportRead` | Aggregated dashboard summary |
| `GET /api/v1/reports/history` | `AuthenticatedUser` | Paginated generation log for current user |
| `GET /api/v1/reports/export` | `ReportExport` | Download report as `pdf`, `xlsx`, or `csv` |
| `GET /api/v1/reports/system-metrics` | `Admin` | Platform metrics consumed by Dashboard Service |

---

## Health Checks

| Endpoint | Description |
|---|---|
| `GET /health` | Overall status (Render health check) |
| `GET /health/live` | Liveness probe |
| `GET /health/ready` | Readiness probe (MongoDB) |

---

## Testing

```bash
dotnet test tests/LifeBalance.Reporting.UnitTests/LifeBalance.Reporting.UnitTests.csproj
```
