# Mapa de Endpoints API

Documento generado que describe **recibe** (inputs) y **retorna** (outputs) de cada endpoint HTTP de los dos proyectos analizados:

1. **`C:\Users\Rodrigo\source\repos\Backend`** — monorepo **LifeBalance** (.NET 9, Clean Architecture + CQRS, 6 microservicios).
2. **`C:\Users\Rodrigo\Downloads\backapi-main`** — monorepo de servicios complementarios (Gateway, MedicalData, MLPrediction, SedentaryEngine, Gamification, Ingestion).

---

# PARTE 1 — LifeBalance Backend

## Envases de respuesta usados

| Servicio | Envelope | Ejemplo |
|---|---|---|
| Auth_Profile | `ApiResponse<T>` + `StatusCode` | 200 / 400 / 401 |
| Dashboard | `ApiResponse<T>` (`Ok`→200, `Fail`→400) | |
| Reporting | `ApiResponse<T>` + `File()` para export | |
| Notifications | `Response<T> { Success, Message, Data }` | |
| Organization/SaaS | `Response<T>` vía `Ok(result)` / `CreatedAtAction` | |
| Administration | `Response<T>` vía `Ok(result)` / `CreatedAtAction` | |

**Convenciones de identidad (anti-IDOR):** `userId` casi siempre proviene del claim `ClaimTypes.NameIdentifier` del JWT (salvo rutas explícitas `{id}`). `tenant_id`/`X-Tenant-Id` resuelve el tenant. Roles: `NormalizedName` en mayúsculas (`ADMIN`, `SUPERADMIN`, `SYSTEMADMINISTRATOR`, `USER`).

---

## 1. Auth_Profile (Auth & Profile)

### AuthController — `api/v1/auth`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `register` | `[AllowAnonymous]` body `RegisterRequest` → `RegisterCommand` | `ApiResponse<RegisterResponse>` (200/400) |
| POST | `login` | `[AllowAnonymous]` body `LoginRequest` (IP desde remote addr) | `ApiResponse<LoginResponse>` (200/401) |
| POST | `logout` | body `LogoutRequest?`; `userId` del claim | `ApiResponse<bool>` |
| POST | `refresh-token` | `[AllowAnonymous]` body `RefreshTokenRequest` | `ApiResponse<RefreshTokenResponse>` (200/401) |
| POST | `revoke-token` | body `TokenRevocationRequest`; `userId` del claim | `ApiResponse<bool>` |
| POST | `forgot-password` | `[AllowAnonymous]` body `ForgotPasswordRequest` | `ApiResponse<bool>` |
| POST | `reset-password` | `[AllowAnonymous]` body `ResetPasswordRequest` | `ApiResponse<bool>` (200/400) |
| POST | `send-confirmation` | `[AllowAnonymous]` body `SendConfirmationRequest` | `ApiResponse<bool>` |
| POST | `confirm-email` | `[AllowAnonymous]` body `ConfirmEmailRequest` | `ApiResponse<bool>` (200/400) |

### ProfileController — `api/v1/profile`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `me` | `userId` del claim | `ApiResponse<UserProfileDto>` |
| PUT | `me` | body `UpdateProfileRequest`; `userId` del claim | `ApiResponse<UserProfileDto>` |
| GET | `preferences` | `userId` del claim | `ApiResponse<UserPreferenceDto>` |
| PUT | `preferences` | body `UpdatePreferenceRequest`; `userId` del claim | `ApiResponse<UserPreferenceDto>` |
| PUT | `change-password` | body `ChangePasswordRequest`; `userId` del claim | `ApiResponse<bool>` |

### RolesController — `api/v1/roles` (`[Authorize(Roles="ADMIN")]`)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | — | `ApiResponse<IEnumerable<RoleDto>>` |
| POST | `` | body `CreateRoleRequest` | `ApiResponse<RoleDto>` (201) |
| PUT | `{id}` | ruta `id`, body `UpdateRoleRequest` | `ApiResponse<RoleDto>` |
| DELETE | `{id}` | ruta `id` | `ApiResponse<bool>` |

