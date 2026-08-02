# LifeBalance - Notifications & Alerts Microservice 🔔

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Firebase](https://img.shields.io/badge/Push-FirebaseAdmin-FFCA28?logo=firebase)

Central notifications and alerts service for **LifeBalance**. Handles dispatch, scheduling, and management of Push, Email, and in-app notifications.

> Details in [AGENTS.md](../AGENTS.md).

---

## 🏛️ Architecture

Clean Architecture + DDD. **NO MediatR used in this service.**
Flow: Controller (Presentation) → Service Interface (Application) → Implementation (Infrastructure) → MongoDB.

### MongoDB Collections
`notifications`, `notification_preferences`, `notification_templates`, `scheduled_notifications`, `delivery_logs`, `alerts`, `metrics_records`, `device_registrations`.

---

## 🔐 Security & Validation

- **Anti-IDOR:** `userId` strictly from JWT `ClaimTypes.NameIdentifier`. Other users' resources → **403**.
- **`ADMIN` Role Required:** `Templates`, `Metrics`, `History` (global & organization), `Push` (`broadcast`/`company`/`family`/`department`), and `Emails`.
- **Validations:** Bulk ≤500 emails, email format checks, DTO DataAnnotations, IP rate limiting (429).
- **Hardened JWT:** HS256, 1-min ClockSkew; invalid/short secret → fail-fast startup crash in Production.
- **Generic Client Errors:** Details in logs only.

---

## ⚙️ Environment Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__MongoDb` | MongoDB connection URL |
| `DatabaseName` | Database name (`LifeBalanceNotificationsDb`) |
| `Jwt__SecretKey` | Shared JWT secret key |
| `Jwt__Issuer` / `Jwt__Audience` | `LifeBalance` / `LifeBalance` |
| `Firebase__ProjectId` | Firebase Project ID |
| `Firebase__CredentialsPath` | Path to Firebase credentials JSON |
| `Smtp__*` | SMTP host, port, credentials, sender |
| `Cors__AllowedOrigins` | CORS Allowlist |

---

## 📚 API Endpoints (`api/v1/`)

- `notifications`: CRUD, schedule, bulk, read-all, patches (`read`, `archive`, `favorite`, `cancel`)
- `alerts`: CRUD, read/dismiss patches
- `devices`: `register`, `unregister`
- `emails`: `send`, `bulk`, `template` (Admin)
- `push`: `send`, `broadcast`, `company`, `family`, `department`, `wear` (Admin)
- `history`: `user`, `organization/{organizationId}` (Admin)
- `metrics`: global, channels, delivery, errors (Admin)
- `preferences`: GET/PUT, patches (`email`, `push`, `wear`)
- `templates`: CRUD (Admin)

---

## 🧪 Testing

```bash
dotnet test tests/LifeBalance.Notifications.UnitTests/LifeBalance.Notifications.UnitTests.csproj
```
~249 green unit tests.
