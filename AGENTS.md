# AGENTS.md — LifeBalance Backend

Monorepo with **5 independent microservices in .NET 9** (Clean Architecture + CQRS), MongoDB, shared JWT, deployed on Render (free tier). **No root `.sln` exists**: each microservice is a separate solution with its own Dockerfile, appsettings, docker-compose, and tests.

## Repository Structure

```
Auth_Profile/            → Auth & Profile Service (lifebalance-auth-service)
DashboardService/        → Dashboard Service (lifebalance-dashboard-service)
NotificationsAndAlerts/  → Notifications & Alerts Service (lifebalance-notifications-api)
OrganizationAndSaaS/     → Organization & SaaS Service (lifebalance-organization-saas)
AdministrationService/   → Administration & Configuration Service (lifebalance-administration-service)
tests/ContractTests/     → Inter-service contract tests (root, no .sln)
.github/                 → CI, CodeQL, Dependabot
render.yaml              → Defines 5 Render services
.gitignore               → bin/obj ignored (never commit)
```

- Legacy directories NOT touched: `DashboardService/DashboardService/`, `OrganizationAndSaaS/Organization&SaaS/` and solution `Organization&SaaS.sln` (reference existing projects; CodeQL manually builds only 5 official solutions). `NotificationsAndAlerts/Notifications&Alerts.sln` was **removed** (referenced nonexistent project breaking CodeQL autobuild).
- Official solutions: `Auth_Profile/Auth_Profile.sln`, `DashboardService/LifeBalance.DashboardService.sln`, `NotificationsAndAlerts/LifeBalance.Notifications.sln`, `OrganizationAndSaaS/LifeBalance.OrganizationSaaS.sln`, `AdministrationService/LifeBalance.Administration.sln`.

## Global Security Rules (DO NOT BREAK — Applied Security Remediation)

1. **userId MUST ALWAYS come from claim `ClaimTypes.NameIdentifier` in JWT token**, never from query/body/route (anti-IDOR). Missing claim → 401.
2. **Role**: JWT token uses standard short names (`sub`, `email`, `name`, `role`), mapped on validation to `ClaimTypes.NameIdentifier`/`Email`/`Name`/`Role`. Role value = `NormalizedName` (UPPERCASE, e.g., `USER`, `ADMIN`). Auth falls back to `USER` in login/refresh if account lacks `RoleIds` (fixes Dashboard 403). Auth handlers filter null/empty `NormalizedName`; repos discard non-ObjectId strings (prevents 500 from Mongo driver `FormatException`).
3. **JWT**: `Issuer`/`Audience` = `"LifeBalance"` across all 5 services, HS256 algorithm, `ClockSkew` 1 minute, identical shared secret.
4. **Fail-fast JWT**: Organization, Notifications and Administration (in Production) throw `InvalidOperationException` on startup if secret is empty, placeholder, or <32 UTF-8 bytes. Auth does not have this check yet.
5. **Fail-closed**: DO NOT mock/stub upstream data. If upstream HTTP client fails or returns null → `UpstreamServiceUnavailableException` → 503. Dashboard validates family/company membership against Organization: non-member → 403.
6. **HTTPS Required for Outbound Services** (Dashboard, Organization, Administration): Outside Development, any `ServiceUrls`/`Microservices` using `http://` aborts startup (`InvalidOperationException`).
7. **CORS allowlist only**, **Swagger in Development only**, **IP rate limiter** (`RemoteIpAddress`), **pagination clamped 1–100**, **generic error messages** to client (details in logs only).
8. **Never commit real secrets** (Organization JWT secret was previously leaked in git history; use placeholders like `CHANGE_THIS_TO_A_32_CHARACTER_SECRET_KEY_IN_PRODUCTION!!`). `appsettings.Development.json` can have local dev secrets.
9. **Common Response Contract**: `Response<T> { bool Success; string Message; T Data; }`.
10. **Multi-tenant (Organization)**: Tenant resolved from `tenant_id` claim (priority) or `X-Tenant-Id` header; unconditional tenant filter in repos via `IGlobalTenantEntity` (exempts `SaaSPlan`).

