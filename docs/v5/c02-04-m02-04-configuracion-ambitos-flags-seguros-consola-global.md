# SOLQARYN V5 FINAL — C02-04.M02.04: configuración tipada, ámbitos y feature flags seguros

**Entrega real:** definición **DOCUMENTAL** de configuración, autoridad y activación segura para la consola global auditada C02-04. **Autor:** PRIMARY DEV 01 — JAVIER, Task ID `6adbb9fff1548191a1bd5b30d9e9984a`, 2026-11-14. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Fuentes:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet institucional PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, [entidades M02.01](c02-04-m02-01-entidades-ownership-invariantes-consola-global.md), [API/DTO M02.02](c02-04-m02-02-api-dto-errores-compatibilidad-consola-global.md), M01.01–06 ya LISTO y A5 independiente. `DAG_TECNICO!F=VALIDADO/G=SI` de este ID únicamente permite reclamar la unidad documental, no habilita ejecución ni despliegue.

## 1. Estado real de configuración comprobado

| Archivo real, SHA blob | Lectura explícita de código | Límite de inferencia |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` — `87fbf187b7993ba0f05edff393500f24d0856833` | Inyecta `IConfiguration`; lee `MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled` en proyección `puedeCrear` y en `alta-controlada` | Son flags del aprovisionamiento actual; NO autorizan consola excepcional, MFA ni grant |
| Mismo controlador, mismo blob | Lee `PublicStore:LegacyUpstreamHost` para comprobar precondición de host legacy | No es identificador de empresa elegida por cliente ni una fuente de autoridad privilegiada |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` — `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Valida autoridad global efectiva desde persistencia por solicitud | Un flag y un JWT aislado no reemplazan validación de rol actual |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` — `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist DEV explícita para GET de catálogo | Ningún feature flag extiende soporte DEV a POST, mutaciones, acceso cross-tenant ni operación global |
| `docs/v5/c02-04-m02-01-entidades-ownership-invariantes-consola-global.md` — `791a1cc82d0cd51c0d3515b88290072395593c7f` | Modelo futuro conceptual de solicitud, MFA, reauth, grant y auditoría | No existe una entidad, esquema o proveedor nuevo instalado |
| `docs/v5/c02-04-m02-02-api-dto-errores-compatibilidad-consola-global.md` — `4c1b3444bccae8a6899e82b68a2cbf53141b4f4b` | Contratos futuros, compatibilidad de tres operaciones actuales y errores seguros | No existen nuevos endpoints ni DTOs C# por estos documentos |

No se leen valores de secretos, credenciales, archivos de entorno reales ni configuración de producción. No se modifica `IConfiguration`, `Program.cs`, `appsettings`, `AppDbContext`, tokens, permisos, esquemas o datos.

## 2. Contrato FUTURO de opciones tipadas, no código implementado

| Grupo conceptual | Propiedad tipada objetivo | Default fail-closed y autoridad | Alcance y protección |
| --- | --- | --- | --- |
| `GlobalConsoleOptions` | `Enabled: bool` | `false`; solo habilita exposición de superficie con autorización adicional | Entorno+versión de despliegue; un `true` nunca otorga rol ni permiso |
| `GlobalConsoleOptions` | `AllowedOperations: Set<OperationId>` | Lista vacía por defecto; enum controlado y revisado | Operación nombrada, no wildcard ni texto ejecutable del usuario |
| `PrivilegedSessionPolicy` | `MfaRequired: bool`, `ReauthRequired: bool` | Ambas obligatoriamente `true` para operación sensible; un falso no es un override comercial | Política backend versionada, no elegible desde frontend |
| `PrivilegedSessionPolicy` | `MaximumProofAge: Duration` | Rechazar duración nula, negativa, ilimitada o fuera del límite aprobado | Por desafío/actor/sesión/acción; reloj monotónico o UTC confiable |
| `ExceptionalGrantPolicy` | `MaxTtl: Duration`, `RequireReason: bool` | TTL corto positivo y acotado, motivo obligatorio | Por tenant/recurso/operación; no transferible |
| `ExceptionalGrantPolicy` | `RequireIndependentApproval: bool` | Política efectiva estricta cuando se requiera, sin desactivación por parámetro de cliente | Aprobación real fuera del actor solicitante; no equivale a A5 de desarrollo |
| `PrivilegedAuditPolicy` | `FailClosedOnAuditFailure: bool`, `RetentionPolicyId` | Fallo cerrado obligatorio; retención sujeta a decisión autorizada | Auditoría con minimización de PII, nunca tokens, contraseñas u OTP |
| `PrivilegedQuotaPolicy` | `MaxRequestsPerActorWindow`, `MaxActiveGrants` | Límites finitos y positivos; cero permisos ante error de cuotas | Actor+empresa+operación, rate limit server-side |
| `PrivilegedResponsePolicy` | `NoStore: bool`, `MinimizeFields: bool` | Ambos true para proyección privilegiada | Respuesta particular sin caché compartido |
| `EmergencyDisablePolicy` | `DenyAllPrivilegedAccess: bool` | El kill-switch tiene precedencia para denegar; no existe switch de habilitación sin autenticación | Revocación controlada y auditoría, no altera el ERP ordinario |

Los nombres anteriores son **etiquetas de diseño**, no claves agregadas a `appsettings.json` o a un proveedor externo. Los flags existentes `MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled` conservan su significado actual y son **insuficientes** para autorización excepcional. Un valor ausente, inválido o inconsistente para acceso privilegiado se interpreta como DENY, nunca como habilitación; no cambiar el default del ERP existente fuera de un changeset propio.

## 3. Jerarquía de autoridad y propagación

1. **Autoridad de identidad**: validación backend contra rol global vigente persistido; ninguna opción tipada sustituye rol, pertenencia o Scope Lock.
2. **Puerta institucional**: DAG, PLAN/COLA y revisión independiente condicionan desarrollo; una opción de aplicación jamás crea autorización administrativa ni promoción a QA/main/PROD.
3. **Política versionada de seguridad**: el servidor valida versión de política y las restricciones más estrictas (MFA, reauth, mínimo privilegio, TTL y auditoría).
4. **Solicitud puntual**: actor/empresa/recurso/acción reales, grant vigente y pruebas frescas. Cliente solo propone parámetros, no políticas ni flags.
5. **Denegación preferente**: kill-switch, rol revocado, MFA ausente, proveedor caído, audit-fail, cuota y tiempo vencido tienen precedencia sobre cualquier flag permisivo.
6. **Observabilidad**: registrar ID de versión y causa de denegación sanitizada; nunca serializar valores secretos, OTP, JWT o claves de configuración sensibles.

**Config overlays futuros:** entorno > política institucional firmada > default estricto, sin permitir que variable de entorno, flag de UI, cabecera HTTP o valores libres rebajen los requisitos. Cualquier cambio exige revisión técnica, seguridad A/B, compatibilidad, rollback y autorización correspondiente. No se activa un servicio de flags ni se selecciona proveedor de pago en esta entrega.

## 4. Invariantes verificables futuras

| ID | Invariante |
| --- | --- |
| CFG-01 | `Enabled` no autoriza operaciones sin rol vigente, grant preciso, MFA y reauth validados. |
| CFG-02 | Falta de opción, parseo inválido o proveedor de configuración caído implica DENY para acceso excepcional. |
| CFG-03 | Política de seguridad nueva no puede reducir MFA, reauth, auditoría obligatoria, límites de TTL o aislamiento. |
| CFG-04 | `tenantId`, host, header y claims recibidos no seleccionan política ni conceden privilegios. |
| CFG-05 | Flags comerciales existentes no se reutilizan para acceso global ni cambian semántica histórica. |
| CFG-06 | Un kill-switch activo invalida toda nueva operación excepcional sin borrar historia. |
| CFG-07 | Rotación de política/versiones invalida grants incompatibles y evita uso de caché autorizado bajo versión vieja. |
| CFG-08 | Los límites de cuota por actor/tenant y tiempo son finitos y comprobados en servidor, sin bypass por concurrencia. |
| CFG-09 | La configuración no contiene JWT, OTP, passwords, DSN ni PII en logs o respuestas. |
| CFG-10 | Backend y auditoría aplican no-store, correlación opaca y denegación consistente ante fallos. |
| CFG-11 | Soporte DEV y rol ERP no se amplían mediante feature flags. |
| CFG-12 | Desactivar temporalmente la consola excepcional no desactiva protección de rutas ERP normales. |

## 5. Matriz simbólica de aceptación, NO pruebas ejecutadas

| Caso | Tipo | Entrada de diseño | Resultado esperado futuro |
| --- | --- | --- | --- |
| P01 | POS | Política futura completa y autorizada + rol global/MFA/reauth vigentes | Solo operación dentro de tenant/recurso/acción, con auditoría |
| P02 | POS | Kill-switch activado tras grant vigente | Siguiente uso privilegiado se deniega, con evento mínimo |
| P03 | POS | Política de cuota y TTL finitos bajo valores autorizados | TTL y límites efectivos estrictos aplicados en backend |
| P04 | POS | Cambio de versión de política con grants antiguos | Revalidación o revocación sin privilegios residuales |
| N01 | NEG | Flag `Enabled=true` sin rol global | DENY; nunca dar permisos desde flags |
| N02 | NEG | Flags `MultiEmpresa:*` activos sin MFA | DENY para consola excepcional |
| N03 | NEG | MFA/reauth requerida desactivada por config o cliente | Rechazar configuración insegura, ninguna concesión |
| N04 | NEG | TTL negativo, infinito, cero o manipulado por frontend | DENY y validación segura |
| N05 | NEG | `EmpresaId` de B se envía desde cuenta de A | DENY, sin lectura/escritura B |
| N06 | NEG | JWT/OTP/DSN en logs de configuración | Fallo de seguridad, sanitización |
| N07 | NEG | Política versionada caducada o no reconocida | DENY, no fallback a valor previo permisivo |
| N08 | NEG | Dependencia de configuración no disponible | DENY, sin 2xx privilegiado |
| N09 | NEG | Auditoría obligatoria falla mientras política permite operar | Rollback/deny, no conceder éxito |
| N10 | NEG | Kill-switch activo pero flag de UI sigue visible | Backend DENY independientemente de UI |
| N11 | NEG | Soporte DEV intenta POST con flag comercial | DENY por autorización y allowlist |
| N12 | NEG | Varias solicitudes paralelas saltan cuota de actor | Máximo efectivo y rechazo del excedente |
| N13 | NEG | Cache sirve respuesta privilegiada de tenant A a B | `no-store` + aislamiento server-side |
| N14 | NEG | Revocación de rol después de evaluar opción | Reevaluación y denegación del siguiente uso |

## 6. Auditoría y cierre propios de C02-04.M02.04

- **A0:** diff único de documentación y CHANGELOG; source blobs y semántica de opciones reales verificadas. No cambios productivos, ni secretos, ni encabezados vacíos.
- **A1:** 4 positivos y 14 negativos simbólicos especificados; **NO** se ejecutan tests de runtime por escribir Markdown.
- **A2:** seguridad documental tenant A/B, no-client-authority, MFA/reauth, RBAC, auditoría y fail-closed sin alterar código.
- **A3:** NO_APLICA tests .NET/Angular/MySQL/HTTP/Playwright porque esta microtarea **no** cambia runtime ni DB. Cualquier CI adicional in_progress/cancelled/skipped no es PASS.
- **A4:** publicar por fast-forward/CAS directo en `dev`; Scope Lock exact-SHA y V5 procedencia del CHANGELOG `completed/success`; readback de PLAN/COLA/BITACORA en 26 columnas.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, nunca el autor. M02.04 LISTO_DEV no habilita M03 IAM/DB, consolas live, secretos, QA, main ni producción.

**Rollback de esta documentación:** `git revert` convencional, sin reset/force ni cambiar historia de BITACORA. Rollback futuro de privilegios y versiones necesita pruebas reales y autorización específica antes de habilitar cualquier API o servicio.
