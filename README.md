# LifeBalance - Backend

¡Bienvenido al repositorio central del backend de **LifeBalance**! 🚀

Este repositorio contiene la arquitectura de microservicios backend que impulsa la plataforma **LifeBalance**. Está construido con el ecosistema **.NET 9** (C#), aplicando principios de **Clean Architecture**, **Domain-Driven Design (DDD)** y **CQRS** para asegurar un código mantenible, escalable y robusto.

> **Para desarrolladores y agentes de IA**: consulta [**AGENTS.md**](./AGENTS.md) — describe con detalle exacto cada microservicio, sus reglas de seguridad, endpoints, bases de datos y comandos, para no tener que leer todo el proyecto.

---

## 🏛️ Arquitectura Global

El backend de LifeBalance está estructurado como un conjunto de microservicios independientes. Cada microservicio cuenta con su propia base de datos (Database-per-service) garantizando un bajo acoplamiento.

### Principales Tecnologías Utilizadas
* **Framework:** .NET 9.0 (ASP.NET Core)
* **Arquitectura:** Clean Architecture, DDD, CQRS (MediatR, excepto Notifications & Alerts)
* **Base de Datos:** MongoDB (NoSQL) — una base por servicio
* **Caché:** Redis (solo en el docker-compose local de Organization & SaaS)
* **Autenticación y Seguridad:** JWT HS256 compartido entre servicios (Issuer/Audience `LifeBalance`), BCrypt, remediaciones OWASP/DevSecOps aplicadas
* **Observabilidad:** Serilog (Dashboard), Health Checks, Swagger solo en Development
* **Contenerización y Despliegue:** Docker, Docker Compose, despliegue en **Render** (`render.yaml`)
* **CI/CD:** GitHub Actions (build + tests por servicio), CodeQL, Dependabot

---

## 📦 Microservicios Disponibles

Actualmente, el repositorio cuenta con 4 microservicios principales:

### 1. 🔐 Auth & Profile (`/Auth_Profile`)
Microservicio de **Autenticación, Autorización y Gestión de Perfiles**.
* **Responsabilidades:** Login, registro, rotación de refresh tokens, gestión de contraseñas, auditorías de acceso y perfiles de usuario.
* **Seguridad:** JWT Bearer, RBAC/PBAC, control de fuerza bruta (lockout), auditoría, fallback del rol `USER` para cuentas sin roles.
* **Base de Datos:** `LifeBalance_Auth` (MongoDB).
* **Tests:** ~164 unitarios.

### 2. 📊 Dashboard Service (`/DashboardService`)
Microservicio de agregación, punto de entrada de las interfaces de usuario.
* **Responsabilidades:** Orquestación de datos de otros microservicios para renderizar los dashboards individual, familiar, de empresa y general.
* **Comportamiento fail-closed:** sin datos fabricados; si un upstream falla → `503` (`UpstreamServiceUnavailableException`). Validación de membresía familia/empresa contra Organization → `403`.
* **Seguridad:** ServiceUrls con HTTPS obligatorio fuera de Development; CORS allowlist (`https://lifebalance-adv3.onrender.com`).
* **Base de Datos:** `lifebalance_dashboard` (MongoDB).
* **Tests:** ~163 unitarios.

### 3. 🏢 Organization & SaaS (`/OrganizationAndSaaS`)
Núcleo empresarial (B2B/B2C) de la plataforma.
* **Responsabilidades:** Gestión multi-tenant de Empresas, Familias, Departamentos y Equipos. Licencias, invitaciones y planes SaaS.
* **Aislamiento Multi-Tenant:** filtro de tenant incondicional en repositorios (claim `tenant_id` prioritario sobre header `X-Tenant-Id`); excepción solo para entidades globales (`IGlobalTenantEntity`, ej. `SaaSPlan`).
* **Seguridad:** JWT fail-fast (secreto vacío/placeholder/<32 bytes → no arranca); `FallbackPolicy` que exige autenticación en todo salvo `/health` y aceptar/rechazar invitaciones.
* **Base de Datos:** `LifeBalance_OrganizationSaaS` (MongoDB).
* **Tests:** ~244 unitarios.

### 4. 🔔 Notifications & Alerts (`/NotificationsAndAlerts`)
API de gestión de alertas y notificaciones del sistema.
* **Responsabilidades:** Despacho de notificaciones (Push, Email, in-app), plantillas, preferencias, historial, métricas y dispositivos.
* **Arquitectura:** Clean Architecture **sin MediatR** (Controller → interfaz → implementación → MongoDB).
* **Seguridad:** JWT endurecido (HS256, ClockSkew 1 min, placeholder rechazado en Production); endpoints de templates/metrics/history-global/push/emails requieren rol `ADMIN`; anti-IDOR (userId del claim).
* **Base de Datos:** `LifeBalanceNotificationsDb` (MongoDB).
* **Tests:** ~249 unitarios.

---

## 🚀 Entorno de Desarrollo y Despliegue

### Levantar el entorno local (Docker Compose)
Cada microservicio incluye su propio `docker-compose.yml`. Levanta un servicio individualmente:
```bash
docker-compose up --build -d
```

| Servicio | Puerto local (dev) | Puerto Docker Compose |
|---|---|---|
| Auth & Profile | `http://localhost:5200` / `https://localhost:7200` | `10000:10000` |
| Dashboard | `http://localhost:5000` / `https://localhost:5001` | `5000:8080`, `5001:8081` |
| Notifications | `http://localhost:5054` / `https://localhost:7269` | `5000:10000` |
| Organization & SaaS | `http://localhost:5072` / `https://localhost:7207` | `8080:8080` |

### Despliegue en Render
El archivo `render.yaml` (raíz) orquesta el despliegue automático de los 4 microservicios (Blueprint, Docker, plan Free).

**⚠️ IMPORTANTE:** los 4 servicios validan JWTs con el **mismo secreto** (`Jwt__SecretKey` en Auth/Dashboard, `Jwt__SecretKey` en Notifications, `JwtSettings__Secret` en Organization). Debe ser idéntico en los 4 servicios. El secreto de Organization se filtró una vez en el historial de git: **rota el secreto** si el repositorio no es privado o fue compartido. Organization crasheará hasta tener `JwtSettings__Secret` configurado (fail-fast intencional).

---

## 🛡️ Seguridad (remediaciones aplicadas)

1. **Fail-fast JWT:** Organization (siempre) y Notifications (en Production) abortan el arranque si el secreto está vacío, es placeholder o mide <32 bytes UTF-8.
2. **HTTPS obligatorio:** Dashboard valida que todos los `ServiceUrls__*` usen `https://` fuera de Development.
3. **Fail-closed:** los clientes HTTP upstream devuelven `null` ante fallo → `503`; no existen datos fabricados.
4. **Anti-IDOR:** el `userId` siempre proviene del claim `ClaimTypes.NameIdentifier`; recursos ajenos → `403`.
5. **Aislamiento multi-tenant:** filtro incondicional en repositorios; `tenant_id` del claim tiene prioridad sobre el header.
6. **Rate limiting** por IP (429), **paginación clamp 1–100**, **CORS allowlist**, **Swagger solo en Development**, **mensajes de error genéricos** (detalle solo en logs).
7. **Dockerfiles non-root** (`appuser`), Mongo/Redis/mongo-express en loopback `127.0.0.1` en los compose, sin credenciales hardcodeadas.
8. **Roles en mayúsculas:** claim `ClaimTypes.Role` = `NormalizedName` (ej. `USER`, `ADMIN`); Auth asigna `USER` por defecto a cuentas sin roles.

---

## 🧪 Testing y CI/CD

* **~820 tests unitarios** verdes (Auth 164, Dashboard 163, Organization 244, Notifications 249) + **4 tests de contrato** en `tests/ContractTests/` (Auth↔Dashboard y degradación elegante).
* **`.github/workflows/ci.yml`:** job por servicio (restore → build Release → tests con cobertura → docker build dry-run) + contract tests + gate `ci-success` obligatorio.
* **`.github/workflows/codeql.yml`:** análisis estático csharp en push/PR + semanal.
* **`.github/dependabot.yml`:** dependencias nuget (×4 servicios) y GitHub Actions semanal.

```bash
# Tests de un servicio (desde su carpeta)
dotnet test tests/<Proyecto>.UnitTests/<Proyecto>.UnitTests.csproj --configuration Release
# Tests de contrato (raíz)
dotnet test tests/ContractTests/ContractTests.csproj --configuration Release
```

---

## 📝 Documentación Adicional

* [**AGENTS.md**](./AGENTS.md) — guía exacta de los 4 microservicios (proyectos, BD, endpoints, reglas de seguridad, infra) para agentes y desarrolladores.
* [`/Auth_Profile/README.md`](./Auth_Profile/README.md)
* [`/DashboardService/README.md`](./DashboardService/README.md)
* [`/OrganizationAndSaaS/README.md`](./OrganizationAndSaaS/README.md)
* [`/NotificationsAndAlerts/README.md`](./NotificationsAndAlerts/README.md)

---
*Propietario — © LifeBalance 2026. Todos los derechos reservados.*
