# LifeBalance - Organization & SaaS Microservice

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD%20%2B%20CQRS-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)

Enterprise core microservice for **LifeBalance**. Manages multi-tenant structures (Companies, Families, Departments, Teams), SaaS subscriptions, plan limits, licenses, memberships, invitations, and compliance audits.

> Details in [AGENTS.md](../AGENTS.md).

---

## 🏛 Architecture

Enforces **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS**:

- `LifeBalance.OrganizationSaaS.Domain`: Aggregate Roots, Entities (`Organization`, `Family`, `Department`, `Team`, `License`, `Subscription`, `Invitation`), Value Objects, Domain Exceptions (`ResourceNotFoundException`, `ValidationException`, `ConflictException`, `UnauthorizedOperationException`, `LimitExceededException`). Zero dependencies.
- `LifeBalance.OrganizationSaaS.Application`: CQRS Handlers (MediatR), DTOs, FluentValidation.
- `LifeBalance.OrganizationSaaS.Infrastructure`: MongoDB context and generic `MongoRepository<T>` with **unconditional tenant filter**, `IGlobalTenantEntity` (exempts global entities like `SaaSPlan`), and `TenantContextAccessor`.
- `LifeBalance.OrganizationSaaS.Api`: RESTful v1 Controllers, security headers, correlation ID, rate limiting, global exception handling (`Response<T>` envelope), OpenAPI.

---

## 🔐 Security & Multi-Tenancy

1. **Fail-fast JWT:** Startup crashes if `JwtSettings__Secret` is empty, placeholder, or <32 UTF-8 bytes (`InvalidOperationException`).
2. **FallbackPolicy Authentication:** All endpoints require valid JWT except `GET /health` and `POST api/v1/invitations/{token}/accept` | `reject`.
3. **Multi-Tenant Isolation:** Repositories filter by tenant unconditionally. Context resolved from `tenant_id` claim in JWT (priority) or `X-Tenant-Id` header.
4. **Rate Limiting:** IP rate limiting (429).
5. **Pagination & Input:** `pageIndex`/`pageSize` clamped 1–100, `Regex.Escape` applied on search queries.

---

## 💎 SaaS Plan Matrix

| Feature / Limit | Free | Personal | Family | Business | Enterprise |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Max Users** | 5 | 1 | 6 | 250 | 10,000+ |
| **Max Families** | 1 | 0 | 1 | 0 | 500 |
| **Max Companies** | 1 | 0 | 0 | 1 | 50 |
| **Max Departments** | 2 | 0 | 0 | 20 | 200 |
| **Max Teams** | 2 | 0 | 0 | 50 | 1,000 |

License assignment checked against `plan.Limits.MaxLicenses` → `LimitExceededException` (**409 Conflict**) when exceeded.

---

## 🚀 API Endpoints (`api/v1/`)

- `organizations`: CRUD, `PATCH activate`/`suspend`/`restore`, `statistics`
- `families`: CRUD, members management (`POST`/`DELETE`), `PATCH administrator`
- `departments` & `teams`: CRUD, members management (`POST`/`DELETE`)
- `licenses`: CRUD, assign, cancel, renew, change-plan
- `subscriptions`: CRUD, renew, change-plan
- `invitations`: CRUD, resend, accept (anonymous), reject (anonymous)

---

## 🐳 Deployment & Environment

- Required Env Var: `JwtSettings__Secret` (must match `Jwt__SecretKey` across services).
- Docker: API port `8080` (or `10000` on Render).

---

## 🧪 Testing

```bash
dotnet test tests/LifeBalance.OrganizationSaaS.UnitTests/LifeBalance.OrganizationSaaS.UnitTests.csproj --configuration Release
```
~244 green unit tests.
