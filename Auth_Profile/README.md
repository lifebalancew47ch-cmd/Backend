# LifeBalance - Auth & Profile Microservice

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD%20%2B%20CQRS-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Security](https://img.shields.io/badge/Security-JWT%20%7C%20RBAC-red)

Microservicio robusto de Autenticación, Autorización y Gestión de Perfiles para la plataforma **LifeBalance**. Construido sobre **.NET 9.0** y diseñado siguiendo los principios de **Clean Architecture**, **DDD (Domain-Driven Design)** y el patrón **CQRS** con MediatR.

---

## 🏛️ Arquitectura del Sistema
El microservicio está estructurado en 4 capas principales:
1. **Auth.Domain**: Entidades de dominio, enumeraciones y reglas de negocio puras (sin dependencias externas).
2. **Auth.Application**: Comandos, consultas, manejadores (handlers), DTOs, validaciones con FluentValidation y mapeos.
3. **Auth.Infrastructure**: Implementación de repositorios MongoDB, servicios criptográficos, JWT, logs de auditoría y middlewares (excepciones globales, cabeceras de seguridad y logging de solicitudes).
4. **Auth.Api**: Controladores REST con control de versiones de API, configuración de Swagger, CORS y Rate Limiting.

---

## 🔒 Medidas de Seguridad Implementadas
Este servicio cumple con estrictas medidas de seguridad preparadas para producción:
- **Autenticación JWT Bearer:** Tokens firmados digitalmente.
- **Rotación y Revocación de Refresh Tokens:** Almacenamiento seguro e invalidación inmediata al cerrar sesión o revocar de manera proactiva.
- **BCrypt para Contraseñas:** Hash seguro unidireccional (nunca se almacena en texto plano).
- **Autorización Basada en Roles y Políticas (RBAC / PBAC):** Restricciones de seguridad por endpoint.
- **Cabeceras de Seguridad HTTP:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`, `X-XSS-Protection` y HSTS/Strict-Transport-Security.
- **Rate Limiting:** Límite de solicitudes configurable a nivel global y específico (ej. `/auth/login`, `/auth/register`).
- **Validación Estricta:** Validaciones de entrada en cada comando con FluentValidation (Emails, Passwords fuertes, UUIDs, longitudes máximas).
- **Protección contra NoSQL Injection & Mass Assignment:** Uso seguro del driver oficial de MongoDB y separación estricta mediante DTOs.
- **Bloqueo de Cuenta por Fuerza Bruta:** Cuenta temporalmente bloqueada tras múltiples intentos fallidos (configurable).
- **Auditoría Completa:** Registro completo de eventos de seguridad (Login, Login Fallido, Cambio de Contraseña, Lockouts) en base de datos.
- **Manejo Global de Errores:** Retorno estandarizado de excepciones utilizando `ProblemDetails` sin exponer el StackTrace en entornos no seguros.

---

## 📂 Colecciones de MongoDB e Índices

Las siguientes colecciones y sus respectivos índices son inicializados automáticamente por la aplicación al iniciar:

- **`users`**
  - Índice Único: `Email`
  - Índice Único: `Username`
  - Índice: `CreatedAt`
- **`refresh_tokens`**
  - Índice Único: `Token`
  - Índice: `UserId`
  - Índice: `ExpiresAt`
  - Índice: `CreatedAt`
- **`roles`**
  - Índice Único: `NormalizedName`
- **`permissions`**
  - Índice Único: `NormalizedName`
- **`user_preferences`**
  - Índice: `UserId`
- **`audit_logs`**
  - Índice: `UserId`, `Action`, `CreatedAt`
- **`login_history`**
  - Índice: `UserId`, `CreatedAt`
- **`password_reset_tokens`**
  - Índice Único: `Token`
  - Índice: `UserId`
  - Índice: `CreatedAt`
- **`email_confirmation_tokens`**
  - Índice Único: `Token`
  - Índice: `UserId`
  - Índice: `CreatedAt`

---

## 🛠️ Variables de Entorno y Configuración
El microservicio lee las configuraciones a través del *Options Pattern*. En producción, estas se definen mediante variables de entorno (por ejemplo en **Render**):

| Variable de Entorno | Descripción | Valor por Defecto |
|---|---|---|
| `MongoDb__ConnectionString` | URL de conexión a la base de datos | `mongodb://localhost:27017` |
| `MongoDb__DatabaseName` | Nombre de la base de datos MongoDB | `LifeBalance_Auth` |
| `Jwt__SecretKey` | Clave secreta para firmar los JWTs | *(Definir secreto seguro de 32 bytes)* |
| `Jwt__Issuer` | Emisor del JWT | `LifeBalance` |
| `Jwt__Audience` | Audiencia del JWT | `LifeBalance` |
| `Jwt__AccessTokenExpirationMinutes` | Duración del token de acceso | `30` |
| `Jwt__RefreshTokenExpirationDays` | Duración del token de refresco | `7` |
| `Security__MaxFailedLoginAttempts` | Intentos fallidos permitidos antes de bloqueo | `5` |
| `Security__LockoutDurationMinutes` | Duración del bloqueo en minutos | `15` |

---

## 🚀 Despliegue en Render

Para desplegar este microservicio en **Render**:

1. Crea un **Web Service** en Render.
2. Vincula tu repositorio de GitHub.
3. Selecciona el **Environment** como **Docker**.
4. Render leerá automáticamente el archivo `Dockerfile` del raíz para construir la imagen.
5. Agrega las **Variables de Entorno** requeridas descritas en la sección anterior (sobre todo la clave secreta de producción y el string de conexión de MongoDB Atlas).
6. El puerto expuesto por el contenedor es el `10000` (el puerto estándar de Render).

---

## 🐳 Uso de Docker en Desarrollo Local

Levanta la base de datos MongoDB y la API de manera local con un solo comando:

```bash
docker-compose up --build
```

La API estará accesible localmente en `http://localhost:10000` y la documentación Swagger se ubicará en `http://localhost:10000/swagger`.

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

### Roles (`/api/v1/roles/`)
- `GET /` - Listar roles (Admin).
- `POST /` - Crear rol (Admin).
- `PUT /{id}` - Modificar rol (Admin).
- `DELETE /{id}` - Eliminar rol (Admin).

### Permisos (`/api/v1/permissions/`)
- `GET /` - Listar permisos (Admin).
- `POST /` - Crear permiso (Admin).
- `PUT /{id}` - Modificar permiso (Admin).
- `DELETE /{id}` - Eliminar permiso (Admin).

### Auditoría (`/api/v1/audit/`)
- `GET /login-history` - Obtener historial de inicios de sesión (Admin).
- `GET /security-events` - Obtener logs de seguridad y auditoría (Admin).

### Healthcheck
- `GET /health` - Estado de salud de la API y conexión a la base de datos.
