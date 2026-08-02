# AGENTS.md — LifeBalance Backend

Monorepo con **4 microservicios independientes en .NET 9** (Clean Architecture + CQRS), MongoDB, JWT compartido y despliegue en Render (plan free). **No existe un `.sln` raíz**: cada microservicio es una solución separada, cada uno con su propio Dockerfile, appsettings, docker-compose y tests.

## Estructura del repo

```
Auth_Profile/            → Servicio Auth & Profile (lifebalance-auth-service)
DashboardService/        → Servicio Dashboard (lifebalance-dashboard-service)
NotificationsAndAlerts/  → Servicio Notifications & Alerts (lifebalance-notifications-api)
OrganizationAndSaaS/     → Servicio Organization & SaaS (lifebalance-organization-saas)
tests/ContractTests/     → Tests de contrato entre servicios (raíz, sin .sln)
.github/                 → CI, CodeQL, Dependabot
render.yaml              → Defines los 4 servicios de Render
.gitignore               → bin/obj ignorados (no subir nunca)
```

- Carpetas legado que NO se tocan: `DashboardService/DashboardService/`, `OrganizationAndSaaS/Organization&SaaS/` y su solución `Organization&SaaS.sln` (referencian proyectos que sí existen; CodeQL solo compila las 4 soluciones oficiales en modo manual). `NotificationsAndAlerts/Notifications&Alerts.sln` fue **eliminada** porque referenciaba un proyecto inexistente y rompía el autobuild de CodeQL.
- Soluciones oficiales: `Auth_Profile/Auth_Profile.sln`, `DashboardService/LifeBalance.DashboardService.sln`, `NotificationsAndAlerts/LifeBalance.Notifications.sln`, `OrganizationAndSaaS/LifeBalance.OrganizationSaaS.sln`.

## Reglas globales (NO romper — son remediaciones de seguridad aplicadas)

1. **userId SIEMPRE viene del claim `ClaimTypes.NameIdentifier` del token JWT**, nunca de query/body/route (anti-IDOR). Si falta el claim → 401.
2. **Rol**: claim `ClaimTypes.Role` = `NormalizedName` del rol (MAYÚSCULAS, ej. `USER`, `ADMIN`). Auth hace fallback a `USER` en login/refresh si la cuenta no tiene `RoleIds` (fix del 403 de Dashboard).
3. **JWT**: `Issuer`/`Audience` = `"LifeBalance"` en los 4 servicios, algoritmo HS256, `ClockSkew` 1 minuto, mismo secreto compartido entre servicios.
4. **Fail-fast JWT**: Organization y Notifications (en Production) lanzan `InvalidOperationException` al arrancar si el secreto está vacío, es placeholder o <32 bytes UTF-8. Auth NO tiene este check (hardening pendiente).
5. **Fail-closed**: NO fabricar datos de ejemplo. Si un cliente HTTP upstream falla o devuelve null → `UpstreamServiceUnavailableException` → 503. Dashboard valida membresía familia/empresa contra Organization: no miembro → 403.
6. **Servicios de salida con HTTPS obligatorio** (Dashboard): fuera de Development, cualquier `ServiceUrls` con `http://` aborta el arranque (`InvalidOperationException`).
7. **CORS solo allowlist**, **Swagger solo en Development**, **rate limiter por IP** (`RemoteIpAddress`), **paginación clamp 1–100**, **mensajes de error genéricos** al cliente (detalle solo en logs).
8. **Nunca commitear secretos reales** (el secreto JWT de Organization se filtró una vez en el historial de git; usar placeholders como `CHANGE_THIS_TO_A_32_CHARACTER_SECRET_KEY_IN_PRODUCTION!!`). Los `appsettings.Development.json` pueden tener secretos dev propios.
9. **Contrato de respuesta común**: `Response<T> { bool Success; string Message; T Data; }`.
10. **Multi-tenant (Organization)**: el tenant se obtiene del claim `tenant_id` (prioridad) o header `X-Tenant-Id`; filtro de tenant incondicional en repositorios vía `IGlobalTenantEntity` (exime a `SaaSPlan`).

## Comandos

```powershell
# Por servicio (working directory del servicio):
dotnet build <solucion.sln> --configuration Release
dotnet test tests/<Proyecto>.UnitTests/<Proyecto>.UnitTests.csproj --no-build --configuration Release
# Contrato (raíz):
dotnet test tests/ContractTests/ContractTests.csproj --configuration Release
```