### PermissionsController — `api/v1/permissions` (`[Authorize(Roles="ADMIN")]`)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | — | `ApiResponse<IEnumerable<PermissionDto>>` |
| POST | `` | body `CreatePermissionRequest` | `ApiResponse<PermissionDto>` (201) |
| PUT | `{id}` | ruta `id`, body `UpdatePermissionRequest` | `ApiResponse<PermissionDto>` |
| DELETE | `{id}` | ruta `id` | `ApiResponse<bool>` |

### AuditController — `api/v1/audit` (`[Authorize(Roles="ADMIN")]`)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `login-history` | query `page`, `pageSize` | `ApiResponse<PagedResult<LoginHistoryDto>>` |
| GET | `security-events` | query `page`, `pageSize` | `ApiResponse<PagedResult<AuditLogDto>>` |

---

## 2. DashboardService (Dashboard)

Base: `api/v1/dashboard*`. Todos los GET sin body (salvo query). Rate-limited.

### GeneralDashboardController — `api/v1/dashboard`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `summary` | `[Authorize(Policy=DashboardRead)]` | `ApiResponse<GetGeneralSummary>` |
| GET | `indicators` | idem | `ApiResponse<GetGeneralIndicators>` |
| GET | `kpis` | idem | `ApiResponse<GetGeneralKpis>` |
| GET | `system` | `[AllowAnonymous]` | `ApiResponse<GetGeneralSystem>` |
| GET | `health` | `[AllowAnonymous]` (simulado 200 OK) | `ApiResponse<GetGeneralHealth>` |
| GET | `version` | `[AllowAnonymous]` | `ApiResponse<GetGeneralVersion>` |

### IndividualDashboardController — `api/v1/dashboard/individual` (userId del claim, 401 si falta)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` (raíz) | — | `ApiResponse<IndividualDashboardResponse>` |
| GET | `summary` | — | Summary |
| GET | `kpis` | — | Kpis |
| GET | `statistics` | — | Statistics |
| GET | `heatmap` | — | Heatmap |
| GET | `goals` | — | Goals |
| GET | `progress` | — | Progress |
| GET | `activity` | — | Activity |
| GET | `recommendations` | — | Recommendations |
| GET | `rewards` | — | Rewards |
| GET | `notifications` | — | Notifications |
| GET | `biometrics` | — | Biometrics |

### FamilyDashboardController — `api/v1/dashboard/family` (query `familyId`; 401/403/503)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | query `familyId` | `ApiResponse<GetFamilyDashboard>` |
| GET | `statistics` | query `familyId` | Statistics |
| GET | `goals` | query `familyId` | Goals |
| GET | `ranking` | query `familyId` | Ranking |
| GET | `members` | query `familyId` | Members |
| GET | `challenges` | query `familyId` | Challenges |
| GET | `rewards` | query `familyId` | Rewards |
| GET | `heatmap` | query `familyId` | Heatmap |

### CompanyDashboardController — `api/v1/dashboard/company` (query `companyId`; 400/401/403/503)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | query `companyId` | `ApiResponse<GetCompanyDashboard>` |
| GET | `kpis` | query `companyId` | Kpis |
| GET | `statistics` | query `companyId` | Statistics |
| GET | `departments` | query `companyId` | Departments |
| GET | `heatmap` | query `companyId` | Heatmap |
| GET | `adherence` | query `companyId` | Adherence |
| GET | `trends` | query `companyId` | Trends |
| GET | `ranking` | query `companyId` | Ranking |
| GET | `licenses` | query `companyId` | Licenses |
| GET | `organization` | query `companyId` | Organization |

---

## 3. OrganizationAndSaaS (Organization & SaaS)

Base hard-coded `api/v1/...`. Tenant desde `TenantContext` (`tenant_id` claim / `X-Tenant-Id`). Paginación clamp 1–100.

