# FASE 2 — Auditoría general de Solqaryn / SOLQARYN

Fecha de cierre técnico: 27 de julio de 2026.

Rama auditada y modificada: `Desarrollo`.

Producción congelada: `main`, variables, dominios, servicios, bases, credenciales, activos y despliegues productivos no fueron modificados.

## 1. Alcance auditado

Se revisaron:

- configuración del backend y frontend;
- Docker y configuración declarativa de Render/Vercel;
- conexión MySQL y estrategia de migraciones;
- autenticación JWT y renovación de sesión;
- permisos y alcance por usuario;
- CORS, host filtering, proxy inverso y encabezados HTTP;
- endpoints públicos y de disponibilidad;
- almacenamiento Cloudinary;
- envío SMTP;
- dependencias .NET y npm;
- colas, tareas programadas y servicios en segundo plano;
- logs, auditoría y observabilidad;
- dominios, HTTPS y separación de entornos.

## 2. Entornos oficiales y datos

Solo se reconocen dos entornos lógicos:

```text
SOLQARYN_producción (Producción)
SOLQARYN_desarrollo
```

La base confirmada para Desarrollo es `SOLQARYN_desarrollo`. La aplicación de Desarrollo no debe utilizar `defaultdb`, una base sin destino claro ni el usuario administrativo `avnadmin`.

Las migraciones automáticas permanecen deshabilitadas:

```text
Database__ApplyMigrationsOnStartup=false
```

No se ejecutó ninguna migración contra Producción. El SQL forward continúa siendo evidencia revisable y no un mecanismo de ejecución automática.

## 3. Hallazgos y correcciones aplicadas

### 3.1 Intentos ilimitados de inicio de sesión — corregido

Hallazgo: el endpoint `POST /auth/login` no tenía limitación de solicitudes.

Corrección:

- política fija por dirección IP;
- límite configurable mediante `Security__LoginRateLimitPerMinute`;
- Desarrollo usa 20 intentos por minuto;
- respuesta HTTP 429 con mensaje sanitizado;
- las pruebas aisladas elevan el límite para no interferir con la matriz E2E.

### 3.2 Configuración JWT sin validación temprana — corregido

Hallazgo: un secreto placeholder o demasiado corto podía llegar al registro de autenticación.

Corrección:

- el proceso falla al iniciar si `Jwt:Secret` contiene `CHANGE_ME`;
- se exige un mínimo de 32 bytes;
- `Jwt:Issuer` y `Jwt:Audience` son obligatorios;
- se mantienen validación de firma, issuer, audience, vida útil y `ClockSkew=0`.

### 3.3 Encabezados reenviados sin límite — corregido

Hallazgo: el backend aceptaba encabezados de proxy sin limitar la cantidad de saltos.

Corrección:

- `ForwardLimit=1`;
- se conserva compatibilidad con el proxy de Render;
- la dirección resuelta se utiliza para la política de rate limiting.

### 3.4 Encabezados defensivos ausentes — corregido

Se añadieron:

- `Strict-Transport-Security` fuera de Development;
- `X-Content-Type-Options: nosniff`;
- `X-Frame-Options: DENY`;
- `Referrer-Policy: no-referrer`;
- `Permissions-Policy` para bloquear cámara, micrófono y geolocalización en la API.

### 3.5 Health check sin verificación de base — corregido

Se separaron:

- `/health`: liveness del proceso, sin exponer nombre del ambiente ni hora del servidor;
- `/health/ready`: disponibilidad real de MySQL mediante `CanConnectAsync`, con HTTP 503 cuando la base no está disponible.

El workflow integral exige que ambos endpoints respondan correctamente antes de iniciar Playwright.

### 3.6 Contenedor ejecutado como root — corregido

El runtime ahora utiliza el usuario no privilegiado incorporado en la imagen oficial .NET 8:

```dockerfile
USER $APP_UID
```

La imagen continúa compilándose en GitHub Actions.

### 3.7 Host filtering de Render Desarrollo — corregido

Render Desarrollo declara:

```text
AllowedHosts=solqaryn-api-desarrollo.onrender.com
```

Esto evita aceptar hosts arbitrarios en el servicio autorizado de Desarrollo.

### 3.8 Dependencia visual de Producción — corregido

Hallazgo: Render Desarrollo obtenía el logo desde `SOLQARYN.vercel.app`.

Corrección:

```text
https://solqaryn-desarrollo.vercel.app/assets/SOLQARYN-logo.png
```

Desarrollo ya no depende del frontend productivo para generar documentos o correos.

### 3.9 Vulnerabilidad crítica transitiva .NET — corregido

La auditoría detectó:

```text
System.Text.Encodings.Web 4.5.0
Severidad: Critical
GHSA-ghhp-997w-qr28
```

El paquete llegaba al proyecto de pruebas mediante una referencia obsoleta a `Microsoft.AspNetCore.Http.Abstractions 2.2.0` dentro de una solución .NET 8.

Corrección:

- se eliminó la referencia ASP.NET 2.2;
- el proyecto conserva `Microsoft.AspNetCore.App` de .NET 8;
- la segunda auditoría .NET terminó sin paquetes vulnerables.

### 3.10 Dependencias npm — aprobadas

`npm audit --omit=dev --audit-level=high` no detectó vulnerabilidades altas o críticas en dependencias productivas.

## 4. Resultado de la auditoría por área