Suites verificadas verdes (total ~824): Auth 164, Dashboard 163, Organization 244, Notifications 249, Contract 4.

---

## 1. Auth_Profile (Auth & Profile)

- **Solución**: `Auth_Profile/Auth_Profile.sln`. **Proyectos**: `src/Auth.Api` (web), `src/Auth.Application` (MediatR handlers), `src/Auth.Domain`, `src/Auth.Infrastructure` (repos MongoDB), `src/Auth.Shared`.
- **BD**: MongoDB `LifeBalance_Auth` — colecciones: `users`, `user_preferences`, `roles`, `permissions`, `refresh_tokens`, `password_reset_tokens`, `email_confirmation_tokens`, `login_history`, `audit_logs` (índices en `Persistence/MongoDbInitializer.cs`).
- **JWT** (`appsettings.json` → `Jwt`): `SecretKey` placeholder, issuer/audience `LifeBalance`, access 30 min, refresh 7 días. Seguridad: 5 intentos fallidos → lockout 15 min.
- **Endpoints** (`api/v{version:apiVersion}/[controller]`, versión 1):
  - `POST api/v1/auth/register`, `login`, `refresh-token`, `logout`, `revoke-token`, `confirm-email`, `send-confirmation`, `forgot-password`, `reset-password`
  - `api/v1/profile`: `GET me`, `PUT me`, `GET preferences`, `PUT preferences`, `PUT change-password`
  - `api/v1/roles`: CRUD; `api/v1/permissions`: CRUD
  - `api/v1/audit`: `GET login-history`, `GET security-events`
- **Archivos clave**: `Auth.Application/Handlers/Auth/{LoginHandler,RefreshTokenHandler,RegisterHandler}.cs` (fallback rol `USER`), `Auth.Infrastructure/Services/JwtService.cs`, `Auth.Infrastructure/Repositories/*.cs`.
- **Hardening pendiente** (NO aplicado aún): placeholder JWT permitido, refresh tokens en texto plano (`RefreshToken.cs`), IDOR en revoke-all, lockout evadible por refresh, sin 2FA, Swagger/CORS `AllowAnyOrigin`.
- **Render**: `lifebalance-auth-service`, health `/health`.

## 2. DashboardService (Dashboard)

- **Solución**: `DashboardService/LifeBalance.DashboardService.sln`. **Proyectos**: `src/LifeBalance.Dashboard.API`, `src/LifeBalance.Dashboard.Application` (MediatR handlers), `src/LifeBalance.Dashboard.Contracts`, `src/LifeBalance.Dashboard.Domain`, `src/LifeBalance.Dashboard.Infrastructure` (HttpClients + Mongo), `src/LifeBalance.Dashboard.Shared`.
- **BD**: MongoDB `lifebalance_dashboard` — colecciones: `DashboardCache`, `AggregationLogs`. Logging: Serilog consola + archivo `logs/dashboard-.log`.
- **Clientes upstream** (config `ServiceUrls`): Auth, Organization, Notifications, MedicalData, SedentaryEngine, Gamification, ML Prediction, Reporting — **deben ser HTTPS** en producción (ver regla 6). Los 5 servicios "legado" (medical/sedentary/gamification/ml/reporting) NO están desplegados: sus endpoints devuelven 503 fail-closed.
- **CORS**: solo `https://lifebalance-adv3.onrender.com` (frontend).
- **Endpoints** (`api/v1/dashboard`, `api/v1/dashboard/individual`, `/family`, `/company`), todos GET, todos requieren JWT:
  - General: `summary`, `kpis`, `indicators`, `system`, `version`, `health` (este último lanza 503 fail-closed: sin fuente de health indicadores — no fabrica "Simulated-OK")
  - Individual: `summary`, `kpis`, `activity`, `biometrics`, `goals`, `heatmap`, `notifications`, `progress`, `recommendations`, `rewards`, `statistics`
  - Family: `GET /api/v1/dashboard/family`, `members`, `goals`, `challenges`, `ranking`, `rewards`, `heatmap`, `statistics` — requieren `familyId` válido y membresía (403 si no)
  - Company: `GET /api/v1/dashboard/company`, `kpis`, `licenses`, `organization`, `departments`, `adherence`, `ranking`, `trends`, `statistics`, `heatmap` — requieren `companyId`/`organizationId` reales y membresía (403/503)