### OrganizationsController — `api/v1/organizations`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateOrgRequest(Name, TaxId, PlanId, ContactInfo, Address)` | `CreatedAtAction` / `ApiResponse<Organization>` (201) |
| GET | `` | query `pageIndex`, `pageSize`, `search` | Pág. de orgs |
| GET | `{id}` | ruta `id` | `ApiResponse<Organization>` |
| PUT | `{id}` | ruta `id`, body `UpdateOrgRequest(Name, TaxId, ContactInfo, Address)` | org actualizada |
| DELETE | `{id}` | ruta `id` | resultado suspensión |
| PATCH | `{id}/activate` | ruta `id` | org activada |
| PATCH | `{id}/suspend` | ruta `id` | org suspendida |
| PATCH | `{id}/restore` | ruta `id` | org restaurada |
| GET | `{id}/statistics` | ruta `id` | estadísticas de org |

### FamiliesController — `api/v1/families`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateFamilyCommand` | 201 |
| GET | `` | query `pageIndex`, `pageSize` | list pág. |
| GET | `{id}` | ruta `id` | familia |
| PUT | `{id}` | ruta `id`, body `UpdateFamilyRequest(Name)` | familia |
| DELETE | `{id}` | ruta `id` | — |
| POST | `{id}/members` | ruta `id`, body `AddMemberRequest(UserId)` | — |
| DELETE | `{id}/members/{userId}` | ruta `id`, `userId` | — |
| PATCH | `{id}/administrator` | ruta `id`, body `TransferAdminRequest(NewAdminUserId)` | — |

### DepartmentsController — `api/v1/departments`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateDepartmentCommand` | 201 |
| GET | `` | query `organizationId`, `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | depto |
| PUT | `{id}` | ruta `id`, body `UpdateDeptRequest(Name, Description, ManagerUserId?, ParentDepartmentId?)` | depto |
| DELETE | `{id}` | ruta `id` | — |
| POST | `{id}/members` | ruta `id`, body `DeptMemberRequest(UserId)` | — |
| DELETE | `{id}/members/{userId}` | ruta `id`, `userId` | — |

### TeamsController — `api/v1/teams`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateTeamCommand` | 201 |
| GET | `` | query `organizationId`, `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | team |
| PUT | `{id}` | ruta `id`, body `UpdateTeamRequest(Name, DepartmentId?, LeaderUserId?)` | team |
| DELETE | `{id}` | ruta `id` | — |

### LicensesController — `api/v1/licenses`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateLicenseCommand` | 201 |
| GET | `` | query `organizationId`, `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | licencia |
| DELETE | `{id}` | ruta `id` | revocada |
| POST | `{id}/assign` | ruta `id`, body `AssignLicenseRequest(UserId)` | — |
| POST | `{id}/renew` | ruta `id`, body `RenewLicenseRequest(NewExpiration)` | — |

### SubscriptionsController — `api/v1/subscriptions`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateSubscriptionCommand` | 201 |
| GET | `` | query `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | suscripción |
| PATCH | `{id}/renew` | ruta `id` | — |
| PATCH | `{id}/change-plan` | ruta `id`, body `ChangePlanRequest(NewPlanId)` | — |

### InvitationsController — `api/v1/invitations`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateInvitationCommand` | 201 |
| GET | `` | query `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | invitación |
| POST | `{token}/accept` | `[AllowAnonymous]` ruta `token` | — |
| POST | `{token}/reject` | `[AllowAnonymous]` ruta `token` | — |
| POST | `{id}/cancel` | ruta `id` | — |
| POST | `{id}/resend` | ruta `id` | — |

---

## 4. AdministrationService (Administration & Configuration)

Base `api/v{version}/[controller]`, v1, `[Authorize(Policy=AdministratorOnlyPolicy)]` (SUPERADMIN/SYSTEMADMINISTRATOR). Cada mutación registra auditoría.

### CatalogsController — `api/v1/catalogs`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateCatalogRequest(Code, Name, Description, Category, Items?)` | 201 |
| GET | `` | query `pageIndex`, `pageSize`, `search`, `category`, `onlyActive` | listág. |
| GET | `{id}` | ruta `id` | catálogo |
| PUT | `{id}` | ruta `id`, body `UpdateCatalogRequest(Name, Description, Category, Items?)` | catálogo |
| DELETE | `{id}` | ruta `id` | — |
| PATCH | `{id}/activate` | ruta `id` | — |
| PATCH | `{id}/deactivate` | ruta `id` | — |

