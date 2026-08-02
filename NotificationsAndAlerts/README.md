# LifeBalance - Notifications & Alerts Microservice 🔔

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD%20%2B%20CQRS-blue)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?logo=mongodb)
![Firebase](https://img.shields.io/badge/Push-FirebaseAdmin-FFCA28?logo=firebase)

El **Microservicio de Notificaciones y Alertas** forma parte central del ecosistema **LifeBalance**. Es responsable de la distribución, programación y gestión de todas las notificaciones (Push, Email, in-app) para los usuarios y organizaciones de la plataforma.

---

## 🏛️ Arquitectura

El proyecto está diseñado bajo los principios de **Clean Architecture**, **Domain-Driven Design (DDD)** y **CQRS**:

1. **LifeBalance.Notifications.Domain**: Entidades núcleo (Notificaciones, Alertas, Preferencias, Plantillas), Value Objects y reglas de negocio.
2. **LifeBalance.Notifications.Application**: Casos de uso (Commands/Queries), manejadores, validaciones y mapeos de DTOs.
3. **LifeBalance.Notifications.Infrastructure**: Integraciones externas (MongoDB, FirebaseAdmin para Push Notifications, servicios SMTP/Email).
4. **LifeBalance.Notifications.Presentation**: Entry point de la API REST, configuración de Swagger/OpenAPI y middlewares.
5. **LifeBalance.Notifications.Shared**: Elementos y utilidades transversales compartidos.

---

## 🚀 Tecnologías Clave

- **.NET 9.0**: Framework base para alto rendimiento.
- **MongoDB Driver**: Persistencia de logs de notificaciones y configuración.
- **FirebaseAdmin**: Envío de notificaciones Push a dispositivos móviles (FCM).
- **Autenticación JWT Bearer**: Seguridad en los endpoints de la API.
- **Swagger / OpenAPI**: Documentación y pruebas interactivas de la API.

---

## ⚙️ Configuración y Despliegue

### Requisitos Previos
- .NET 9.0 SDK
- MongoDB (local o Atlas)
- Credenciales de Firebase Admin (archivo JSON de cuenta de servicio)
- Docker & Docker Compose (opcional)

### Variables de Entorno

Asegúrate de configurar las siguientes variables (vía `appsettings.json`, variables de entorno o Secret Manager):

| Variable | Descripción |
|---|---|
| `MongoDb__ConnectionString` | URL de conexión a MongoDB |
| `MongoDb__DatabaseName` | Nombre de la base de datos (ej. `LifeBalance_Notifications`) |
| `Firebase__ServiceAccountKeyPath` | Ruta o contenido del JSON de credenciales de Firebase |
| `Jwt__SecretKey` | Clave secreta para la validación de tokens JWT |

### Ejecución Local

```bash
# Restaurar y compilar
dotnet build

# Ejecutar el proyecto Presentation (API)
dotnet run --project src/LifeBalance.Notifications.Presentation
```

### Ejecutar con Docker

```bash
docker-compose up -d --build
```
El servicio estará disponible en el puerto especificado en la configuración, y la documentación de Swagger en `/swagger`.

---

## 🧪 Testing

El proyecto cuenta con pruebas automatizadas que aseguran la fiabilidad del servicio:

```bash
# Pruebas Unitarias
dotnet test tests/LifeBalance.Notifications.UnitTests

# Pruebas de Integración
dotnet test tests/LifeBalance.Notifications.IntegrationTests
```