## Commands

```powershell
# Per service (working directory of service):
dotnet build <solution.sln> --configuration Release
dotnet test tests/<Project>.UnitTests/<Project>.UnitTests.csproj --no-build --configuration Release
# Contract tests (root):
dotnet test tests/ContractTests/ContractTests.csproj --configuration Release
```

Verified passing test suites (~824 total): Auth 164, Dashboard 163, Organization 244, Notifications 249, Contract 4.

---

## 1. Auth_Profile (Auth & Profile)

- **Solution**: `Auth_Profile/Auth_Profile.sln`. **Projects**: `src/Auth.Api` (web), `src/Auth.Application` (MediatR handlers), `src/Auth.Domain`, `src/Auth.Infrastructure` (MongoDB repos), `src/Auth.Shared`.
- **DB**: MongoDB `LifeBalance_Auth` — collections: `users`, `user_preferences`, `roles`, `permissions`, `refresh_tokens`, `password_reset_tokens`, `email_confirmation_tokens`, `login_history`, `audit_logs` (indexes in `Persistence/MongoDbInitializer.cs`).
- **JWT** (`appsettings.json` → `Jwt`): `SecretKey` placeholder, issuer/audience `LifeBalance`, access 30m, refresh 7d. Security: 5 failed attempts → 15m lockout.
- **Endpoints** (`api/v{version:apiVersion}/[controller]`, v1):
  - `POST api/v1/auth/register`, `login`, `refresh-token`, `logout`, `revoke-token`, `confirm-email`, `send-confirmation`, `forgot-password`, `reset-password`
  - `api/v1/profile`: `GET me`, `PUT me`, `GET preferences`, `PUT preferences`, `PUT change-password`
  - `api/v1/roles`: CRUD; `api/v1/permissions`: CRUD
  - `api/v1/audit`: `GET login-history`, `GET security-events`
- **Key files**: `Auth.Application/Handlers/Auth/{LoginHandler,RefreshTokenHandler,RegisterHandler}.cs` (`USER` role fallback), `Auth.Infrastructure/Services/JwtService.cs`, `Auth.Infrastructure/Repositories/*.cs`.
- **Tenant membership auto-provisioning**: on `register`, `login` and `refresh-token`, if the user has no membership (`tenants/me` returns 404 → no `tenant_id`), Auth calls `POST /api/v1/internal/memberships` on Organization (guarded by `Internal:ProvisioningKey` via header `X-Internal-Key`, same value in both services) which creates a dedicated org + assigned license; JWT then carries `tenant_id`/`organization_id`. Best-effort: on failure the token is issued without tenant and retried on next login/refresh. See `IOrganizationService.ProvisionMembershipAsync` / `OrganizationServiceClient.cs`.
- **Pending Hardening**: placeholder JWT allowed, plaintext refresh tokens (`RefreshToken.cs`), IDOR on revoke-all, lockout bypass via refresh, no 2FA, Swagger/CORS `AllowAnyOrigin`.
- **Render**: `lifebalance-auth-service`, health `/health`.

## 2. DashboardService (Dashboard)