### ParametersController — `api/v1/parameters`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateParameterRequest(Code, Name, Description, DataType, Value, Category, MinValue?, MaxValue?, Unit, Order)` | 201 |
| GET | `` | query `pageIndex`, `pageSize`, `search`, `category`, `onlyActive` | listág. |
| GET | `{id}` | ruta `id` | parámetro |
| PUT | `{id}` | ruta `id`, body `UpdateParameterRequest(...)` | parámetro |
| DELETE | `{id}` | ruta `id` | — |
| PATCH | `{id}/activate` / `deactivate` | ruta `id` | — |

### FeatureFlagsController — `api/v1/feature-flags`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateFeatureFlagRequest(Code, Name, Description, Category, IsSystem)` | 201 |
| GET | `` | query `pageIndex`, `pageSize`, `search`, `category`, `onlyEnabled` | listág. |
| GET | `{id}` | ruta `id` | flag |
| PUT | `{id}` | body `UpdateFeatureFlagRequest(Name, Description, Category)` | flag |
| DELETE | `{id}` | ruta `id` | — |
| PATCH | `{id}/enable` / `disable` | ruta `id` | — |

### LogsController — `api/v1/logs`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `LogEntryRequest` | ingesta |
| POST | `bulk` | body `IReadOnlyList<LogEntryRequest>` | — |
| GET | `` | query `pageIndex`, `pageSize`, `service`, `level`, `userId`, `correlationId`, `fromDate`, `toDate` | listág. |
| GET | `errors` | query `pageIndex`, `pageSize` | listág. |
| GET | `warnings` | query `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | log |

### AuditController — `api/v1/audit`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | query `pageIndex`, `pageSize`, `userId`, `service`, `eventType`, `organizationId`, `companyId`, `fromDate`, `toDate` | listág. |
| GET | `by-user/{userId}` | ruta `userId`, `pageIndex`, `pageSize` | listág. |
| GET | `by-service/{service}` | ruta `service`, `pageIndex`, `pageSize` | listág. |
| GET | `{id}` | ruta `id` | auditoría |

### SettingsController — `api/v1/settings`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | — | settings |
| PUT | `` | body `UpdateSettingsRequest` (`CurrentUser.UserId`) | settings |
| POST | `reset` | `CurrentUser.UserId` | — |

### MaintenanceController — `api/v1/maintenance`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `status` | — | estado |
| PUT | `status` | body `SetMaintenanceModeRequest(IsEnabled, Message, ScheduledEndAt?)` | — |

### ServicesController — `api/v1/services`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `status` | query `forceRefresh` | board de servicios |
| GET | `{service}/status` | ruta `service`, query `forceRefresh` | estado por servicio |

### StatisticsController — `api/v1/statistics`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | — | estadísticas admin |

### IntegrationsController — `api/v1/integrations` (proxy upstream, 503 si cae)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `auth/roles` | — | roles Auth (upstream) |
| GET | `auth/permissions` | — | permisos Auth (upstream) |
| GET | `organization` | — | config Organization (upstream) |

---

## 5. NotificationsAndAlerts (Notifications & Alerts)

Base `api/v1/...`, rate limited. `userId` del claim `ClaimTypes.NameIdentifier` (401 si falta). Ownership → 404/403.

### NotificationsController — `api/v1/notifications`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `SendNotificationDto{UserId(claim), Title, Body, Payload?, Type, Channel}` | `Response<NotificationResponseDto>` |
| POST | `bulk` | body `List<SendNotificationDto>` | `Response<List<NotificationResponseDto>>` |
| POST | `schedule` | body `ScheduleNotificationDto{...+ScheduledFor}` | `Response<NotificationResponseDto>` |
| GET | `` | query `organizationId?`, `familyId?`, `departmentId?` | `Response<List<...>>` |
| GET | `{id}` | ruta `id` | `Response<NotificationResponseDto>` (404/403) |
| DELETE | `{id}` | ruta `id` | `Response<string>` |
| PATCH | `{id}/cancel` | ruta `id` | `Response<string>` |
| PATCH | `{id}/read` | ruta `id` | `Response<string>` |
| PATCH | `read-all` | — | `Response<string>` |
| PATCH | `{id}/archive` | ruta `id` | `Response<string>` |
| PATCH | `{id}/favorite` | ruta `id` | `Response<string>` |
| GET | `user` | query `limit` (1–100) | `List<NotificationItemDto>` (raw) |

### AlertsController — `api/v1/alerts`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateAlertDto{UserId(claim), Title, Body, Source, Priority}` | `Response<AlertDto>` |
| GET | `` | — | `Response<List<AlertDto>>` |
| GET | `{id}` | ruta `id` | `Response<AlertDto>` (404/403) |
| PATCH | `{id}/read` | ruta `id` | `Response<string>` |
| PATCH | `{id}/dismiss` | ruta `id` | `Response<string>` |

