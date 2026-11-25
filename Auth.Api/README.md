# Arquitectura de Microservicios – Autenticación y Seguridad Distribuida (AA01)

## Descripción

Proyecto desarrollado para la asignatura **Aplicaciones Distribuidas**, correspondiente a la actividad autónoma **AA01**.

El proyecto implementa un módulo distribuido de autenticación, seguridad y gestión de usuarios para un **Sistema Distribuido de Gestión Comercial** (comercializadora de productos tecnológicos y electrónicos en el Ecuador). 

La solución está construida bajo una arquitectura de microservicios REST independientes utilizando **ASP.NET Core Web API (.NET 10)**, **SQL Server**, **Entity Framework Core**, **ASP.NET Core Identity**, **JWT (JSON Web Tokens)** e **IHttpClientFactory**.

El sistema permite el registro de credenciales con rol predeterminado **Cliente**, creación coordinada del perfil de usuario, autenticación externa con **Google (OpenID Connect)**, emisión y validación estricta de tokens JWT, recuperación segura de contraseñas mediante **correo electrónico real (SMTP)**, control de acceso por propietario y roles (**401 / 403**), protección del último administrador activo y tolerancia a fallos con **compensación automática (*rollback*)** y control de **Timeout**.

Cada microservicio cuenta con su propia base de datos y responsabilidades independientes, comunicándose mediante solicitudes HTTP/JSON seguras sin compartir `DbContext` ni claves foráneas.

---

## Tecnologías utilizadas

- C#
- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Identity (PBKDF2)
- Entity Framework Core
- SQL Server
- JWT (JSON Web Tokens)
- Google Identity / OpenID Connect (`Google.Apis.Auth`)
- IHttpClientFactory (HTTP/JSON con BaseAddress y Timeout explícitos)
- SMTP (Envío de correos reales a través de Gmail)
- Swagger / OpenAPI
- Postman
- Visual Studio Community

---

## Estructura del repositorio

```text
Aplicativos Distribuidos/
│
├── Auth.Api/
│   ├── Auth.Api/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── DTOs/
│   │   ├── Migrations/
│   │   ├── Models/
│   │   ├── Properties/
│   │   ├── wwwroot/
│   │   │   ├── index.html
│   │   │   └── reset-password.html
│   │   ├── appsettings.json
│   │   └── Program.cs
│   └── Auth.Api.slnx
│
├── Usuarios.Api/
│   ├── Usuarios.Api/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── DTOs/
│   │   ├── Migrations/
│   │   ├── Models/
│   │   ├── Properties/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   └── Usuarios.Api.slnx
│
├── Notificaciones.Api/
│   ├── Notificaciones.Api/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── DTOs/
│   │   ├── Migrations/
│   │   ├── Models/
│   │   ├── Properties/
│   │   ├── Services/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   └── Notificaciones.Api.slnx
│
├── SQL SERVER/
│   ├── AuthDB.sql
│   ├── NotificacionesDB.sql
│   └── UsuariosDB.sql
│
├── POSTMAN/
│   └── Sistema Distribuido de Seguridad y Autenticación.postman_collection.json
│
└── README.md
```

---

## Instrucciones de uso

### 1. Crear las bases de datos

Abrir **SQL Server Management Studio (SSMS)** y conectarse a la instancia local de SQL Server.

Ejecutar los scripts ubicados en la carpeta `SQL SERVER/`:

- `AuthDB.sql`: Crea la base de datos `AuthDB`, tablas de ASP.NET Core Identity, roles iniciales (`Cliente`, `Administrador`) y usuario semilla.
- `UsuariosDB.sql`: Crea la base de datos `UsuariosDB` y la tabla `Perfiles`.
- `NotificacionesDB.sql`: Crea la base de datos `NotificacionesDB` y la tabla `EnviosCorreo`.

Cada microservicio mantiene su persistencia totalmente separada para garantizar la independencia de la información.

### 2. Configurar las conexiones y secretos

Los secretos sensibles (claves JWT, credenciales SMTP y credenciales de Google) se encuentran gestionados de forma segura mediante **User Secrets** para evitar exponerlos en el repositorio de código.

Cadenas de conexión configuradas:

```json
{
  "ConnectionStrings": {
    "AuthConnection": "Server=SERVIDOR\\SQLEXPRESS;Database=AuthDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Bases de datos asignadas:
- **Auth.Api**: `AuthDB`
- **Usuarios.Api**: `UsuariosDB`
- **Notificaciones.Api**: `NotificacionesDB`

*Nota: La instancia del servidor SQL (`Server=...`) puede variar según el equipo de ejecución.*

### 3. Abrir los proyectos

Abrir los proyectos en **Visual Studio Community** navegando a sus carpetas correspondientes:

- `Auth.Api/Auth.Api.slnx`
- `Usuarios.Api/Usuarios.Api.slnx`
- `Notificaciones.Api/Notificaciones.Api.slnx`

Esperar a que Visual Studio restaure los paquetes NuGet de cada microservicio.

### 4. Ejecutar los microservicios

Ejecutar los tres proyectos ASP.NET Core Web API en simultáneo.

Los puertos configurados son:

| Microservicio | URL HTTPS | URL HTTP | Swagger |
| :--- | :--- | :--- | :--- |
| **Auth.Api** | `https://localhost:7105` | `http://localhost:5195` | `https://localhost:7105/swagger` |
| **Usuarios.Api** | `https://localhost:7110` | `http://localhost:5222` | `https://localhost:7110/swagger` |
| **Notificaciones.Api** | `https://localhost:7023` | `http://localhost:5162` | `https://localhost:7023/swagger` |

Cada servicio dispone de su documentación y pruebas mediante Swagger en entorno de desarrollo.

### 5. Importar la colección de Postman

Abrir Postman y seleccionar la opción:

**Import**

Importar el archivo ubicado en:

`POSTMAN/Sistema Distribuido de Seguridad y Autenticación.postman_collection.json`

La colección contiene las peticiones HTTP y scripts de prueba automáticos para validar los **16 casos de prueba obligatorios** de la actividad.

### 6. Ejecutar y comprobar el funcionamiento

**URL base de los servicios:**

- `Auth.Api`: `https://localhost:7105`
- `Usuarios.Api`: `https://localhost:7110`
- `Notificaciones.Api`: `https://localhost:7023`

**Métodos disponibles en los microservicios:**

#### Auth.Api
- `POST /api/Auth/registro`: Registro de cuenta, asignación de rol Cliente, llamada distribuida a Usuarios.Api y Notificaciones.Api con compensación.
- `POST /api/Auth/login`: Autenticación local, verificación de contraseña (PBKDF2), control de bloqueo y emisión de JWT.
- `POST /api/Auth/google`: Autenticación con Google (OpenID Connect), validación de ID Token por `sub` y emisión de JWT propio.
- `POST /api/Auth/recuperar-password`: Emisión de token temporal de recuperación y envío de enlace a correo real.
- `POST /api/Auth/restablecer-password`: Restablecimiento de contraseña con token válido y rechazo de tokens vencidos/reutilizados.
- `POST /api/Auth/cambiar-password`: Cambio de contraseña autenticado validando contraseña actual.
- `GET /api/Auth/usuarios`: Consulta administrativa de usuarios registrados (Solo Administrador).
- `PUT /api/Auth/usuarios/{id}/rol`: Asignación y cambio de roles (Protege último administrador activo).
- `PUT /api/Auth/usuarios/{id}/estado`: Habilitación y deshabilitación de cuentas (Protege último administrador activo).

#### Usuarios.Api
- `POST /api/Perfiles`: Creación de perfil vinculada por `UsuarioId` (Consumido por Auth.Api con token de servicio).
- `GET /api/Perfiles/mi-perfil`: Consulta del perfil propio con JWT válido (Control por propietario).
- `GET /api/Perfiles/{usuarioId}`: Consulta de perfil específico (Solo propietario o Administrador).
- `GET /api/Perfiles`: Listado general de perfiles (Solo Administrador).
- `PUT /api/Perfiles/{usuarioId}`: Modificación de datos personales propios (Envía aviso opcional a Notificaciones.Api).

#### Notificaciones.Api
- `POST /api/Notificaciones/enviar`: Envío real de correo electrónico mediante SMTP y registro en `EnviosCorreo` (Protegido para servicios internos y Administrador).
- `GET /api/Notificaciones/historial`: Consulta del historial de envíos sin exponer contraseñas ni tokens (Solo Administrador).

---

## Comunicación entre microservicios

El sistema implementa comunicación sincrónica mediante `IHttpClientFactory` con manejo explícito de Timeout (10 segundos) y cabeceras de autorización Bearer internas:

1. **Flujo de Registro (Auth.Api $\rightarrow$ Usuarios.Api $\rightarrow$ Notificaciones.Api)**:
   - `Auth.Api` crea la identidad en `AuthDB` (inactiva).
   - `Auth.Api` envía solicitud HTTP `POST` a `Usuarios.Api/api/Perfiles`.
   - Si `Usuarios.Api` responde exitoso, `Auth.Api` activa la cuenta y solicita a `Notificaciones.Api/api/Notificaciones/enviar` el correo de bienvenida.
   - **Compensación / Rollback**: Si `Usuarios.Api` se cae o falla, `Auth.Api` ejecuta compensación eliminando la identidad para evitar cuentas incompletas.

