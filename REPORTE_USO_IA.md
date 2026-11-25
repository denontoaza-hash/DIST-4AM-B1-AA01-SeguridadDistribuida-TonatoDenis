# INSTITUTO SUPERIOR TECNOLÓGICO CORDILLERA
## Carrera de Desarrollo de Software
### Aplicaciones Distribuidas – Presencial | 4.° Semestre
**Actividad Autónoma:** AA01 – Sistema Distribuido de Autenticación y Seguridad  
**Estudiante:** Denis Omar Tonato  
**Paralelo:** Cuarto A Matutina (4AM)  
**Periodo Académico:** Primer Bimestre – 2026  
**Enlace del Repositorio:** [https://github.com/denontoaza-hash/DIST-4AM-B1-AA01-SeguridadDistribuida-TonatoDenis](https://github.com/denontoaza-hash/DIST-4AM-B1-AA01-SeguridadDistribuida-TonatoDenis)

---

# INFORME DE USO DE INTELIGENCIA ARTIFICIAL, PROMPTS Y FUENTES

En cumplimiento con el **numeral 11 (Entregable 3)** de la guía de la actividad AA01, a continuación se detalla de forma transparente y justificada la herramienta de Inteligencia Artificial utilizada, los propósitos de consulta, los aportes propios del estudiante, los errores corregidos, las verificaciones realizadas y las fuentes oficiales consultadas.

---

### 1. Herramienta de Inteligencia Artificial Utilizada
- **Herramienta:** Asistente de IA Antigravity (Google DeepMind / Modelos Gemini 3.8 Flash).
- **Entorno de trabajo:** Visual Studio Community, SQL Server Management Studio, Postman y Windows PowerShell.

---

### 2. Propósito General del Uso de IA
La herramienta de IA fue utilizada como un soporte de auditoría técnica, consultoría arquitectónica y verificación de calidad de código para:
1. Auditar la coherencia y cumplimiento estricto de los tres microservicios frente a los 16 casos de prueba obligatorios de la rúbrica docente.
2. Identificar posibles excepciones no controladas en .NET 10 durante la comunicación HTTP entre microservicios (específicamente la diferencia entre `OperationCanceledException` y `TimeoutException` de `HttpClient`).
3. Estructurar la colección de pruebas en formato estándar de Postman con scripts de validación (`pm.test`).
4. Generar la plantilla de documentación técnica del archivo `README.md` siguiendo el formato oficial proporcionado por el docente.
5. Configurar las reglas de versionamiento en `.gitignore` para garantizar la exclusión obligatoria de las carpetas de compilación `bin` y `obj`.

---

### 3. Registro Detallado de Solicitudes (Prompts) Realizadas

| N.º | Solicitud realizada (Prompt) | Propósito de la solicitud |
| :---: | :--- | :--- |
| **1** | *"Estoy desarrollando la actividad AA01... revisa completo todo lo que tengo y ayúdame a completar/corregir lo que falte para cumplir con los 16 casos de prueba..."* | Auditoría integral del código fuente, bases de datos y User Secrets frente al documento de requerimientos del Instituto Cordillera. |
| **2** | *"¿Cómo ejecuto cada cosa y pruebo?"* | Conocer la secuencia técnica de despliegue y pruebas independientes de los microservicios en puertos locales. |
| **3** | *"¿El postman vale sin estar los microservicios activos?"* | Clarificación conceptual sobre el funcionamiento de Postman como cliente HTTP frente a las pruebas de tolerancia a fallos (Casos 12 y 13). |
| **4** | *"¿Para qué sirve el script de Postman `pm.test` y `pm.collectionVariables.set`?"* | Comprensión didáctica del funcionamiento de los tests automáticos y captura dinámica de variables de entorno en Postman. |
| **5** | *"Dame los datos para pegar manualmente en cada uno de los 16 casos de prueba."* | Obtención de las URLs absolutas, métodos, encabezados y payloads JSON con los datos reales de prueba (`emily07morocho11@gmail.com`). |
| **6** | *"Explícame las tablas de AuthDB y por qué algunas están vacías."* | Sustentación técnica sobre el esquema de ASP.NET Core Identity y el desacoplamiento de tokens/claims en memoria con JWT. |
| **7** | *"Ayúdame haciendo un README tomando como referencia el del ingeniero."* | Estandarización de la documentación del proyecto respetando la estructura, títulos y orden pedagógico del docente. |
| **8** | *"¿Cómo configuro el versionamiento en GitHub para omitir bin y obj?"* | Implementación de las reglas de exclusión en `.gitignore` y comandos de inicialización de Git. |

---

### 4. Aportes Propios del Estudiante
El desarrollo central, la infraestructura y la validación funcional fueron realizados y gestionados por el estudiante:
1. **Creación y configuración inicial de los tres proyectos**: Arquitectura base creada en ASP.NET Core Web API (.NET 10) con Entity Framework Core.
2. **Configuración de persistencia en SQL Server**: Creación de las tres bases de datos separadas (`AuthDB`, `UsuariosDB`, `NotificacionesDB`) en la instancia `TONATO\SQLEXPRESS`.
3. **Servicio SMTP real con Gmail**: Configuración de credenciales seguras de aplicación en User Secrets para el envío real de correos electrónicos desde `Notificaciones.Api`.
4. **Implementación de modelos y migraciones**: Ejecución de migraciones EF Core (`InitialCreate`) en cada proyecto.
5. **Generación y exportación de scripts SQL**: Extracción manual de los archivos `.sql` independientes en SQL Server Management Studio para su inclusión en la carpeta `SQL SERVER/`.
6. **Ejecución y evidencia de pruebas**: Ejecución interactiva de las pruebas en Postman, pruebas de navegación real con Google OAuth y restablecimiento interactivo de contraseña desde el buzón de Gmail.
7. **Control de versiones**: Vinculación y publicación del repositorio personal en GitHub (`denontoaza-hash`).

---

### 5. Errores Detectados y Corregidos Durante la Auditoría
1. **Tratamiento de Timeout en .NET 10 (`IHttpClientFactory`)**:
   - *Problema:* El código original capturaba únicamente `OperationCanceledException`. En .NET 10, los timeouts de `HttpClient` pueden lanzar `TimeoutException`, la cual no deriva de dicha clase y hubiera provocado un HTTP 500 no controlado.
   - *Corrección:* Se implementó `catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)` para asegurar que el registro ejecute la compensación (*rollback*) y retorne el código HTTP 503 correspondiente.
2. **Simulación de latencia para el Caso de Prueba 14**:
   - *Problema:* No existía un mecanismo determinista para forzar una latencia superior a los 10 segundos configurados en el cliente HTTP.
   - *Corrección:* Se implementó soporte para el parámetro `?simularTimeout=true` y encabezado `X-Simulate-Delay`, permitiendo demostrar en vivo la cancelación de la solicitud y la compensación en base de datos.
3. **Omisión de interfaces mínimas web para Google y Recuperación**:
   - *Problema:* El PDF exigía una interfaz para el botón de Google y la apertura del enlace de recuperación, pero no se habían creado las vistas estáticas.
   - *Corrección:* Se diseñaron `index.html` (Google Identity Services) y `reset-password.html` dentro de `wwwroot` de `Auth.Api`.
4. **Soporte de certificados locales de desarrollo en HttpClient**:
   - *Problema:* Posibles fallos de confianza en llamadas internas HTTPS entre microservicios locales (`localhost:7105` $\rightarrow$ `localhost:7110` / `localhost:7023`).
   - *Corrección:* Se incorporó `DangerousAcceptAnyServerCertificateValidator` en el handler primario de los clientes HTTP de desarrollo.

---

### 6. Verificaciones y Pruebas Realizadas
Se comprobaron de forma satisfactoria los 16 casos de prueba del documento:
- **Casos 1 y 2:** Registro exitoso (`201 Created`) con rol `Cliente` y perfil en `UsuariosDB`; rechazo por correo duplicado (`409 Conflict`).
- **Casos 3 y 4:** Login correcto (`200 OK`) con emisión de JWT; bloqueo tras 5 intentos fallidos (`423 Locked`); rechazo a rutas protegidas sin token (`401 Unauthorized`).
- **Casos 5 y 6:** Control de acceso por propietario y rol (`403 Forbidden` al cliente en perfiles ajenos o rutas admin); modificación administrativa de estado y rol; **protección efectiva del último administrador activo** (`400 Bad Request`).
- **Casos 7 y 8:** Flujo de autenticación con Google validando firma y `sub`; rechazo inmediato a tokens de Google manipulados (`401 Unauthorized`).
- **Casos 9, 10 y 11:** Envío de correo real con enlace seguro; cambio efectivo de clave y renovación de hash PBKDF2; rechazo de tokens vencidos o alterados (`400 Bad Request`).
- **Casos 12, 13 y 14:** Demostración de tolerancia a caídas: apagado de `Usuarios.Api` con compensación automática comprobada en `AuthDB`; apagado de `Notificaciones.Api` sin filtrar tokens; simulación de timeout con respuesta `503 Service Unavailable`.
- **Casos 15 y 16:** Bloqueo de endpoints internos de notificación a clientes no autorizados (`403 Forbidden`); rechazo de cambio de clave cuando la contraseña actual no coincide (`400 Bad Request`).

---

### 7. Referencias y Documentación Oficial Consultada
1. **Google Identity:** *OpenID Connect & Identity Services Authentication*.  
   URL: [https://developers.google.com/identity/openid-connect/openid-connect](https://developers.google.com/identity/openid-connect/openid-connect)
2. **Microsoft Learn:** *Inicio de sesión social y autenticación en ASP.NET Core*.  
   URL: [https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins)
3. **Microsoft Learn:** *Configuración de autenticación con JWT Bearer en ASP.NET Core Web API*.  
   URL: [https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
4. **Microsoft Learn:** *Consumo de servicios HTTP con IHttpClientFactory y gestión de ciclos de vida*.  
   URL: [https://learn.microsoft.com/aspnet/core/fundamentals/http-requests](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests)
5. **Microsoft Learn:** *Personalización y almacenamiento de tokens en ASP.NET Core Identity*.  
   URL: [https://learn.microsoft.com/aspnet/core/security/authentication/identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