### DevicesController — `api/v1/devices`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `register` | body `DeviceRegistrationDto{UserId, DeviceToken, Platform}` | `Response<string>` |
| DELETE | `unregister` | query `deviceToken` | `Response<string>` |

### EmailsController — `api/v1/emails` (`[Authorize(Roles="ADMIN")]`)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `send` | body `SendEmailDto{To, Subject, Body, IsHtml, TemplateId?, TemplateVariables?}` | `Response<NotificationResponseDto>` |
| POST | `template` | body `EmailTemplateDto{To(List), TemplateId, Variables?}` | `Response<NotificationResponseDto>` |
| POST | `bulk` | body `BulkEmailDto{To(≤500), Subject, Body, IsHtml}` | `Response<List<...>>` |

### PushController — `api/v1/push`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `send` | body `SendPushDto{UserId, Title, Body, Payload?, DeviceTokens, Platform?}` (token ≤2048) | `Response<NotificationResponseDto>` |
| POST | `wear` | body `SendPushDto` (Platform=WearOS) | idem |
| POST | `broadcast` | `[ADMIN]` body `BroadcastPushDto{Title, Body, Payload?, UserIds?, OrganizationId?, FamilyId?, DepartmentId?, Platform?}` | `Response<List<...>>` |
| POST | `company` | `[ADMIN]` delega a broadcast | idem |
| POST | `family` | `[ADMIN]` delega a broadcast | idem |
| POST | `department` | `[ADMIN]` delega a broadcast | idem |

### TemplatesController — `api/v1/templates` (`[ADMIN]`)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `` | body `CreateTemplateDto` | `Response<TemplateDto>` |
| GET | `` | — | `Response<List<TemplateDto>>` |
| GET | `{id}` | ruta `id` | `Response<TemplateDto>` |
| PUT | `{id}` | ruta `id`, body `CreateTemplateDto` | `Response<TemplateDto>` |
| DELETE | `{id}` | ruta `id` | `Response<string>` |

### PreferencesController — `api/v1/preferences`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | — | `Response<NotificationPreferenceDto>` |
| PUT | `` | body `UpdatePreferenceDto` | `Response<NotificationPreferenceDto>` |
| PATCH | `push` | query `enabled` | `Response<NotificationPreferenceDto>` |
| PATCH | `email` | query `enabled` | idem |
| PATCH | `wear` | query `enabled` | idem |

### MetricsController — `api/v1/metrics` (`[ADMIN]`)

| Método | Ruta | Retorna |
|---|---|---|
| GET | `` | `Response<MetricsDto>` |
| GET | `delivery` | `Response<DeliveryMetricsDto>` |
| GET | `channels` | `Response<List<ChannelMetricsDto>>` |
| GET | `errors` | `Response<ErrorMetricsDto>` |

### HistoryController — `api/v1/history`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `` | `[ADMIN]` query `page`, `pageSize` | `Response<PaginatedResult<NotificationHistoryDto>>` |
| GET | `user` | — | `Response<List<...>>` |
| GET | `organization/{organizationId}` | ruta `organizationId` | `Response<List<...>>` |

---

## 6. ReportingService (Reporting)