- **Archivos clave**: `LifeBalance.Dashboard.Infrastructure/DependencyInjection.cs` (RegisterTypedClient + check HTTPS, línea ~102), `API/Middlewares/GlobalExceptionMiddleware.cs`, `Application/Features/**/*Queries.cs` (UpstreamServiceUnavailableException → 503), `Infrastructure/HttpClients/*.cs` (devuelven null ante fallo).
- **Render**: `lifebalance-dashboard-service`, health `/health/live` (ASP.NET Health Checks; el `/api/v1/dashboard/health` ya NO se usa como health de Render porque es fail-closed).

## 3. NotificationsAndAlerts (Notifications & Alerts)

- **Solución**: `NotificationsAndAlerts/LifeBalance.Notifications.sln`. **Proyectos**: `src/LifeBalance.Notifications.Presentation` (web), `src/LifeBalance.Notifications.Application` (DTOs/interfaces, SIN MediatR), `src/LifeBalance.Notifications.Domain`, `src/LifeBalance.Notifications.Infrastructure`, `src/LifeBalance.Notifications.Shared`.
- **IMPORTANTE**: NO usa MediatR. Patrón: Controller → interfaz (`Application/Interfaces`) → implementación (`Infrastructure/Services`) → MongoDB.
- **BD**: MongoDB `LifeBalanceNotificationsDb` — colecciones: `notifications`, `notification_preferences`, `notification_templates`, `scheduled_notifications`, `delivery_logs`, `alerts`, `metrics_records`, `device_registrations` (todas en `Infrastructure/Data/MongoDbContext.cs`).
- **JWT endurecido**: `ValidAlgorithms = {HmacSha256}`, ClockSkew 1 min, secreto placeholder/empty/short → crash en Production.
- **Roles**: los endpoints de `Templates`, `Metrics`, `History` (global y `organization/{organizationId}`), `Push` (`broadcast`/`company`/`family`/`department`) y `Emails` requieren rol **`ADMIN`**. Los endpoints propios del usuario usan `userId` del claim (nunca query). Recursos ajenos → 403.
- **Validaciones**: bulk ≤500 emails, validación de emails, rate limit 429.
- **Endpoints** (`api/v1/...`): `notifications` (CRUD + `schedule`, `bulk`, `read-all`, patches `read`/`archive`/`favorite`/`cancel`, `GET user`), `alerts` (CRUD + `PATCH {id}/read`/`dismiss`), `devices` (`POST register`, `DELETE unregister`), `emails` (`send`, `bulk`, `template`), `push` (`send`, `broadcast`, `company`, `family`, `department`, `wear`), `history` (`GET user`, `GET organization/{organizationId}`), `metrics` (global, `channels`, `delivery`, `errors`), `preferences` (GET/PUT + patches `email`/`push`/`wear`), `templates` (CRUD).
- **Archivos clave**: `Presentation/Program.cs`, `Presentation/Controllers/*.cs`, `Presentation/Middlewares/ExceptionHandlingMiddleware.cs` (mensajes genéricos), `Application/DTOs/*.cs` (DataAnnotations).
- **Render**: `lifebalance-notifications-api` (sin healthCheckPath definido).

## 4. OrganizationAndSaaS (Organization & SaaS)