- **Solution**: `DashboardService/LifeBalance.DashboardService.sln`. **Projects**: `src/LifeBalance.Dashboard.API`, `src/LifeBalance.Dashboard.Application` (MediatR handlers), `src/LifeBalance.Dashboard.Contracts`, `src/LifeBalance.Dashboard.Domain`, `src/LifeBalance.Dashboard.Infrastructure` (HttpClients + Mongo), `src/LifeBalance.Dashboard.Shared`.
- **DB**: MongoDB `lifebalance_dashboard` — collections: `DashboardCache`, `AggregationLogs`. Logging: Serilog console + file `logs/dashboard-.log`.
- **Upstream clients** (`ServiceUrls` config): Auth, Organization, Notifications, MedicalData, SedentaryEngine, Gamification, ML Prediction, Reporting — **MUST be HTTPS** in production (Rule 6). 5 legacy services (medical/sedentary/gamification/ml/reporting) are deployed on Render. In case of downtime, endpoints gracefully degrade (empty objects/lists) or simulate 200 OK.
- **CORS**: `https://lifebalance-adv3.onrender.com` only (frontend).
- **Endpoints** (`api/v1/dashboard`, `api/v1/dashboard/individual`, `/family`, `/company`), all GET, all require JWT:
  - General: `summary`, `kpis`, `indicators`, `system`, `version`, `health` (simulated 200 OK to prevent deployment blockers when upstreams are down)
  - Individual: `summary`, `kpis`, `activity`, `biometrics`, `goals`, `heatmap`, `notifications`, `progress`, `recommendations`, `rewards`, `statistics`
  - Family: `GET /api/v1/dashboard/family`, `members`, `goals`, `challenges`, `ranking`, `rewards`, `heatmap`, `statistics` — require valid `familyId` & membership (403 if not)
  - Company: `GET /api/v1/dashboard/company`, `kpis`, `licenses`, `organization`, `departments`, `adherence`, `ranking`, `trends`, `statistics`, `heatmap` — require real `companyId`/`organizationId` & membership (403/503)
- **Key files**: `LifeBalance.Dashboard.Infrastructure/DependencyInjection.cs` (RegisterTypedClient + HTTPS check), `API/Middlewares/GlobalExceptionMiddleware.cs`, `Application/Features/**/*Queries.cs` (`UpstreamServiceUnavailableException` → 503), `Infrastructure/HttpClients/*.cs` (return null on failure).
- **Render**: `lifebalance-dashboard-service`, health `/health/live` or `/api/v1/dashboard/health`.

## 3. NotificationsAndAlerts (Notifications & Alerts)

- **Solution**: `NotificationsAndAlerts/LifeBalance.Notifications.sln`. **Projects**: `src/LifeBalance.Notifications.Presentation` (web), `src/LifeBalance.Notifications.Application` (DTOs/interfaces, NO MediatR), `src/LifeBalance.Notifications.Domain`, `src/LifeBalance.Notifications.Infrastructure`, `src/LifeBalance.Notifications.Shared`.
- **IMPORTANT**: Does NOT use MediatR. Pattern: Controller → interface (`Application/Interfaces`) → implementation (`Infrastructure/Services`) → MongoDB.
- **DB**: MongoDB `LifeBalanceNotificationsDb` — collections: `notifications`, `notification_preferences`, `notification_templates`, `scheduled_notifications`, `delivery_logs`, `alerts`, `metrics_records`, `device_registrations` (all in `Infrastructure/Data/MongoDbContext.cs`).
- **Hardened JWT**: `ValidAlgorithms = {HmacSha256}`, ClockSkew 1 min, placeholder/empty/short secret → crash in Production.
- **Roles**: Endpoints for `Templates`, `Metrics`, `History` (global and `organization/{organizationId}`), `Push` (`broadcast`/`company`/`family`/`department`) and `Emails` require **`ADMIN`** role. User endpoints use claim `userId`. Resource belonging to others → 403.
- **Validations**: bulk ≤500 emails, email validation, rate limit 429.
- **Endpoints** (`api/v1/...`): `notifications` (CRUD + `schedule`, `bulk`, `read-all`, patches `read`/`archive`/`favorite`/`cancel`, `GET user`), `alerts` (CRUD + `PATCH {id}/read`/`dismiss`), `devices` (`POST register`, `DELETE unregister`), `emails` (`send`, `bulk`, `template`), `push` (`send`, `broadcast`, `company`, `family`, `department`, `wear`), `history` (`GET user`, `GET organization/{organizationId}`), `metrics` (global, `channels`, `delivery`, `errors`), `preferences` (GET/PUT + patches `email`/`push`/`wear`), `templates` (CRUD).
- **Key files**: `Presentation/Program.cs`, `Presentation/Controllers/*.cs`, `Presentation/Middlewares/ExceptionHandlingMiddleware.cs`, `Application/DTOs/*.cs`.
- **Render**: `lifebalance-notifications-api`.

