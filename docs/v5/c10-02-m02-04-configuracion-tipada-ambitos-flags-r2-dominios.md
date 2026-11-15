# SOLQARYN V5 — C10-02.M02.04: configuración tipada y flags seguros para archivos y dominios futuros

**Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` · **ID:** C10-02.M02.04, PLAN fila 1487 / DAG fila 1448 · **Autor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507` · **Ámbito:** solqaryn/Solqaryn@dev · **Scope Lock:** STRICT · **Fecha:** 2026-11-15.

## Propósito y fronteras

Este trabajo **solamente documenta** una configuración futura para buckets/prefijos por empresa, dominios privados, enlaces firmados, caducidad, replay y lifecycle. No crea una cuenta ni conexión R2, bucket, DNS, certificado, secreto, middleware, flag ejecutable, tabla, worker, endpoint, migración ni despliegue. La infraestructura vigente y Cloudinary se conservan; Hostinger/Cloudflare son objetivos sujetos a futura orden y gates de seguridad y cutover.

La autoridad efectiva del usuario/empresa deriva de JWT, membresía vigente, RBAC y repositorio/servicio backend, nunca del host ni del prefijo aportado por el navegador. Un objeto de tenant B no será visible en A aunque comparta key, URL, hash o cache. No se habilita consumo público por un default ni se interpretan categorías/archivos legados de propietario desconocido como compartidos.

## Ámbitos con precedencia obligatoria

| Ámbito propuesto | Control | Garantía |
| --- | --- | --- |
| PLATFORM_SECURITY | Operador global autorizado | Cotas máximas de TTL, política de firma, allowed domains, privacidad, límite de replay y auditoría: no reducibles por tenant |
| DEPLOYMENT_ENV | CI/infraestructura autorizada, fuera de repositorio | Credenciales, regiones, endpoint R2 y secreto HMAC no expuestos en frontend ni en documentación |
| TENANT_FILES | Administrador tenant autenticado dentro de ceiling platform | Alias, metadatos y opciones de publicación acotadas; no modifica credenciales ni tenant de objeto |
| RESOURCE_POLICY | Backend por recurso con ownership y versión | Visibilidad, expiración y revocación específicas; no puede vencer los controles superiores |

Precedencia: `PLATFORM_SECURITY -> DEPLOYMENT_ENV -> TENANT_FILES -> RESOURCE_POLICY`, con intersección restrictiva, nunca unión permisiva. La UI no sustituye autorización del servidor.

## Opciones tipadas propuestas — no existen en runtime por este documento

| ID | Opción conceptual | Default seguro | Validación que deberá implementarse |
| --- | --- | --- | --- |
| CFG01 | `FutureObjectStoreEnabled: bool` | false | No activable sin aprobación de infraestructura y pruebas de rollback |
| CFG02 | `TenantPrefixMode: enum` | `SERVER_DERIVED_ONLY` | Nunca prefijo proporcionado por cliente |
| CFG03 | `PrivateByDefault: bool` | true | No permitir false a nivel tenant sin autorización superior |
| CFG04 | `MaxSignedUrlTtlSeconds: int` | 0 hasta política aprobada | Tope plataforma, duración positiva sólo con auth y expiración verificables |
| CFG05 | `ReplayDefenseMode: enum` | `DENY_UNCONFIGURED` | Nonce/one-time lease futuro; replay de token rechazado |
| CFG06 | `AllowedHostBindings: set<host>` | vacío | Validación hostname server-side y ownership A/B |
| CFG07 | `PublicMediaProjectionEnabled: bool` | false | Sólo medios aprobados y DTO público mínimo |
| CFG08 | `UploadMaxBytes: long` | límite bloqueante hasta calibrar | Anti-DoS; MIME y contenido verificados, no confiar en extensión |
| CFG09 | `AllowedMimeTypes: set<string>` | allowlist vacía | Rechazar contenido ejecutable/políglota |
| CFG10 | `DownloadRateLimit: int/window` | techo plataforma | Presupuesto por tenant+usuario+recurso |
| CFG11 | `RetentionClass: enum` | `SERVER_POLICY` | Preservar retención legal, auditoría y referencias comerciales |
| CFG12 | `LifecycleDeleteMode: enum` | `DENY_UNAPPROVED` | Tombstone y backup comprobados antes de limpieza |
| CFG13 | `CanonicalObjectIdVersion: int` | versión conocida | No aceptar path traversal ni ID arbitrario |
| CFG14 | `SignedUrlAudience: enum` | `AUTHENTICATED_SCOPE` | Revocación vigente y binding de recurso, usuario y tenant |
| CFG15 | `CacheIsolationKeyMode: enum` | `TENANT_RESOURCE_VERSION` | Evitar fuga host A/B y reuso de respuesta privada |
| CFG16 | `AuditMinimumPolicy: enum` | `REQUIRED` | Correlación sin firma/PII/secreto en logs |
| CFG17 | `ProviderFailoverMode: enum` | `DISABLED` | No activar nuevo destino sin comparativa de checksums |
| CFG18 | `ReadOnlyMigrationPreview: bool` | false | Sólo después de validación documental, sin escritura externa |

