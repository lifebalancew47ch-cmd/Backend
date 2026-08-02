# LifeBalance - Backend

Central backend repository for **LifeBalance**! 🚀

This monorepo contains the microservices architecture powering the **LifeBalance** platform. Built with **.NET 9** (C#), applying **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** principles.

> **For AI Agents and Developers**: Refer to [**AGENTS.md**](./AGENTS.md) for precise details on microservices, security rules, endpoints, databases, and commands.

---

## 🏛️ Global Architecture

The LifeBalance backend is structured as independent microservices. Each service uses its own database (Database-per-service) to ensure loose coupling.

### Tech Stack
* **Framework:** .NET 9.0 (ASP.NET Core)
* **Architecture:** Clean Architecture, DDD, CQRS (MediatR, except Notifications & Alerts)
* **Database:** MongoDB (NoSQL) — one DB per microservice
* **Cache:** Redis (local docker-compose in Organization & SaaS)
* **Auth & Security:** Shared JWT HS256 (Issuer/Audience `LifeBalance`), BCrypt, OWASP/DevSecOps remediations applied
* **Observability:** Serilog (Dashboard), Health Checks, Swagger (Development only)
* **Containerization & Deployment:** Docker, Docker Compose, **Render** (`render.yaml`)
* **CI/CD:** GitHub Actions (build + tests per service), CodeQL, Dependabot

---

## 📦 Microservices

### 1. 🔐 Auth & Profile (`/Auth_Profile`)
* **Responsibilities:** Login, registration, refresh token rotation, password management, access auditing, and user profiles.
* **Security:** JWT Bearer, RBAC/PBAC, brute force protection (lockout), audit logs, default `USER` role fallback.
* **Database:** `LifeBalance_Auth` (MongoDB).
* **Tests:** ~164 unit tests.

### 2. 📊 Dashboard Service (`/DashboardService`)
* **Responsibilities:** Data aggregation and orchestration to render individual, family, company, and general dashboards.
* **Behavior:** Fail-closed (no mock data); upstream failures return `503` (`UpstreamServiceUnavailableException`). Family/company membership checked against Organization (`403` on failure).
* **Security:** Enforced HTTPS for `ServiceUrls` in non-Dev; CORS allowlist (`https://lifebalance-adv3.onrender.com`).
* **Database:** `lifebalance_dashboard` (MongoDB).
* **Tests:** ~163 unit tests.

### 3. 🏢 Organization & SaaS (`/OrganizationAndSaaS`)
* **Responsibilities:** Multi-tenant management for Companies, Families, Departments, and Teams. Licenses, invitations, and SaaS plans.
* **Multi-Tenant Isolation:** Repositories unconditionally filter by tenant (`tenant_id` claim priority over header).
* **Security:** Fail-fast JWT (startup crash on empty/placeholder secret); `FallbackPolicy` requiring auth everywhere except `/health` and accepting/rejecting invitations.
* **Database:** `LifeBalance_OrganizationSaaS` (MongoDB).
* **Tests:** ~244 unit tests.

### 4. 🔔 Notifications & Alerts (`/NotificationsAndAlerts`)
* **Responsibilities:** Dispatching notifications (Push, Email, in-app), templates, preferences, history, metrics, and device registration.
* **Architecture:** Clean Architecture **without MediatR** (Controller → interface → implementation → MongoDB).
* **Security:** Hardened JWT (HS256, 1-min ClockSkew); admin endpoints restricted to `ADMIN` role; anti-IDOR via `userId` claim.
* **Database:** `LifeBalanceNotificationsDb` (MongoDB).
* **Tests:** ~249 unit tests.

---

## 🚀 Development & Deployment

### Local Environment (Docker Compose)
Run an individual microservice using its local `docker-compose.yml`:
```bash
docker-compose up --build -d
```

| Service | Local Dev Port | Docker Compose Port |
|---|---|---|
| Auth & Profile | `http://localhost:5200` / `https://localhost:7200` | `10000:10000` |
| Dashboard | `http://localhost:5000` / `https://localhost:5001` | `5000:8080`, `5001:8081` |
| Notifications | `http://localhost:5054` / `https://localhost:7269` | `5000:10000` |
| Organization & SaaS | `http://localhost:5072` / `https://localhost:7207` | `8080:8080` |

### Render Deployment
Root `render.yaml` orchestrates automatic deployment for all 4 services (Free tier Docker web services).

> **⚠️ IMPORTANT:** All services share the **same JWT secret**. Ensure matching values across all deployment variables on Render.

---

## 🛡️ Security Rules

1. **Fail-fast JWT:** Startup validation for production secret lengths and non-placeholder values.
2. **Mandatory HTTPS:** Dashboard validates HTTPS outbound URLs outside Development.
3. **Fail-closed:** Upstream HTTP errors return `503`.
4. **Anti-IDOR:** `userId` always retrieved from JWT `ClaimTypes.NameIdentifier`.
5. **Multi-tenant isolation:** Repository-level tenant filtering.
6. **Rate limiting (429), pagination clamping (1–100), CORS allowlist, Dev-only Swagger, generic client error messages.**
7. **Non-root Docker containers (`appuser`).**

---

## 🧪 Testing & CI/CD

* **~820 passing unit tests** across services + **4 contract tests** (`tests/ContractTests/`).
* **GitHub Actions (`ci.yml`):** Per-service build/test matrix + contract verification + `ci-success` gate.

```bash
# Run unit tests for a specific service
dotnet test tests/<Project>.UnitTests/<Project>.UnitTests.csproj --configuration Release

# Run contract tests
dotnet test tests/ContractTests/ContractTests.csproj --configuration Release
```

---

*Owner — © LifeBalance 2026. All rights reserved.*