2. **Flujo de Recuperación (Auth.Api $\rightarrow$ Notificaciones.Api)**:
   - `Auth.Api` genera el token seguro de un solo uso y llama por HTTP a `Notificaciones.Api`.
   - `Notificaciones.Api` despacha el correo a la bandeja real de Gmail con el enlace funcional.

3. **Flujo de Actualización (Usuarios.Api $\rightarrow$ Notificaciones.Api)**:
   - Al modificar el perfil, `Usuarios.Api` envía aviso de modificación mediante `Notificaciones.Api`.

---

## Interfaces Web de Demostración

`Auth.Api` sirve dos interfaces HTML mínimas para pruebas desde el navegador:

1. **Ingreso con Google**: `https://localhost:7105/index.html`  
   Permite presionar el botón oficial de Google, validar el ID Token con el backend y visualizar en pantalla el JWT local emitido con el rol `Cliente`.
2. **Restablecimiento de Contraseña**: `https://localhost:7105/reset-password.html`  
   Recibe el enlace del correo real con los parámetros `?email=...&token=...` y permite ingresar la nueva clave de forma interactiva.

---

## Validaciones y pruebas (16 Casos Obligatorios)

Se realizaron pruebas mediante Postman y navegador para comprobar:

- **Caso 1**: Registro de Cliente válido con persistencia en `AuthDB` y `UsuariosDB` (`201 Created`).
- **Caso 2**: Rechazo por correo duplicado (`409 Conflict`).
- **Caso 3**: Login local correcto (`200 OK`) y erróneo (`401 Unauthorized`), con bloqueo temporal por 5 fallos repetidos (`423 Locked`).
- **Caso 4**: Consulta de perfil sin token o con token vencido (`401 Unauthorized`).
- **Caso 5**: Control de acceso por propietario y roles; rechazo de consulta de perfiles ajenos o rutas administrativas (`403 Forbidden`).
- **Caso 6**: Modificación de roles y estados por el Administrador, con **protección del último administrador activo** (`400 Bad Request`).
- **Caso 7**: Inicio de sesión real con Google validando firma y asociando por `sub` con rol `Cliente` y JWT local.
- **Caso 8**: Rechazo criptográfico de ID Token de Google alterado o inválido (`401 Unauthorized`).
- **Caso 9**: Solicitud de recuperación con respuesta genérica segura y entrega de correo real en buzón de Gmail.
- **Caso 10**: Restablecimiento exitoso con token válido y actualización de hash PBKDF2.
- **Caso 11**: Rechazo de tokens de recuperación vencidos, alterados o reutilizados (`400 Bad Request`).
- **Caso 12**: Tolerancia a caídas: apagado de `Usuarios.Api` durante registro con error controlado (`503 Service Unavailable`) y compensación comprobada.
- **Caso 13**: Tolerancia a caídas: apagado de `Notificaciones.Api` durante recuperación con error controlado (`503 Service Unavailable`) sin divulgar tokens.
- **Caso 14**: Simulación de latencia superior al Timeout (`POST /api/Auth/registro?simularTimeout=true`), cancelación a los 10 segundos, rollback y respuesta `503 Service Unavailable`.
- **Caso 15**: Bloqueo de endpoints internos de `Notificaciones.Api` al consumirse sin autorización (`401`) o con rol Cliente (`403`).
- **Caso 16**: Cambio de contraseña rechazado si la contraseña actual es incorrecta (`400 Bad Request`).

---

## Orden de ejecución

Para ejecutar correctamente el proyecto se recomienda seguir este orden:

Iniciar SQL Server

↓

Ejecutar los scripts de `SQL SERVER/` (`AuthDB.sql`, `UsuariosDB.sql`, `NotificacionesDB.sql`)

↓

Abrir las soluciones en Visual Studio Community

↓

Ejecutar `Notificaciones.Api` (Puerto 7023)

↓

Ejecutar `Usuarios.Api` (Puerto 7110)

↓

Ejecutar `Auth.Api` (Puerto 7105)

↓

Verificar los puertos y páginas Swagger de cada servicio

↓

Abrir Postman

↓

Importar la colección desde `POSTMAN/Sistema Distribuido de Seguridad y Autenticación.postman_collection.json`

↓

Probar y comprobar el funcionamiento de las operaciones de los tres microservicios (Casos 1 al 16).

---

## Autor

**Estudiante:** Denis Tonato  
**Asignatura:** Aplicaciones Distribuidas  
**Paralelo:** Cuarto A Matutina  
**Tema:** AA01 - Sistema Distribuido de Autenticación y Seguridad  
**Institución:** Instituto Superior Tecnológico Cordillera  
