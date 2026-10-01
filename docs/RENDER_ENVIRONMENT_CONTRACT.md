# Contrato canónico de variables de entorno — Render

Estado vigente para `solqaryn-api-dev` y `solqaryn-api-prod`.

## Regla de paridad

DEV y PROD deben tener exactamente las mismas claves administradas por SOLQARYN. Solo pueden cambiar los valores propios del entorno (por ejemplo conexión MySQL, JWT, CORS, prefijo Cloudinary, URL pública y refresh token OAuth2).

Contrato canónico: **28 claves en DEV y 28 claves en PROD**.

| Variable | Estado | Motivo | Valor por entorno |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | REQUERIDA | Selecciona comportamiento Development/Production del host ASP.NET. | Diferente |
| `AllowedHosts` | REQUERIDA | Restringe Host headers al hostname del servicio. | Diferente |
| `Database__ServerVersion` | REQUERIDA | Configura Pomelo/MySQL para la versión real de Aiven. | Normalmente igual |
| `Database__ApplyMigrationsOnStartup` | REQUERIDA | Política explícita de migraciones: DEV puede aplicarlas; PROD no. | Diferente |
| `ConnectionStrings__DefaultConnection` | REQUERIDA/SECRETA | Conexión a la base MySQL del entorno. | Diferente |
| `Jwt__Secret` | REQUERIDA/SECRETA | Firma de tokens JWT. | Diferente |
| `Jwt__Issuer` | REQUERIDA | Emisor JWT validado por backend. | Diferente |
| `Jwt__Audience` | REQUERIDA | Audiencia JWT validada por backend. | Diferente |
| `Jwt__ExpiraMinutos` | REQUERIDA | Política de duración de sesión. | Igual salvo decisión explícita |
| `Security__LoginRateLimitPerMinute` | REQUERIDA | Control de intentos de login. | Igual salvo decisión explícita |
| `Cloudinary__CloudName` | REQUERIDA | Identifica el cloud activo de medios. | Puede variar |
| `Cloudinary__ApiKey` | REQUERIDA/SECRETA | Autenticación Cloudinary. | Puede variar |
| `Cloudinary__ApiSecret` | REQUERIDA/SECRETA | Autenticación Cloudinary. | Puede variar |
| `Cloudinary__EnvironmentPrefix` | REQUERIDA | Aísla objetos DEV/PROD. | Diferente |
| `Cors__AllowedOrigins__0` | REQUERIDA | Autoriza únicamente el frontend del entorno. | Diferente |
| `AppSettings__BackendPublicUrl` | REQUERIDA | Genera enlaces públicos de facturas con el backend correcto. | Diferente |
| `AppSettings__EnlacePublicoFacturaHorasValidez` | REQUERIDA | Política de vencimiento de enlaces públicos. | Igual salvo decisión explícita |
| `AppSettings__EnlacePublicoFacturaMaximoAccesos` | REQUERIDA | Límite de aperturas del enlace público. | Igual salvo decisión explícita |
| `AppSettings__CorreoFacturaIdempotenciaMinutos` | REQUERIDA | Evita reenvíos duplicados de factura. | Igual salvo decisión explícita |
| `Smtp__Host` | REQUERIDA | Endpoint SMTP de Outlook. | Igual |
| `Smtp__Port` | REQUERIDA | Puerto SMTP STARTTLS. | Igual |
| `Smtp__UsuarioSmtp` | REQUERIDA | Identidad del buzón SOLQARYN. | Igual |
| `Smtp__OAuth2ClientId` | REQUERIDA | Identificador público de la app Microsoft. | Igual |
| `Smtp__OAuth2RefreshToken` | REQUERIDA/SECRETA | Renueva access tokens sin almacenar contraseña. | Diferente |
| `Smtp__NombreRemitente` | REQUERIDA | Distingue mensajes DEV/PROD y mantiene identidad SOLQARYN. | Diferente |
| `Smtp__TimeoutSeconds` | REQUERIDA | Límite operacional de conexión/envío. | Igual salvo ajuste |
| `Smtp__MaxAttempts` | REQUERIDA | Política de reintentos transitorios. | Igual salvo ajuste |
| `Smtp__RetryBaseDelayMilliseconds` | REQUERIDA | Backoff base de reintentos. | Igual salvo ajuste |

## Variables retiradas del contrato de Render

Estas claves no deben existir como configuración desplegada de DEV/PROD:

- `Smtp__PasswordSmtp`: autenticación básica eliminada; SOLQARYN usa OAuth2 exclusivamente.
- `Smtp__OAuth2ClientSecret`: no aplica al cliente público Microsoft utilizado.
- `Smtp__AuthenticationMode`: dejó de tener sentido al existir un único modo autenticado: OAuth2.
- `Smtp__OAuth2TokenEndpoint`: constante canónica del backend.
- `Smtp__OAuth2Scope`: constante canónica del backend.
- `Smtp__UsarSsl`: Render usa el default seguro; Outlook requiere STARTTLS.
- `Smtp__RequiereAutenticacion`: Render usa el default seguro `true`; la opción `false` queda reservada a pruebas locales aisladas.
- `Smtp__CorreoRemitente`: se deriva de `Smtp__UsuarioSmtp`.
- `Smtp__CorreoRespuesta`: por defecto se deriva del remitente.
- `Swagger__Enabled`: redundante; PROD ya queda deshabilitado por configuración base y DEV se habilita por `ASPNETCORE_ENVIRONMENT=Development`.
- `AppSettings__LogoPublicUrl`: redundante mientras no exista un fallback externo; el logo empresarial se resuelve desde la configuración de empresa.
- `SeedAdmin__Username`: secreto/parametrización de bootstrap innecesaria en servicios ya provisionados.
- `SeedAdmin__Password`: secreto de bootstrap innecesario en servicios ya provisionados.

## SMTP canónico

- proveedor: Outlook.com;
- host: `smtp-mail.outlook.com`;
- puerto: `587`;
- seguridad: STARTTLS;
- autenticación: OAuth2;
- endpoint de token: `https://login.microsoftonline.com/consumers/oauth2/v2.0/token`;
- scope: `https://outlook.office.com/SMTP.Send offline_access`;
- cuenta: `solqaryn.platform@outlook.com`;
- contraseña SMTP: no utilizada;
- client secret OAuth2: no utilizado;
- refresh token: secreto independiente por entorno.

## Regla de promoción

No promover cambios de configuración a PROD sin autorización explícita y sin certificar en DEV el build/deploy, health/readiness y los contratos de aislamiento aplicables.

El SMTP real está actualmente aplazado/no bloqueante por la conectividad disponible en Render Free. La promoción no autoriza comprar un plan ni introducir keep-alive. Si SMTP entra expresamente en el alcance de una release futura, entonces sí debe superar diagnóstico OAuth2 y envío real controlado antes de declarar ese componente certificado.
