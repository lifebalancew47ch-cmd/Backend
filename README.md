# LifeBalance - Backend

¡Bienvenido al repositorio central del backend de **LifeBalance**! 🚀

Este repositorio contiene la arquitectura de microservicios backend que impulsa la plataforma **LifeBalance**. La solución está construida utilizando el ecosistema **.NET** (C#), aplicando principios de **Clean Architecture**, **Domain-Driven Design (DDD)** y **CQRS** (Command Query Responsibility Segregation) para asegurar un código mantenible, escalable y robusto.

---

## 🏛️ Arquitectura Global

El backend de LifeBalance está estructurado como un conjunto de microservicios independientes que se comunican entre sí. Cada microservicio cuenta con su propia base de datos (Database-per-service) garantizando un bajo acoplamiento.

### Principales Tecnologías Utilizadas
* **Framework:** .NET (ASP.NET Core)
* **Arquitectura:** Clean Architecture, DDD, CQRS (con MediatR)
* **Base de Datos:** MongoDB (NoSQL)
* **Caché:** Redis (usado en microservicios clave como Organization & SaaS)
* **Autenticación y Seguridad:** JWT (JSON Web Tokens), BCrypt, Políticas de Seguridad de OWASP
* **Resiliencia HTTP:** Polly (Retries con Exponential Backoff, Circuit Breaker)
* **Contenerización y Despliegue:** Docker, Docker Compose, configuración lista para **Render** (`render.yaml`)

---

## 📦 Microservicios Disponibles

Actualmente, el repositorio cuenta con 4 microservicios principales:

### 1. 🔐 Auth & Profile (`/Auth_Profile`)
Microservicio robusto encargado de la **Autenticación, Autorización y Gestión de Perfiles**.
* **Responsabilidades:** Login, registro, rotación de refresh tokens, gestión de contraseñas, auditorías de acceso, y perfiles de usuario.
* **Seguridad:** Autenticación JWT Bearer, RBAC/PBAC, control de fuerza bruta (bloqueos temporales), y protección contra NoSQL Injection.
* **Base de Datos:** `LifeBalance_Auth` (MongoDB).

### 2. 📊 Dashboard Service (`/DashboardService`)
Microservicio de agregación que sirve como punto de entrada para las interfaces de usuario.
* **Responsabilidades:** Recopilación y orquestación de datos provenientes de otros microservicios (Médico, Sedentarismo, Gamificación, Machine Learning, etc.) para renderizar el Dashboard del usuario.
* **Integraciones:** Se comunica con el resto del ecosistema mediante clientes HTTP tipados.
* **Base de Datos:** `LifeBalanceDashboard` (MongoDB).

### 3. 🏢 Organization & SaaS (`/OrganizationAndSaaS`)
Núcleo empresarial (B2B/B2C) de la plataforma.
* **Responsabilidades:** Gestión multi-tenant de Empresas, Familias, Departamentos y Equipos. Administración de licencias, invitaciones y planes SaaS (Free, Personal, Family, Business, Enterprise).
* **Aislamiento Multi-Tenant:** Aislamiento estricto de datos por `TenantId` para prevenir fugas de información.
* **Base de Datos:** `LifeBalance_OrganizationSaaS` (MongoDB) y Redis Cache.

### 4. 🔔 Notifications & Alerts (`/NotificationsAndAlerts`)
API encargada de la gestión de alertas y notificaciones del sistema.
* **Responsabilidades:** Despacho de notificaciones a los usuarios (alertas del sistema, expiración de licencias, invitaciones, correos de confirmación, etc.).
* **Base de Datos:** `LifeBalanceNotificationsDb` (MongoDB).

---

## 🚀 Entorno de Desarrollo y Despliegue

La solución está completamente Dockerizada para un despliegue y desarrollo simplificado.

### Levantar el entorno local (Docker Compose)
Cada microservicio incluye su propio `docker-compose.yml` local. Para levantar los microservicios individualmente, navega al directorio del servicio y ejecuta:
```bash
docker-compose up --build -d
```

### Despliegue en Render
El proyecto incluye un archivo `render.yaml` en la raíz que orquesta el despliegue automático de los 4 microservicios en la plataforma **Render.com** utilizando contenedores Docker.
* **Environment:** Docker
* **Región:** Oregon
* **Plan:** Free

Para desplegar, simplemente conecta el repositorio en el Dashboard de Render usando la opción **Blueprint** y Render configurará todos los web services con las variables de entorno base. **Asegúrate de inyectar las variables sensibles manualmente** (como Connection Strings de MongoDB Atlas, `Jwt__SecretKey` y credenciales SMTP).

---

## 🛡️ Seguridad

Todo el proyecto sigue prácticas seguras recomendadas por OWASP:
1. **Validaciones estrictas:** Implementadas en la capa de Aplicación utilizando *FluentValidation*.
2. **Cabeceras de Seguridad HTTP:** `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, etc.
3. **Manejo global de excepciones:** Respuestas estandarizadas usando el estándar RFC 7807 (`ProblemDetails`).
4. **Rate Limiting:** Límites configurados a nivel global e individual por IP/Tenant para proteger los endpoints públicos.

---

## 📝 Documentación Adicional
Para ver detalles profundos sobre endpoints, configuración y diagramas específicos de cada microservicio, dirígete al archivo `README.md` localizado en cada una de sus carpetas:
* [`/Auth_Profile/README.md`](./Auth_Profile/README.md)
* [`/DashboardService/README.md`](./DashboardService/README.md)
* [`/OrganizationAndSaaS/README.md`](./OrganizationAndSaaS/README.md)
* `/NotificationsAndAlerts` (Consultar la estructura interna)

---
*Propietario — © LifeBalance 2026. Todos los derechos reservados.*