Base `api/v1/reports/...`, rate limited, `ApiResponse<T>`. Identidad de `ICurrentUserService` (401 si falta). Políticas: `ReportRead`, `ReportExport`, `AuthenticatedUser`, `Admin`.

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `reports/dashboard-summary` | query `scope`, `scopeId?`, `from?`, `to?` | `ApiResponse<DashboardSummaryResponse>` |
| GET | `reports/individual` | userId claim; query `from?`, `to?` | `ApiResponse<IndividualReportResponse>` |
| GET | `reports/family/{familyId}` | ruta `familyId`; query `from?`, `to?` | `ApiResponse<FamilyReportResponse>` |
| GET | `reports/company/{companyId}` | ruta `companyId`; query `from?`, `to?` | `ApiResponse<CompanyReportResponse>` |
| GET | `reports/export` | query `scope`, `scopeId?`, `format`, `from?`, `to?`, `metrics` | `File(content, contentType, fileName)` / `BadRequest` |
| GET | `reports/history` | query `pageIndex`, `pageSize`, `scope?`, `format?` | `ApiResponse<PaginatedResponse<ReportHistoryItemDto>>` |
| GET | `reports/statistics` | query `scope`, `scopeId?`, `from?`, `to?` | `ApiResponse<ReportStatisticsResponse>` |
| GET | `reports/trends` | query `scope`, `scopeId?`, `from?`, `to?`, `metrics` | `ApiResponse<ReportTrendsResponse>` |
| GET | `reports/system-metrics` | `[Admin]` | `ApiResponse<GeneralSystemMetricsDto>` |

---

# PARTE 2 — backapi-main

## Envase global (todos los servicios)

`ResponseEnvelopeFilter` envuelve **todos** los resultados en `Response<T> = { bool Success; string Message; T Data; List<string> Errors }`. 2xx → `Success=true`; 4xx/5xx → `Success=false`.

**Auth:** todos `[Authorize]` (JWT). `CurrentUser` resuelve: `UserId` (claim), `TenantId` (`tenant_id`/`X-Tenant-Id`), `FamilyId` (`family_id`/`X-Family-Id`), `CompanyId` (`company_id`/`X-Company-Id`), `IsAdministrator` (role en `ADMIN|SUPERADMIN|SYSTEMADMINISTRATOR`). Gate anti-IDOR: `CanAccessUser = (self || admin)`.

---

## 1. ApiGateway

No tiene controllers; es un reverse proxy catch-all (`MapFallback`) + endpoints propios.

| Ruta / Comport. | Recibe | Retorna |
|---|---|---|
| `/health` (GET, anón) | — | 200 `{status="Healthy", service, timestampUtc}` |
| `/health/live` (GET, anón) | — | 200 `{status="Healthy"}` |
| `/api/v1/gateway/services` (GET, role admin) | — | mapa `serviceName → BaseUri` de `IServiceRegistry` |
| `*` (cualquier método) | ruta+query+headers+body original; `X-Internal-Service-Key` a ML/Sedentary | streams respuesta upstream verbatim; `401` si no-JWT en ruta no pública; `404` sin match |
| Rutas públicas sin auth | `login/register/forgot/reset/send-confirmation/confirm-email/refresh-token` + `/predictive-alerts`, `/recommendations`, `/sedentary-risk`, `/active-breaks`, `/goals/reminders`, `/sedentary-score` | proxy a upstream |

Middleware global: `X-Correlation-Id`, headers de seguridad, CORS, rate limiter IP (429).

---

## 2. MedicalDataService — `api/v1/medical`

### MedicalController

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `readings` (also `/api/v1/medical-data`) | body `MedicalReadingRequest{HeartRate(1–260), Hrv(0–300), Spo2(1–100), Steps(0–200000), Latitude?, Longitude?, Acel.X/Y/Z?, Giro.X/Y/Z?, SysBP=0, DiaBP=0, Weight=0, Height=0, DeviceId="unknown", RecordedAtUtc}`. `FamilyId/CompanyId`[JsonIgnore] desde current. | 200 `MedicalReadingResponse{Id, UserId, HeartRate, Hrv, Spo2, Steps, Latitude?, Longitude?, RecordedAtUtc}`; 401 |
| POST | `readings/batch` | body `IReadOnlyList<MedicalReadingRequest>` (1..500) | 200 `IReadOnlyList<MedicalReadingResponse>` |
| GET | `latest` | — | 200 `MedicalReadingResponse`; **404** si no hay |
| GET | `history` | query `from?`, `to?`, `page=1`, `pageSize=50` (1–100) | 200 `IReadOnlyList<MedicalReadingResponse>` |
| GET | `biometrics/{userId}` | ruta `userId` (self/admin) | 200 `MedicalDataResponseDto{UserId, HeartRate, SysBP, DiaBP, Weight, Height, Bmi, RecordedAt}`; 403 |
| GET | `family/{familyId}` | ruta `familyId` | 200 `IReadOnlyList<MedicalDataResponseDto>`; 403/503 |

