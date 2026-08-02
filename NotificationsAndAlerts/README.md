# LifeBalance - Notifications & Alerts Microservice 🔔

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Firebase](https://img.shields.io/badge/Push-FirebaseAdmin-FFCA28?logo=firebase)

El **Microservicio de Notificaciones y Alertas** forma parte central del ecosistema **LifeBalance**. Es responsable de la distribución, programación y gestión de todas las notificaciones (Push, Email, in-app) para los usuarios y organizaciones de la plataforma.

> Más detalle en el [AGENTS.md](../AGENTS.md) del repositorio.

---

## 🏛️ Arquitectura

Clean Architecture + DDD. **IMPORTANTE: este servicio NO usa MediatR ni Commands/Queries.** El patrón es:

```
Controller (Presentation)
   → Interfaz de servicio (Application/Interfaces)
   → Implementación (Infrastructure/Services)
   → MongoDB (Infrastructure/Data)
```

1. **LifeBalance.Notifications.Domain**: Entidades núcleo (Notificaciones, Alertas, Preferencias, Plantillas), Value Objects y reglas de negocio.
2. **LifeBalance.Notifications.Application**: DTOs (con DataAnnotations), interfaces de servicio y lógica de orquestación.
3. **LifeBalance.Notifications.Infrastructure**: Integraciones externas (MongoDB, FirebaseAdmin para Push, servicios SMTP/Email).
4. **LifeBalance.Notifications.Presentation**: Entry point de la API REST, configuración de Swagger/OpenAPI y middlewares.
5. **LifeBalance.Notifications.Shared**: Elementos y utilidades transversales compartidos.

---

## 🚀 Tecnologías Clave

- **.NET 9.0**: Framework base.
- **MongoDB Driver**: Persistencia (`LifeBalanceNotificationsDb`).
- **FirebaseAdmin**: Push notifications (FCM).
- **Autenticación JWT Bearer endurecida:** HS256, `ValidAlgorithms = {HmacSha256}`, `ClockSkew` 1 minuto; secreto vacío/placeholder/<32 bytes → **no arranca en Production** (fail-fast).
- **Swagger / OpenAPI** (solo en Development).

### Colecciones MongoDB
`notifications`, `notification_preferences`, `notification_templates`, `scheduled_notifications`, `delivery_logs`, `alerts`, `metrics_records`, `device_registrations`.

---

## 🔐 Seguridad

- **Anti-IDOR:** el `userId` siempre proviene del claim `ClaimTypes.NameIdentifier` del token; nunca se acepta por query/body. Recursos de otros usuarios → **403**.
- **Rol `ADMIN` obligatorio** en: `Templates`, `Metrics`, `History` global y `history/organization/{organizationId}`, `Push` (`broadcast`/`company`/`family`/`department`) y `Emails`.
- **Validaciones:** bulk ≤500 emails, validación de emails, DataAnnotations en DTOs, **rate limiting por IP** (429).
- **Mensajes de error genéricos** al cliente ("Delivery failed.", "Resource not found."); detalle solo en logs.

---

## ⚙️ Configuración y Despliegue

### Requisitos Previos
- .NET 9.0 SDK
- MongoDB (local o Atlas)
- Credenciales de Firebase Admin (archivo JSON de cuenta de servicio)
- Docker & Docker Compose (opcional)

### Variables de Entorno

| Variable | Descripción |
|---|---|
| `ConnectionStrings__MongoDb` | URL de conexión a MongoDB |
| `DatabaseName` | Nombre de la base de datos (`LifeBalanceNotificationsDb`) |
| `Jwt__SecretKey` | Clave secreta JWT compartida con los otros 3 servicios |
| `Jwt__Issuer` / `Jwt__Audience` | `LifeBalance` / `LifeBalance` |
| `Firebase__ProjectId` | ID del proyecto de Firebase |
| `Firebase__CredentialsPath` | Ruta al JSON de credenciales de Firebase |
| `Smtp__*` | Host, puerto, credenciales SMTP y remitente |
| `Cors__AllowedOrigins` | Allowlist de CORS (frontend `https://lifebalance-adv3.onrender.com` + localhost) |

### Ejecución Local

```bash
# Restaurar y compilar
dotnet build LifeBalance.Notifications.sln

# Ejecutar la API (http://localhost:5054 / https://localhost:7269)
dotnet run --project src/LifeBalance.Notifications.Presentation
```

### Ejecutar con Docker

```bash
docker-compose up -d --build
```
API en `http://localhost:5000` (contenedor en el puerto `10000`), MongoDB en loopback `127.0.0.1:27017`.

### Render
Configurado en `render.yaml` de la raíz como `lifebalance-notifications-api` (plan free, puerto 10000). Recuerda el **mismo `Jwt__SecretKey`** que los otros servicios.

---

## 📚 Endpoints de la API (prefijo `api/v1/`)

| Recurso | Endpoints |
|---|---|
| `notifications` | CRUD + `POST schedule`/`bulk`, `GET user`, `PATCH read-all`/`{id}:read`/`archive`/`favorite`/`cancel`, `DELETE {id}` |
| `alerts` | CRUD + `PATCH {id}/read`, `PATCH {id}/dismiss` |
| `devices` | `POST register`, `DELETE unregister` |
| `emails` | `POST send`, `bulk`, `template` — Admin |
| `push` | `POST send`, `broadcast`, `company`, `family`, `department`, `wear` — Admin (broadcast/company/family/department) |
| `history` | `GET user` (propio), `GET organization/{organizationId}` — Admin |
| `metrics` | `GET` (global), `channels`, `delivery`, `errors` — Admin |
| `preferences` | `GET`, `PUT`, `PATCH email`/`push`/`wear` |
| `templates` | CRUD — Admin |

---

## 🧪 Testing

```bash
# Pruebas Unitarias (~249 verdes)
dotnet test tests/LifeBalance.Notifications.UnitTests/LifeBalance.Notifications.UnitTests.csproj

# Pruebas de Integración
dotnet test tests/LifeBalance.Notifications.IntegrationTests/LifeBalance.Notifications.IntegrationTests.csproj
```