## 4. OrganizationAndSaaS (Organization & SaaS)

- **Solution**: `OrganizationAndSaaS/LifeBalance.OrganizationSaaS.sln`. **Projects**: `src/Api/LifeBalance.OrganizationSaaS.Api`, `src/Core/LifeBalance.OrganizationSaaS.Application`, `src/Core/LifeBalance.OrganizationSaaS.Domain`, `src/Infrastructure/LifeBalance.OrganizationSaaS.Infrastructure`.
- **DB**: MongoDB `LifeBalance_OrganizationSaaS` — collections: `organizations`, `families`, `departments`, `teams`, `licenses`, `invitations` (indexes in `Infrastructure/Persistence/MongoDbContext.cs`).
- **JWT** (`JwtSettings` config): **fail-fast**: empty/placeholder/<32 byte `Secret` → startup crash. `appsettings.json` has `Secret: ""` (Render must inject `JwtSettings__Secret`); `appsettings.Development.json` has a 36-char dev secret.
- **Auth**: `FallbackPolicy` with `RequireAuthenticatedUser()` — everything requires JWT except `GET /health` and `POST api/v1/invitations/{token}/accept`|`reject`.
- **Multi-tenant**: Handlers receive `OrganizationId` from `TenantContext` (`tenant_id` claim priority, then `X-Tenant-Id` header).
- **Licenses**: Assignment checked against `plan.Limits.MaxLicenses` → `LimitExceededException` (409); missing plan → `ResourceNotFoundException` (404). Mongo aggregation counts.
- **Domain Exceptions**: `ResourceNotFoundException`, `ValidationException` (400), `ConflictException` (409), `UnauthorizedOperationException` (403), `LimitExceededException` (409) — mapped in `Api/Middlewares/GlobalExceptionMiddleware.cs`.
- **Endpoints** (`api/v1/...`): `organizations` (CRUD + `PATCH {id}/activate`|`suspend`|`restore`, `GET {id}/statistics`), `families` (CRUD + `PATCH {id}/administrator`, `POST {id}/members`, `DELETE {id}/members/{userId}`), `departments` & `teams` (CRUD + `POST {id}/members`, `DELETE {id}/members/{userId}`), `licenses` (CRUD + `POST {id}/assign`, `POST {id}/cancel`, `POST {id}/renew`, `PATCH {id}/change-plan`, `PATCH {id}/renew`), `subscriptions`, `invitations` (CRUD + `POST {id}/resend`, `POST {token}/accept`, `POST {token}/reject`), `POST api/v1/internal/memberships` (`[AllowAnonymous]`, guarded by `X-Internal-Key` = `Internal:ProvisioningKey`, only for Auth) — creates a dedicated org + assigned license for a user (auto-provision).
- **Pagination**: `pageIndex`/`pageSize` clamped 1–100; regex escape on searches.
- **Key files**: `Api/Program.cs`, `Infrastructure/Services/TenantServices.cs`, `Infrastructure/Persistence/MongoRepository.cs`, `Core/LifeBalance.OrganizationSaaS.Application/Features/LicensesAndSubscriptions/LicenseAndSubscriptionFeatures.cs`.
- **Render**: `lifebalance-organization-saas`, health `/health`. **Will crash-loop until `JwtSettings__Secret` is set in Render (intentional).**

## 5. AdministrationService (Administration & Configuration)