### StatisticsController — `api/v1/statistics` (`[ADMIN...]`)

| Método | Ruta | Retorna |
|---|---|---|
| GET | `` | 200 `MedicalStatisticsResponse{TotalReadings, ActiveUsers, AvgHeartRate, AvgSpo2, GeneratedAtUtc}` |

---

## 3. MLPredictionService — `api/v1/ml`

### MLController

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `predict` | body `PredictionRequest{HeartRate(0–260), Hrv(0–300), Spo2(0–100), Steps(0–200000), SedentaryMinutes(0–1440), SleepHours(0–24)}` | 200 `PredictionResponse{UserId, RiskScore, RiskLevel, RecommendedActions, ModelVersion, PredictedAtUtc}` |
| GET | `recommendations/{userId}` | ruta `userId` | 200 `IReadOnlyList<RecommendationDto{Id, Category, Title, Description, PriorityScore}>`; 403 |
| GET | `risk-trend/{userId}` | ruta `userId` | 200 `HealthRiskTrendDto{UserId, RiskLevel, SedentaryRiskScore, RecommendedActions}`; 403 |
| GET | `risk/{userId}` | ruta `userId` (delega a trend) | igual que Trend |
| POST | `dataset` | body `DatasetRequest{Required TenantId, JsonElement Data}` | 202 Accepted; 403 mismatch de tenant |
| GET | `model/status` | `[ADMIN]` | `{status:"Ready", modelVersion, engine, lastCheckedAtUtc}` |
| GET | `config` | `[ADMIN]` | `MlConfiguration{Id, HighRiskThreshold=0.7, MediumRiskThreshold=0.4, ActiveModelVersion}` |
| PUT | `config` | `[ADMIN]` body `MlConfiguration` (Id forzado "global") | `MlConfiguration` actualizado |

### CompatibilityController — `api/v1` (Policy `InternalService`, header `X-Internal-Service-Key`)

| Método | Rutas | Recibe | Retorna |
|---|---|---|---|
| POST | `predictive-alerts`, `recommendations`, `sedentary-risk` | body `PredictAlertRequest{UserId, AlertType, Recommendation(≤500), SedentaryRisk(0–100)}` | 200 `{accepted:true, ...}` |

---

## 4. SedentaryEngineService — `api/v1/sedentary`

### SedentaryController

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `activity` | body `ActivityRequest{DailySteps(0–200000), ActiveMinutes(0–1440), SedentaryHours(0–24), CaloriesBurned(0–20000), HourlyHeatmap?, RecordedAtUtc}` (CompanyId[JsonIgnore]) | 200 `SedentaryScoreResponse{UserId, Score, RiskLevel, RecordedAtUtc}` |
| GET | `score/{userId}` | ruta `userId` | 200 `SedentaryActivityResponseDto{DailySteps, ActiveMinutes, SedentaryHours, CaloriesBurned, HourlyHeatmap}`; 404/403 |
| GET | `score` | — | 200 `SedentaryScoreResponse`; 404 |
| POST | `goals` | body `GoalRequest{DailyStepsTarget(100–100000), ActiveMinutesTarget(1–1440)}` | **204** |
| GET | `goals` | — | 200 `SedentaryGoal{Id, UserId, DailyStepsTarget=8000, ActiveMinutesTarget=30, UpdatedAtUtc}` |
| GET | `progress` | — | 200 `ProgressResponse{DailySteps, DailyStepsTarget, ActiveMinutes, ActiveMinutesTarget, StepsProgress, ActiveProgress}` |
| GET | `company/{companyId}/adherence` | ruta `companyId` | 200 `CompanyAdherenceResponseDto{CompanyId, AdherencePercentage, TotalEmployees, ActiveEmployees, HighRiskDepartments}`; 403/503 |

