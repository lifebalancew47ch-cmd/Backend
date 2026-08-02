# LifeBalance - Auth & Profile Microservice

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD%20%2B%20CQRS-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Security](https://img.shields.io/badge/Security-JWT%20%7C%20RBAC-red)

Authentication, Authorization, and Profile Management microservice for **LifeBalance**. Built on **.NET 9.0** following **Clean Architecture**, **DDD**, and **CQRS** with MediatR.

> Details in [AGENTS.md](../AGENTS.md).

---

## 🏛️ System Architecture
1. **Auth.Domain**: Domain entities, enums, pure business logic.
2. **Auth.Application**: Commands, queries, handlers, DTOs, FluentValidation.
3. **Auth.Infrastructure**: MongoDB repos, cryptographic services, JWT, audit logs, middlewares.
4. **Auth.Api**: REST controllers, API versioning, Swagger, CORS, Rate Limiting.

---

## 🔒 Security Implementation
- **JWT Bearer Auth:** Signed HS256, Issuer/Audience `LifeBalance`, `ClockSkew` 1m.
- **Refresh Token Rotation & Revocation:** Unique index on `Token` in MongoDB.
- **Default Role:** Accounts without `RoleIds` receive `USER` role on login/refresh.
- **BCrypt Password Hashing:** One-way salted hashing.
- **RBAC / PBAC:** Role/Policy restrictions on endpoints.
- **HTTP Security Headers:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`, `X-XSS-Protection`, HSTS.
- **Rate Limiting:** Configurable global and per-endpoint policies.
- **Validation:** Strict FluentValidation rules on commands.
- **NoSQL Injection & Mass Assignment Protection:** Official Mongo driver, strict DTO isolation.
- **Brute Force Lockout:** 5 failed attempts → 15-min lockout.
- **Full Audit Logging:** Logins, failed logins, password changes, lockouts.
- **Global Error Handling:** Standardized responses hiding stack traces in non-Development.

---

## 📂 MongoDB Collections & Indexes
- **`users`** — Unique: `Email`, `Username`; Index: `CreatedAt`
- **`refresh_tokens`** — Unique: `Token`; Indexes: `UserId`, `ExpiresAt`, `CreatedAt`
- **`roles`** — Unique: `NormalizedName`
- **`permissions`** — Unique: `NormalizedName`
- **`user_preferences`** — Index: `UserId`
- **`audit_logs`** — Indexes: `UserId`, `Action`, `CreatedAt`
- **`login_history`** — Indexes: `UserId`, `CreatedAt`
- **`password_reset_tokens`** — Unique: `Token`; Indexes: `UserId`, `CreatedAt`
- **`email_confirmation_tokens`** — Unique: `Token`; Indexes: `UserId`, `CreatedAt`

---

## 🛠️ Environment Variables

| Variable | Description | Default |
|---|---|---|
| `MongoDb__ConnectionString` | MongoDB connection URL | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Database name | `LifeBalance_Auth` |
| `Jwt__SecretKey` | JWT Secret Key (shared across services) | dev placeholder |
| `Jwt__Issuer` | JWT Issuer | `LifeBalance` |
| `Jwt__Audience` | JWT Audience | `LifeBalance` |
| `Jwt__AccessTokenExpirationMinutes` | Access token lifespan | `30` |
| `Jwt__RefreshTokenExpirationDays` | Refresh token lifespan | `7` |
| `Security__MaxFailedLoginAttempts` | Max attempts before lockout | `5` |
| `Security__LockoutDurationMinutes` | Lockout duration (minutes) | `15` |

---

## 🚀 Render Deployment
- Docker environment reading root Dockerfile.
- Port: `10000`. Health check: `GET /health`.

---

## 🧪 Testing
```bash
dotnet test tests/UnitTests/UnitTests.csproj --configuration Release
```
~164 green unit tests.

---

## 📚 API Endpoints

### Auth (`/api/v1/auth/`)
`register`, `login`, `logout`, `refresh-token`, `revoke-token`, `forgot-password`, `reset-password`, `send-confirmation`, `confirm-email`

### Profile (`/api/v1/profile/`)
`GET me`, `PUT me`, `GET preferences`, `PUT preferences`, `PUT change-password`

### Roles & Permissions — Admin
`api/v1/roles`, `api/v1/permissions`

### Audit & Health
`api/v1/audit/login-history`, `security-events`, `/health`
