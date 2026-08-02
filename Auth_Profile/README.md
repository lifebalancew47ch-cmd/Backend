# LifeBalance - Auth & Profile Microservice

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD%20%2B%20CQRS-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Security](https://img.shields.io/badge/Security-JWT%20%7C%20RBAC-red)

Microservicio robusto de Autenticación, Autorización y Gestión de Perfiles para la plataforma **LifeBalance**. Construido sobre **.NET 9.0** y diseñado siguiendo los principios de **Clean Architecture**, **DDD (Domain-Driven Design)** y el patrón **CQRS** con MediatR.

> Más detalle en el [AGENTS.md](../AGENTS.md) del repositorio.

---

## 🏛️ Arquitectura del Sistema
El microservicio está estructurado en 4 capas principales:
1. **Auth.Domain**: Entidades de dominio, enumeraciones y reglas de negocio puras (sin dependencias externas).
2. **Auth.Application**: Comandos, consultas, manejadores (handlers), DTOs, validaciones con FluentValidation y mapeos.
3. **Auth.Infrastructure**: Implementación de repositorios MongoDB, servicios criptográficos, JWT, logs de auditoría y middlewares (excepciones globales, cabeceras de seguridad y logging de solicitudes).
4. **Auth.Api**: Controladores REST con control de versiones de API, configuración de Swagger, CORS y Rate Limiting.

---

## 🔒 Medidas de Seguridad Implementadas
- **Autenticación JWT Bearer:** Tokens firmados con HS256, Issuer/Audience `LifeBalance`, `ClockSkew` 1 minuto.
- **Rotación y Revocación de Refresh Tokens:** almacenados en MongoDB con índice único por `Token`, invalidados al cerrar sesión o revocar.
- **Rol por defecto:** las cuentas sin `RoleIds` reciben automáticamente el rol `USER` en login/refresh (fix del 403 en Dashboard).
- **BCrypt para Contraseñas:** hash seguro unidireccional (nunca se almacena en texto plano).
- **Autorización Basada en Roles y Políticas (RBAC / PBAC):** restricciones por endpoint.
- **Cabeceras de Seguridad HTTP:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`, `X-XSS-Protection` y HSTS.
- **Rate Limiting:** límite de solicitudes configurable a nivel global y específico (ej. `/auth/login`).
- **Validación Estricta:** FluentValidation en cada comando (emails, contraseñas fuertes, UUIDs, longitudes máximas).
- **Protección contra NoSQL Injection & Mass Assignment:** uso del driver oficial de MongoDB y separación estricta mediante DTOs.
- **Bloqueo de Cuenta por Fuerza Bruta:** 5 intentos fallidos → lockout de 15 minutos (configurable).
- **Auditoría Completa:** login, login fallido, cambio de contraseña, lockouts, etc.
- **Manejo Global de Errores:** respuestas estandarizadas sin exponer StackTrace fuera de Development.

### ⚠️ Limitaciones conocidas (hardening pendiente)
- El secreto JWT placeholder de `appsettings.json` **no** aborta el arranque (los demás servicios ya lo hacen).
- Los refresh tokens se almacenan **sin hash** (texto plano).
- `revoke-all` no valida propiedad del token (posible IDOR).
- El lockout de contraseña es evadible usando el refresh token.
- Sin 2FA; Swagger habilitado y CORS `AllowAnyOrigin` fuera de Development.

---

## 📂 Colecciones de MongoDB e Índices

Colecciones inicializadas automáticamente por la aplicación al iniciar (`Persistence/MongoDbInitializer.cs`):

- **`users`** — Índice único: `Email`, `Username`; índice: `CreatedAt`
- **`refresh_tokens`** — Índice único: `Token`; índices: `UserId`, `ExpiresAt`, `CreatedAt`
- **`roles`** — Índice único: `NormalizedName`
- **`permissions`** — Índice único: `NormalizedName`
- **`user_preferences`** — Índice: `UserId`
- **`audit_logs`** — Índices: `UserId`, `Action`, `CreatedAt`
- **`login_history`** — Índices: `UserId`, `CreatedAt`
- **`password_reset_tokens`** — Índice único: `Token`; índices: `UserId`, `CreatedAt`
- **`email_confirmation_tokens`** — Índice único: `Token`; índices: `UserId`, `CreatedAt`

---

## 🛠️ Variables de Entorno y Configuración
Configuración vía *Options Pattern*. En producción se definen con variables de entorno (p. ej. **Render**):

| Variable de Entorno | Descripción | Valor por Defecto |
|---|---|---|
| `MongoDb__ConnectionString` | URL de conexión a MongoDB | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Nombre de la base de datos | `LifeBalance_Auth` |
| `Jwt__SecretKey` | Clave secreta para firmar JWTs (compartida con los otros 3 servicios) | placeholder en dev |
| `Jwt__Issuer` | Emisor del JWT | `LifeBalance` |
| `Jwt__Audience` | Audiencia del JWT | `LifeBalance` |
| `Jwt__AccessTokenExpirationMinutes` | Duración del token de acceso | `30` |
| `Jwt__RefreshTokenExpirationDays` | Duración del token de refresco | `7` |
| `Security__MaxFailedLoginAttempts` | Intentos fallidos antes del bloqueo | `5` |
| `Security__LockoutDurationMinutes` | Duración del bloqueo | `15` |

---

## 🚀 Despliegue en Render

1. Web Service con **Environment: Docker** (Render lee el `Dockerfile` de la raíz).
2. Agrega las variables de entorno de la tabla anterior — **el mismo `Jwt__SecretKey` que los otros 3 servicios**.
3. Puerto del contenedor: `10000` (estándar de Render). Health check: `GET /health`.

---

## 🐳 Docker en Desarrollo Local

```bash
docker-compose up --build
```
Levanta MongoDB + API. La API queda en `http://localhost:10000` y Swagger en `http://localhost:10000/swagger`. (Ejecución con `dotnet run`: `http://localhost:5200` / `https://localhost:7200`.)

---

## 🧪 Testing

```bash
dotnet test tests/UnitTests/UnitTests.csproj --configuration Release
```
~164 tests unitarios verdes.

---

## 📚 Endpoints de la API

### Autenticación (`/api/v1/auth/`)
- `POST /register` - Registrar un nuevo usuario.
- `POST /login` - Iniciar sesión (retorna tokens de acceso y refresco).
- `POST /logout` - Cerrar sesión (revoca tokens).
- `POST /refresh-token` - Obtener un nuevo token de acceso usando el token de refresco.
- `POST /revoke-token` - Revocar un token de refresco manualmente.
- `POST /forgot-password` - Solicitar enlace de recuperación de contraseña.
- `POST /reset-password` - Reestablecer contraseña con un token válido.
- `POST /send-confirmation` - Re-enviar email de confirmación.
- `POST /confirm-email` - Confirmar cuenta de correo electrónico.

### Perfil (`/api/v1/profile/`)
- `GET /me` - Obtener perfil del usuario autenticado.
- `PUT /me` - Actualizar información del perfil.
- `GET /preferences` - Obtener preferencias de interfaz/notificaciones.
- `PUT /preferences` - Actualizar preferencias.
- `PUT /change-password` - Cambiar contraseña actual.

### Roles (`/api/v1/roles/`) — Admin
- `GET /` - Listar roles. · `POST /` - Crear rol. · `PUT /{id}` - Modificar rol. · `DELETE /{id}` - Eliminar rol.

### Permisos (`/api/v1/permissions/`) — Admin
- `GET /` - Listar permisos. · `POST /` - Crear permiso. · `PUT /{id}` - Modificar permiso. · `DELETE /{id}` - Eliminar permiso.

### Auditoría (`/api/v1/audit/`) — Admin
- `GET /login-history` - Historial de inicios de sesión.
- `GET /security-events` - Logs de seguridad y auditoría.

### Healthcheck
- `GET /health` - Estado de la API y conexión a la base de datos.