### EngineAdminController — `api/v1` (`[ADMIN...]`)

| Método | Ruta | Retorna |
|---|---|---|
| GET | `metrics/summary` | `{totalUsers, averageScore, highRiskUsers, generatedAtUtc}` |
| GET | `config` | `EngineConfiguration{Id, InactivityThresholdMinutes=45, ReminderCooldownMinutes=30, MinimumDailySteps=8000}` |
| PUT | `config` | body `EngineConfiguration` (Id forzado "global") → actualizado |

### CompatibilityController — `api/v1` (Policy InternalService)

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `active-breaks`, `goals/reminders`, `config/sedentary-score` | `SedentaryAlertRequest{UserId, Type, Message, SedentaryScore}` | 200 `{accepted:true, ...}` |

---

## 5. GamificationService — `api/v1/gamification`

### GamificationController

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `profile` | — | 200 `GamificationProfileResponse{UserId, Points, Level, BadgesUnlocked, CurrentStreakDays, RecentRewards}` |
| POST | `events` | body `GamificationEventRequest{EventType(≤80), Points(1–10000), RewardName?}` | 200 `GamificationProfileResponse` (updated); 400 `UnknownEventTypeException` |
| GET | `user/{userId}/included` | ruta `userId` (self/admin) | 200 `UserRewardsResponseDto{UserId, Points, BadgesUnlocked, CurrentStreakDays, RecentRewards}`; 403 |
| GET | `family/{familyId}/challenges` | ruta `familyId` | 200 `IReadOnlyList<ChallengeProgressDto{ChallengeId, Title, ProgressPercentage, Completed}>`; 403/503 |
| GET | `leaderboard` | query `take=20` (1–100) | 200 `IReadOnlyList<LeaderboardItem{UserId, Points, Level, Position}>` |
| POST | `challenges` | `[Admin]` body `CreateChallengeRequest{Title(≤120), ScopeType, ScopeId, EndsAtUtc}` | 200 `ChallengeProgressDto` |

### CompatibilityController — `api/v1`

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| GET | `challenges/organizations/{tenantId}` | ruta `tenantId` (tenant match) | 200 `IReadOnlyList<ChallengeProgressDto>` (relación). 403 |
| GET | `rankings/families/{familyId}` | ruta `familyId` | 200 `IReadOnlyList<LeaderboardItem>`; 403/503 |

---

## 6. IngestionService — `api/v1/ingestion`

### IngestionController

| Método | Ruta | Recibe | Retorna |
|---|---|---|---|
| POST | `events` | body `IngestionEventRequest{DeviceId(≤100), EventType(≤80), Source(Mobile/Wearable/Web/System), OccurredAtUtc, Payload(JsonElement), IdempotencyKey?}` | **201** `IngestionEventResponse{Id, EventType, Source, EventId(DeviceId), OccurredAtUtc, ReceivedAtUtc}`; dedup por IdempotencyKey |
| POST | `sync` | body `SyncBatchRequest{ClientBatchId(≤100), DeviceId(≤100), VitalSigns?, ActivitySessions?, Alerts?}` | 200 `SyncBatchResponse{ClientBatchId, Status, AcceptedItems, RejectedItems, CompletedAtUtc}` |
| GET | `history` | query `from?`, `to?`, `type?`, `page`, `pageSize(1–100)` | 200 `IReadOnlyList<IngestionEventResponse>` |
| GET | `sync/{clientBatchId}` | ruta `clientBatchId` | 200 `SyncBatchResponse`; 404 |
| GET | `sync/status` | — | `{status:"Available", serverTimeUtc}` |

**Sub-DTOs de `SyncBatchRequest`:**
- `VitalSignSyncItem {Timeestamp, HeartRate(1–260), Hrv(0–300), Spo2(0–100), Steps(0–200000)}`
- `ActivitySessionSyncItem {StartTime, EndTime, Type(≤60), DurationMinutes(1–1440)}`
- `AlertSyncItem {Timestamp, Type(≤60), DurationMinutes(1–1440), Acknowledged}`