- **Solución**: `OrganizationAndSaaS/LifeBalance.OrganizationSaaS.sln`. **Proyectos**: `src/Api/LifeBalance.OrganizationSaaS.Api`, `src/Core/LifeBalance.OrganizationSaaS.Application`, `src/Core/LifeBalance.OrganizationSaaS.Domain`, `src/Infrastructure/LifeBalance.OrganizationSaaS.Infrastructure`.
- **BD**: MongoDB `LifeBalance_OrganizationSaaS` — colecciones: `organizations`, `families`, `departments`, `teams`, `licenses`, `invitations` (índices en `Infrastructure/Persistence/MongoDbContext.cs`).
- **JWT** (config `JwtSettings`): **fail-fast**: `Secret` vacío/placeholder/<32 bytes → crash al arrancar**. `appsettings.json` tiene `Secret: ""` (Render debe inyectar `JwtSettings__Secret`); `appsettings.Development.json` tiene un secreto dev propio de 36 chars.
- **Autorización**: `FallbackPolicy` con `RequireAuthenticatedUser()` — todo exige JWT salvo `GET /health` y `POST api/v1/invitations/{token}/accept`|`reject`.
- **Multi-tenant**: ver regla 10. Handlers reciben `OrganizationId` desde `TenantContext` (claim `tenant_id` prioridad, luego header `X-Tenant-Id`).
- **Licencias**: asignación validada contra `plan.Limits.MaxLicenses` → `LimitExceededException` (409); plan inexistente → `ResourceNotFoundException` (404). Consultas de agregación (count) en repositorio Mongo.
- **Excepciones del dominio**: `ResourceNotFoundException`, `ValidationException` (400), `ConflictException` (409), `UnauthorizedOperationException` (403), `LimitExceededException` (409) — mapeadas en `Api/Middlewares/GlobalExceptionMiddleware.cs`.
- **Endpoints** (`api/v1/...`): `organizations` (CRUD + `PATCH {id}/activate`|`suspend`|`restore`, `GET {id}/statistics`), `families` (CRUD + `PATCH {id}/administrator`, `POST {id}/members`, `DELETE {id}/members/{userId}`), `departments` y `teams` (CRUD + `POST {id}/members`, `DELETE {id}/members/{userId}`), `licenses` (CRUD + `POST {id}/assign`, `POST {id}/cancel`, `POST {id}/renew`, `PATCH {id}/change-plan`, `PATCH {id}/renew`), `subscriptions`, `invitations` (CRUD + `POST {id}/resend`, `POST {token}/accept`, `POST {token}/reject`).
- **Paginación**: `pageIndex`/`pageSize` clamp 1–100; búsquedas con `Regex.Escape`.
- **Archivos clave**: `Api/Program.cs` (fallback policy, CORS, rate limit, fail-fast JWT), `Infrastructure/Services/TenantServices.cs`, `Infrastructure/Persistence/MongoRepository.cs` (filtro de tenant), `Core/LifeBalance.OrganizationSaaS.Application/Features/LicensesAndSubscriptions/LicenseAndSubscriptionFeatures.cs`.
- **Render**: `lifebalance-organization-saas`, health `/health`. **Crash-looppeará hasta que se configure `JwtSettings__Secret` en Render (intencional).**

---

## Infraestructura / CI-CD / Deploy

- **`.github/workflows/ci.yml`**: jobs independientes por servicio (restore → build Release → unit tests con trx + XPlat Code Coverage → `docker build` dry-run) + job `contract-tests` + gate `ci-success` que falla si algún job no es success. Dashboard parchea `global.json` con SDK `9.0.100` + `rollForward: latestMajor`. Triggers: push a `main`, `develop`, `feature/**`, `fix/**` y PRs a `main`/`develop`.
- **`.github/workflows/codeql.yml`**: csharp autobuild, push/PR a `main`/`develop` + semanal (lun 02:30).
- **`.github/dependabot.yml`**: nuget semanal para los 4 servicios + github-actions.
- **`render.yaml`**: 4 servicios plan free — `lifebalance-auth-service`, `lifebalance-dashboard-service`, `lifebalance-notifications-api`, `lifebalance-organization-saas`. Env vars clave: `Jwt__SecretKey` (Auth/Dashboard), `JwtSettings__Secret` (Organization), `JwtSettings__Issuer/Audience=LifeBalance`, `ServiceUrls__*` (Dashboard, todas HTTPS), `MongoConnectionString`/`DatabaseName` por servicio. **Secreto JWT compartido entre los 4 servicios — rotar si se filtra y setear idéntico en Render.**
- **Dockerfiles**: los 4 corren como usuario no-root (`appuser`/`appgroup`, chown de `/app`). Dashboard ya era non-root.
- **docker-compose** (por servicio): Mongo en `127.0.0.1` (loopback), sin credenciales root hardcodeadas, mongo-express con `ME_CONFIG_BASICAUTH=true`; `OrganizationAndSaaS/docker/docker-compose.yml` usa `JWT_SECRET` del `.env.example` (placeholder `CHANGE_ME_GENERATE_A_32_PLUS_CHAR_RANDOM_SECRET`); Notifications expone `5000:10000`.
- **`tests/ContractTests/`**: 4 tests de contrato (Auth↔Dashboard + degradación elegante) en la raíz, sin solución; se ejecutan desde el job `contract-tests`.
- **Secreto JWT de Organization comprometido en historial git** (`SuperSecretKeyForLifeBalanceSaaSMicroservice2026!`): no volver a usarlo; rotación obligatoria antes de producción real.