Estas opciones son propuestas técnicas pendientes de compatibilidad con la configuración existente, no un contrato público ni valores productivos definidos.

## Reglas de denegación y validación

1. Prefijo derivado de identificador tenant autenticado en backend; prohibir `../`, separadores ambiguos, URL prefirmada controlada por usuario y claves cross-tenant.
2. Enlace firmado futuro liga objeto, operación, audiencia, tenant, versión y vencimiento UTC; un enlace vencido o revocado deniega en servidor, no por JS del navegador.
3. Revocación de membresía o rol debe invalidar permisos aun si una URL previamente emitida permanece en memoria/cache.
4. HMAC y credenciales no aparecen en metadatos, logs, URL persistentes ni Git; rotación y observabilidad exigen mecanismos auditables fuera de esta fase.
5. Un objeto sin ownership verificable se cuarentena; no usar null/legacy como permiso global.
6. No permitir upload ejecutable ni rutas ambiguas; validar MIME por contenido y tamaño máximo en el borde.
7. Cache pública y privada aisladas por tenant, recurso y versión; error 404/403 no revela existencia B desde A.
8. Cualquier cambio de retención respeta documentos financieros/legalmente protegidos y referencias; no efectuar borrados físicos desde flags.
9. Modo DEV no reutiliza secretos, buckets o URLs de PROD, no implica cutover ni dominios reales.
10. Todas las operaciones futuras deben incluir límites de tasa/bytes, auditoría de actor y manejo de errores sin PII.
11. La migración a otro proveedor exige reconciliación de manifiestos y checksums sin modificar Cloudinary de transición.
12. Cambio de configuración debe ser versionado/CAS con rollback, sin activar objetos/hosts desconocidos silenciosamente.

## Pruebas de aceptación FUTURAS_NO_EJECUTADAS

| Escenario | Resultado positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Tenant A resuelve prefijo propio | A no lee prefijo/URL de B |
| P02/N02 | URL con audiencia propia y TTL válido | URL caducada o de otro actor falla |
| P03/N03 | Host verificado resuelve media autorizada | Host no acreditado no enumera objetos |
| P04/N04 | Revocación invalida acceso privado | JWT antiguo no reautoriza |
| P05/N05 | Archivo válido cumple MIME y tamaño | Payload ejecutable/disfrazado se rechaza |
| P06/N06 | Cache A responde recurso/version A | Cache B no reutiliza el resultado A |
| P07/N07 | Documento referenciado conserva retención | Flag tenant no borra historial |
| P08/N08 | Error de proveedor da respuesta segura | Falla no activa un fallback inseguro |
| P09/N09 | Auditoría contiene correlationId seguro | Secreto/HMAC/PII no aparecen |
| P10/N10 | Migración futura verifica checksums | Dato divergente no se publica |
| P11/N11 | Firma futura liga versión y operación | Replay/nonce reutilizado se deniega |
| P12/N12 | Cambio versionado permite rollback | CAS viejo no sobrescribe nueva política |

Todas son expectativas para una futura implementación, **no pruebas ejecutadas**. Los trabajos runtime requerirán .NET xUnit, MySQL 8.4 aislado si aplica, pruebas de firma/replay, Playwright si afecta UI y checks GitHub Actions causal exact SHA.

## A0–A5, impacto, rollback

A0: Plan V5/Skill/Scope Lock/HEAD/COLA/DAG/owner. A1: coherencia y cobertura de 18 opciones/12 reglas/12 escenarios; funcional HTTP `NO_APLICA` para este diff documental. A2: amenaza tenant A/B, identidad, TTL/replay, privacidad/retención y secretos documentada sin afirmar seguridad del runtime. A3: compatibilidad conceptual Domain/Application/Infrastructure/API/Cloudinary; builds, MySQL, Playwright y proveedores `NO_APLICA` para sólo Markdown, nunca PASS. A4: commit fast-forward en dev con un documento y CHANGELOG autocontenido por ID, Scope Lock y V5 procedencia `completed/success` en SHA original o equivalencia de blob probada con checks causales; readbacks PLAN/COLA/DAG/BITACORA. A5: exclusivamente otra Task Javier distinta de la autora.

Rollback documental: `git revert` aditivo del changeset, sin reset, force-push ni borrar BITACORA. No realizar DNS, R2, Cloudflare, Hostinger, cambios sobre main/QA/PROD, migraciones, cuentas o compras.