- **Solution**: `AdministrationService/LifeBalance.Administration.sln`. **Projects**: `src/Api/LifeBalance.Administration.Api`, `src/Core/LifeBalance.Administration.Application` (MediatR handlers), `src/Core/LifeBalance.Administration.Domain`, `src/Infrastructure/LifeBalance.Administration.Infrastructure`.
- **DB**: MongoDB `LifeBalance_Administration` — collections: `catalogs`, `system_parameters`, `feature_flags`, `service_status`, `system_logs`, `audit_logs`, `maintenance_modes`, `system_configuration`, `global_configuration`.
- **JWT** (`JwtSettings` config): **fail-fast**: empty/placeholder/<32 byte `Secret` → startup crash. `appsettings.json` has `Secret: ""` (Render must inject `JwtSettings__Secret`); `appsettings.Development.json` (gitignored, holds real Atlas URI + dev JWT secret) provides the local override.
- **Auth**: `FallbackPolicy` + `AdministratorOnlyPolicy` requiring roles `SUPERADMIN`/`SYSTEMADMINISTRATOR` — everything requires JWT except `GET /health`. Anti-IDOR: identity always from `ClaimTypes.NameIdentifier`.
- **Bson mapping**: parameterized-ctor entities (Catalog, SystemLog, ...) need explicit `MapCreator` registrations in `Infrastructure/Persistence/BsonClassMapRegistrations.cs` (the `ImmutableTypeClassMapConvention` otherwise creates a creator with no arguments → `Creator map ... has N arguments, but none are configured`).
- **Endpoints** (`api/v1/...`): `catalogs` (CRUD + `PATCH {id}/activate`|`deactivate`), `parameters` (CRUD + activate/deactivate), `feature-flags` (CRUD + `enable`|`disable`), `logs` (`POST`, `POST bulk`, `GET`, `GET errors|warnings|{id}`), `audit` (`GET`, `by-user/{userId}`, `by-service/{service}`, `{id}`), `settings` (`GET`/`PUT`/`POST reset`), `maintenance` (`GET`/`PUT status`), `services` (`GET status`, `GET {service}/status`), `statistics` (`GET`), `integrations` (`GET auth/roles`, `GET auth/permissions`, `GET organization` — fail-closed → 503 when upstream down).
- **Upstream clients** (`Microservices` config): Auth (`/api/v1/roles`, `/api/v1/permissions`), Organization (`/api/v1/organizations`, `/api/v1/licenses`), plus health probes for Dashboard/Notifications/legacy services — **MUST be HTTPS** outside Development (Rule 6). Bearer token of the caller is propagated via `BearerTokenPropagationHandler` (must stay registered in `DependencyInjection.cs`).
- **Key files**: `Api/Program.cs`, `Api/Middlewares/ApiMiddlewares.cs`, `Infrastructure/Persistence/{MongoDbContext,MongoRepository,BsonClassMapRegistrations}.cs`, `Infrastructure/ExternalServices/{BaseServiceClient,ExternalMicroserviceClients,BearerTokenPropagationHandler}.cs`, `Core/.../Features/Integrations/IntegrationFeatures.cs`.
- **Render**: `lifebalance-administration-service`, health `/health`.

---

## Infrastructure / CI-CD / Deploy

- **`.github/workflows/ci.yml`**: Independent service jobs (restore → Release build → unit tests + coverage → docker build dry-run) + `contract-tests` job + `ci-success` gate.
- **`.github/workflows/codeql.yml`**: C# autobuild on push/PR + weekly.
- **`.github/dependabot.yml`**: Weekly NuGet updates for 5 services + GitHub Actions.
- **`render.yaml`**: 5 free-tier services — `lifebalance-auth-service`, `lifebalance-dashboard-service`, `lifebalance-notifications-api`, `lifebalance-organization-saas`, `lifebalance-administration-service`. Shared JWT secret across services.
- **Dockerfiles**: All 5 run as non-root (`appuser`/`appgroup`).
- **docker-compose**: MongoDB on `127.0.0.1` (loopback), no hardcoded root credentials.
- **`tests/ContractTests/`**: 4 root contract tests (Auth↔Dashboard + graceful degradation).
- **Leaked JWT secret in history**: `SuperSecretKeyForLifeBalanceSaaSMicroservice2026!` (must rotate for real production).