### Configuración y secretos

- No existen secretos reales versionados en `appsettings.json`.
- Los valores sensibles de Render usan `sync: false` o generación segura.
- `Swagger__Enabled=false` en Desarrollo desplegado.
- Migraciones automáticas deshabilitadas.
- Producción y Desarrollo conservan variables independientes.

### Base de datos

- Desarrollo usa la base `SOLQARYN_desarrollo`.
- EF Core se prueba con MySQL 8.4 descartable.
- El modelo y snapshot se verifican.
- El SQL forward se analiza para bloquear `DROP TABLE`, `TRUNCATE` y `DELETE FROM`.
- No se ejecutó SQL sobre Producción.

### Autenticación y autorización

- JWT valida firma, issuer, audience y expiración.
- La renovación requiere token válido y usuario activo.
- El login está limitado por IP.
- Los permisos se resuelven por acción.
- Vendedores y usuarios no administradores se limitan por `UsuarioId`.
- La matriz E2E verifica respuestas 403 y ocultación de recursos ajenos.

### API y CORS

- CORS usa una lista explícita.
- Vercel consume la API mediante proxy same-origin `/api`.
- Errores internos se devuelven con mensajes sanitizados.
- Swagger permanece cerrado fuera del ambiente autorizado.
- Se añadieron headers defensivos y host filtering.

### Cloudinary

- Clave autorizada de Desarrollo: `SOLQARYN_desarrollo`.
- Prefijo obligatorio: `SOLQARYN_desarrollo/`.
- El backend bloquea eliminaciones de `PublicId` ajenos al prefijo.
- Producción mantiene sus rutas actuales.
- `avnadmin`, claves Raíz, moderación y flujos de medios se conservan.

### SMTP

- Credenciales fuera del repositorio.
- Direcciones, encabezados, nombres de archivo y adjuntos se validan.
- Correos se enmascaran en logs.
- Errores técnicos no se envían al navegador.
- La entrega real y los reintentos se resolverán en la Fase 7.

### Colas y tareas programadas

No se encontraron Hangfire, Quartz, cron interno, `BackgroundService`, `IHostedService` ni una cola persistente registrados en la composición actual.

El correo es síncrono y no tiene cola de reintentos. Esto no se cambia en la Fase 2 porque pertenece al alcance de la Fase 7.

### Logs y observabilidad

- Existe logging estructurado en ASP.NET Core.
- Existe auditoría de acciones, sesiones y errores de negocio.
- SMTP enmascara destinatarios.
- No existe todavía agregación centralizada, trazas distribuidas, métricas de aplicación ni alertas automáticas propias.

### Dominios y certificados

- Vercel Desarrollo: `solqaryn-desarrollo.vercel.app`.
- Render Desarrollo: `solqaryn-api-desarrollo.onrender.com`.
- CORS y rewrites apuntan al ambiente de Desarrollo.
- TLS y certificados son administrados por Vercel y Render.
- No se modificó ningún dominio o certificado productivo.

## 5. Riesgos residuales documentados

### Token en localStorage

El frontend conserva el JWT en `localStorage`. Esto es funcional y está cubierto por expiración y cierre por inactividad, pero un XSS exitoso podría leerlo.

Una migración a cookies `HttpOnly`, `Secure` y `SameSite` requeriría rediseñar autenticación, CSRF, CORS y renovación. Se documenta como mejora futura y no se implementa automáticamente.

### Cloudinary compartido

Desarrollo y Producción pueden compartir el mismo product environment de Cloudinary. El aislamiento actual depende de clave, prefijo y guardas de borrado.

La validación real de cargas, reemplazos y eliminaciones queda para las Fases 5 y 8.

### Correo sin cola persistente

Una caída temporal de Gmail/SMTP produce error inmediato. No existe reintento persistente ni idempotencia de cola. Se resolverá en la Fase 7.

### Observabilidad externa

No hay Sentry, OpenTelemetry, Application Insights ni sistema equivalente configurado. Se documenta como recomendación futura; no se añade sin aprobación.

### Preview generado por el proyecto Vercel productivo

El proyecto productivo puede generar un Preview para commits de `Desarrollo`. El host Preview utiliza el rewrite de Desarrollo y no el backend productivo, pero consume despliegues del proyecto productivo.

Desactivarlo exigiría modificar configuración del proyecto productivo, operación prohibida durante este trabajo. Producción permanece intacta.

## 6. Certificación automatizada

Workflow nuevo:

```text
Fase 2 - Auditoría de configuración y dependencias
```

Valida:

- YAML/JSON de despliegue;
- rama, servicio, hosts y dominios de Desarrollo;
- migraciones automáticas deshabilitadas;
- prefijo Cloudinary;
- logo independiente de Producción;
- contenedor no-root;
- rate limiting;
- readiness de base;
- headers defensivos;
- dependencias .NET vulnerables;
- dependencias npm productivas altas o críticas.

La ejecución posterior a la corrección de la dependencia crítica terminó correctamente.

## 7. Criterio de cierre

La Fase 2 queda completa cuando el commit final obtiene:

- `Desarrollo - Compilación y pruebas`: success;
- `Desarrollo - aceptación funcional integral`: success;
- `Fase 2 - Auditoría de configuración y dependencias`: success.

La Fase 3 puede comenzar solamente después de registrar esos resultados en el PR y en el issue colaborativo.
